"""Adapt original local MW frontend artwork, bitmap title glyphs and FE sounds."""
import argparse,base64,hashlib,json,struct,subprocess,wave
from pathlib import Path
from PIL import Image
from project import LOCAL,mw_root,vgmstream_path
from nfs_formats import TexturePack,jdlz,bin_hash
from export_geometry import chunks

ROLES={'grit':0x128fdcb8,'banner':0x128fdcb9,'graf':0x312c3dff,'vignette':0x7f1b63b3,
       'underlay':0x94ec6a18,'arrow':0x02716e8a,'highlight':0x249db7b7,
       'trax':0x575221d4,'trax_base':0x418c7ff8,'noise':0x511ca7b4,
       'icon_0':0x028c24f6,'icon_1':0xc52ccbf6,'icon_2':0xf69ac384,'icon_4':0x9701bde4,
       'icon_5':0x0294d2a3,'icon_10':0xc6a1a6e0,'icon_11':0x3058fe37,
       'icon_13':0x12fe30a5,'icon_14':0xb0c46023,'icon_15':0x228b7e32}
LABELS={0:'VEHICLE',1:'EA TRAX',2:'LIGHTS',3:'NEON',4:'NITRO',5:'WHEELS',6:'BODY',7:'INTERIOR',8:'DISPLAY',9:'TURRET',10:'CAR SELECT',11:'OPTIONS',12:'EXHAUST',13:'PERFORMANCE',14:'CUSTOMIZE',15:'SAVED BUILDS'}

def texture(name,image,limit=(512,512)):
    image=image.convert('RGBA');image.thumbnail(limit,Image.Resampling.LANCZOS)
    return dict(name=name,width=image.width,height=image.height,rgba=base64.b64encode(image.tobytes()).decode('ascii'))

def font_glyphs(blob):
    # Native FNTF, 16-byte glyph records: Unicode, w/h, atlas x/y,
    # signed bearings and advance. Header scale is reciprocal logical atlas size.
    base=blob.find(b'FNTF')
    if base<0 or base+128>len(blob):raise ValueError('Missing native title-font header')
    count=struct.unpack_from('<H',blob,base+10)[0];offset=struct.unpack_from('<I',blob,base+20)[0]
    scale=struct.unpack_from('<2f',blob,base+64);line=struct.unpack_from('<I',blob,base+36)[0]
    if not 1<=count<=512 or base+offset+16*count>len(blob) or not all(0<s<=1 for s in scale) or not 1<=line<=256:raise ValueError('Invalid native font metrics')
    glyphs={}
    for i in range(count):
        p=base+offset+i*16;char,w,h,x,y=struct.unpack_from('<HBBHH',blob,p);bx,by=struct.unpack_from('<bb',blob,p+9);advance=struct.unpack_from('<H',blob,p+14)[0]
        if char in glyphs or advance>256:raise ValueError('Invalid native glyph')
        glyphs[char]=dict(w=w,h=h,x=x,y=y,bx=bx,by=by,advance=advance)
    return glyphs,line,scale

def render_title(text,atlas,metrics):
    glyphs,line,scale=metrics;glyphs_text=[glyphs[ord(c)] for c in text];width=sum(g['advance'] for g in glyphs_text)+8
    canvas=Image.new('RGBA',(width,line+8));x=4;sx=atlas.width*scale[0];sy=atlas.height*scale[1]
    for g in glyphs_text:
        if g['w'] and g['h']:
            box=tuple(round(v) for v in (g['x']*sx,g['y']*sy,(g['x']+g['w'])*sx,(g['y']+g['h'])*sy))
            if box[0]<0 or box[1]<0 or box[2]>atlas.width or box[3]>atlas.height:raise ValueError('Glyph outside native atlas')
            glyph=atlas.crop(box).resize((g['w'],g['h']),Image.Resampling.LANCZOS)
            canvas.alpha_composite(glyph,(x+g['bx'],g['by']))
        x+=g['advance']
    return canvas

