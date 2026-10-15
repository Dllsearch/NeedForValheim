"""Reconstruct the 26 licensed MW songs from local VLT events and MPF node chains.

Pathfinder syntax: github.com/xan1242/MPFmaster (MIT), release 1.8.
No soundtrack downloads or song-order guesses. WAV masters remain private.
"""
import argparse,hashlib,json,re,shutil,struct,subprocess,wave
from pathlib import Path
from project import BASE,LOCAL,config,mw_root,vgmstream_path
from mw_vault import Vault

def digest(path):
    h=hashlib.sha256()
    with Path(path).open('rb') as f:
        for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
    return h.hexdigest()

def parse_map(text):
    nodes={}
    for m in re.finditer(r'^Node (\d+) #[^\n]*\n\{(.*?)^\}',text,re.M|re.S):
        body=m[2];fields={k:int(v) for k,v in re.findall(r'^\s*(Wave|Track|Section|Part) (-?\d+)\s*$',body,re.M)}
        fields['next']=[int(x) for x in re.findall(r'^\s*Control -?\d+,-?\d+ > (\d+)',body,re.M)]
        nodes[int(m[1])]=fields
    events={int(m[1],16):[int(x) for x in re.findall(r'\bBranch (-?\d+) ',m[2]) if int(x)>=0]
            for m in re.finditer(r'^Event 0x([0-9A-Fa-f]+) #[^\n]*\n\{(.*?)^\}',text,re.M|re.S)}
    if not nodes or not events:raise ValueError('Empty Pathfinder map')
    return nodes,events

def song_chain(nodes,start):
    visited=set();waves=[];chain=[];node=start
    while True:
        if node in visited or node not in nodes:raise ValueError('Cyclic or missing licensed-song node')
        visited.add(node);chain.append(node);n=nodes[node]
        if n.get('Section')!=5 or n.get('Track')!=0:raise ValueError('Not a single licensed-song chain')
        if n['Wave']>0:waves.append(n['Wave'])
        if not n['next']:break
        if len(n['next'])!=1:raise ValueError('Interactive branching cannot be exported as one licensed song')
        node=n['next'][0]
    if not waves:raise ValueError('Licensed song has no audio')
    return chain,waves

def song_catalog(root,nodes,events):
    vault=Vault(root/'GLOBAL/attributes.bin');songs=[]
    # Native music metadata class and artist/title/event fields, independently
    # resolved from the installed vault; no fixed soundtrack ordering is used.
    for (cls,row),record in vault.rows.items():
        if cls!=0x565465f8:continue
        f=vault.get(vault.name(cls),vault.name(row));event=int.from_bytes(bytes.fromhex(f['0x6d154d92']['hex']),'little')&0xffffff
        if event==0:continue # inherited Artist1/SongName1 defaults
        starts=events.get(event,[])
        if len(starts)!=1:raise ValueError('Missing or ambiguous native song event')
        chain,waves=song_chain(nodes,starts[0]);artist=f['0x3cc1b385'].strip();song=f['0xc352718e'].strip()
        if not artist or not song:raise ValueError('Missing native song title')
        songs.append(dict(id=f'mw-{event:06x}',title=artist+' – '+song,artist=artist,song=song,event=f'{event:06x}',startNode=starts[0],nodes=chain,segments=waves))
    if len(songs)!=26 or len({s['id'] for s in songs})!=26:raise ValueError('Expected 26 distinct native EA TRAX events')
    return sorted(songs,key=lambda s:s['title'].casefold())

def join_waves(paths,target):
    total=0;fmt=None
    with wave.open(str(target),'wb') as output:
        for path in paths:
            with wave.open(str(path),'rb') as part:
                current=(part.getnchannels(),part.getsampwidth(),part.getframerate())
                if fmt is None:
                    fmt=current;output.setnchannels(fmt[0]);output.setsampwidth(fmt[1]);output.setframerate(fmt[2])
                if current!=fmt or fmt[0]!=2 or fmt[1]!=2:raise ValueError('Song segment format changed')
                frames=part.getnframes();body=part.readframes(frames)
                if len(body)!=frames*fmt[0]*fmt[1]:raise ValueError('Truncated song segment')
                output.writeframesraw(body);total+=frames
    if not fmt or total<fmt[2]*60:raise ValueError('Incomplete licensed song')
    return fmt,total

def export(root,decoder,mpf_tool,ffmpeg,output):
    work=LOCAL/'music-mw-source';work.mkdir(parents=True,exist_ok=True)
    mpf=root/'SOUND/PFDATA/MW_Music.mpf';mus=mpf.with_suffix('.mus');attrib=root/'GLOBAL/attributes.bin'
    if mpf.read_bytes()[:6]!=b'xDFP\x05\x01':raise ValueError('Expected MW PC Pathfinder 5.1')
    identity={p.name:digest(p) for p in (mpf,mus,attrib)}
    text=work/'map.txt';subprocess.run([str(mpf_tool),str(mpf),str(text)],check=True,stdout=subprocess.DEVNULL)
    songs=song_catalog(root,*parse_map(text.read_text(encoding='utf-8-sig')))
    segments=work/'segments';segments.mkdir(exist_ok=True)
    wanted=sorted({s for song in songs for s in song['segments']});cache=work/'segments-source.json'
    valid=cache.exists() and json.loads(cache.read_text())==identity and all((segments/f'{n}.wav').is_file() for n in wanted)
    if not valid:
        subprocess.run([str(decoder),'-i','-s',str(min(wanted)),'-S',str(max(wanted)),'-o',str(segments/'?s.wav'),str(mpf)],check=True,stdout=subprocess.DEVNULL)
        if not all((segments/f'{n}.wav').is_file() for n in wanted):raise ValueError('Decoder omitted a song segment')
        cache.write_text(json.dumps(identity),encoding='utf-8')
    masters=work/'masters';masters.mkdir(exist_ok=True);output.mkdir(parents=True,exist_ok=True)
    manifest=[]
    for song in songs:
        master=masters/(song['id']+'.wav');fmt,frames=join_waves([segments/f'{n}.wav' for n in song['segments']],master)
        pending=output/(song['id']+'.pending.ogg');target=output/(song['id']+'.ogg')
        command=[str(ffmpeg),'-hide_banner','-loglevel','error','-nostdin','-y']
        subprocess.run([*command,'-i',str(master),'-map','0:a:0','-map_metadata','-1','-c:a','libvorbis','-b:a','192k',str(pending)],check=True)
        verify=work/'verify.wav';subprocess.run([*command,'-xerror','-i',str(pending),'-c:a','pcm_s16le',str(verify)],check=True)
        with wave.open(str(verify),'rb') as w:
            if (w.getnchannels(),w.getsampwidth(),w.getframerate())!=fmt or abs(w.getnframes()-frames)>fmt[2]*.02:raise ValueError('Compressed song did not fully decode at original length')
        verify.unlink();pending.replace(target)
        manifest.append(dict(song,file=target.name,sourceGame='mw',source='MW_Music.mpf',seconds=round(frames/fmt[2],3),sampleRate=fmt[2],frames=frames,sha256=digest(target),source_sha256=digest(master),source_files=identity))
        print(f"{len(manifest)}/26 {song['title']}: {frames/fmt[2]:.2f}s, {len(song['segments'])} native segments",flush=True)
    pending=output/'tracks.pending.json';pending.write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8');pending.replace(output/'tracks.json')
    (work/'provenance.json').write_text(json.dumps(dict(source_files=identity,mpf_tool_sha256=digest(mpf_tool),decoder_sha256=digest(decoder),routing='Native licensed-song Section 5 event chains; wave IDs are 1-based decoder subsongs.',songs=manifest),indent=2,ensure_ascii=False),encoding='utf-8')
    return manifest

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--source',type=Path);p.add_argument('--decoder',type=Path);p.add_argument('--mpf-tool',type=Path);p.add_argument('--ffmpeg',type=Path);p.add_argument('--output',type=Path,default=LOCAL/'radio-mw');a=p.parse_args()
    tool=a.mpf_tool or Path(config().get('mw_mpf_tool',LOCAL/'mw-research/mpfmaster/Release/MPFmaster.exe'))
    if not tool.is_absolute():tool=BASE/tool
    ffmpeg=a.ffmpeg or shutil.which('ffmpeg')
    if not ffmpeg:
        try:
            import imageio_ffmpeg
            ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
        except ImportError:pass
    if not ffmpeg:raise SystemExit('Supply local FFmpeg using --ffmpeg')
    export((a.source or mw_root()).resolve(),(a.decoder or vgmstream_path()).resolve(),tool.resolve(),Path(ffmpeg).resolve(),a.output.resolve())
if __name__=='__main__':main()