def export(root,decoder):
    source=root/'FRONTEND/FrontB.lzc';data=source.read_bytes();data=jdlz(data) if data[:4]==b'JDLZ' else data
    cache=LOCAL/'mw-frontend-decoded.bin';cache.write_bytes(data);pack=TexturePack(cache);out=LOCAL/'menu';out.mkdir(exist_ok=True)
    art={};provenance={}
    for role,key in ROLES.items():
        name,image,_=pack.decode(key);art[role]=texture(name,image);provenance[role]=dict(key=f'{key:08x}',name=name)
    font=next(data[a:z] for tag,a,z in chunks(data,0,len(data)) if tag==0x30201 and data[a:a+14].startswith(b'font_mw_title'))
    metrics=font_glyphs(font);_,atlas,_=pack.decode(0xe0463afa);atlas=atlas.convert('RGBA')
    for tab,title in LABELS.items():art[f'nav_{tab}']=texture('FONT_MW_TITLE:'+title,render_title(title,atlas,metrics),(512,80))
    art['wordmark']=texture('FONT_MW_TITLE:most wanted',render_title('most wanted',atlas,metrics),(512,100))
    # Only exact full hash matches; truncated resource prefixes cannot identify a car.
    from mw_source import Source
    aliases={'BMWM3GTR':'M3_GTR','BMWM3GTRE46':'M3_GTRE46','CORVETTEC6':'CORVETTE','CORVETTEC6R':'CORVETTEC6R'}
    for row in Source(root).roster():
        car=row['car'];key=bin_hash('SECONDARY_LOGO_'+aliases.get(car,car))
        if key in pack.records:
            name,image,_=pack.decode(key);role='car_mw-'+car.lower();art[role]=texture(name,image,(384,96));provenance[role]=dict(key=f'{key:08x}',name=name)
    target=out/'mw-frontend.json';pending=target.with_suffix('.pending.json');pending.write_text(json.dumps(art,separators=(',',':')),encoding='utf-8');pending.replace(target)
    # FE_COMMON source samples are reused with explicit configurable adaptation
    # roles, rather than claiming a recovered original native event mapping.
    bank=root/'SOUND/GLOBAL/FE_COMMON_MB.abk';audio=[]
    private=LOCAL/'mw-frontend-audio';private.mkdir(exist_ok=True)
    for role,index in (('navigate',1),('accept',3),('back',2)):
        path=private/(role+'.wav');subprocess.run([str(decoder),'-i','-s',str(index),'-o',str(path),str(bank)],check=True,stdout=subprocess.DEVNULL)
        with wave.open(str(path),'rb') as w:
            if w.getsampwidth()!=2 or w.getnchannels()!=1 or not 0<w.getnframes()<=w.getframerate()*3:raise ValueError('Invalid MW frontend one-shot')
            frames=w.readframes(w.getnframes());params=w.getparams()
        with wave.open(str(out/('mw-'+role+'.wav')),'wb') as w:w.setparams(params);w.writeframes(frames)
        audio.append(dict(role=role,subsong=index,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    (out/'mw-frontend-provenance.json').write_text(json.dumps(dict(source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),resources=provenance,font='Native FNTF glyph metrics and FONT_MW_TITLE atlas; labels rasterized from original glyphs.',audio=audio,audio_source_sha256=hashlib.sha256(bank.read_bytes()).hexdigest(),adaptation='NFVU2 actions and layout using original local MW artwork. FE sound role mapping is adapted, not a recovered event table.'),indent=2),encoding='utf-8')
    print(f'Exported {len(art)} MW artwork/bitmap labels, {len(provenance)-len(ROLES)} exact car logos and 3 FE sounds; {target.stat().st_size/1048576:.1f} MiB.')
if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--source',type=Path);p.add_argument('--decoder',type=Path);a=p.parse_args();export((a.source or mw_root()).resolve(),(a.decoder or vgmstream_path()).resolve())
