import base64,json,struct,sys,tempfile,unittest,wave
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from project import LOCAL,mw_root
from extract_mw_music import parse_map,song_chain,song_catalog,join_waves,digest
from export_mw_frontend import font_glyphs,render_title

class MwFrontendMusic(unittest.TestCase):
    def test_chain_preserves_graph_order_and_repeated_segments(self):
        nodes={0:dict(Wave=0,Section=5,Track=0,next=[7]),7:dict(Wave=5,Section=5,Track=0,next=[2]),2:dict(Wave=2,Section=5,Track=0,next=[8]),8:dict(Wave=5,Section=5,Track=0,next=[9]),9:dict(Wave=-1,Section=5,Track=0,next=[])}
        self.assertEqual(song_chain(nodes,0),([0,7,2,8,9],[5,2,5]))
        nodes[9]['next']=[7]
        with self.assertRaises(ValueError):song_chain(nodes,0)
        nodes[9]['next']=[];nodes[0]['next']=[7,2]
        with self.assertRaises(ValueError):song_chain(nodes,0)
    def test_join_preserves_stereo_sample_boundaries(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);parts=[]
            for i,pair in enumerate(((112,-201),(307,-409))):
                p=root/f'{i}.wav';parts.append(p)
                with wave.open(str(p),'wb') as w:w.setparams((2,2,1000,0,'NONE',''));w.writeframes(struct.pack('<hh',*pair)*31000)
            target=root/'song.wav';fmt,frames=join_waves(parts,target);self.assertEqual((fmt,frames),((2,2,1000),62000))
            with wave.open(str(target),'rb') as w:
                w.setpos(30999);self.assertEqual(w.readframes(2),struct.pack('<4h',112,-201,307,-409))
    @unittest.skipUnless((LOCAL/'music-mw-source/map.txt').is_file(),'local MW soundtrack not prepared')
    def test_all_exported_music_matches_native_event_chains(self):
        nodes,events=parse_map((LOCAL/'music-mw-source/map.txt').read_text(encoding='utf-8-sig'));songs=song_catalog(mw_root(),nodes,events)
        entries=json.loads((LOCAL/'radio-mw/tracks.json').read_text(encoding='utf-8'));self.assertEqual(len(entries),26)
        original={s['id']:s for s in songs};self.assertEqual(set(original),{e['id'] for e in entries})
        for entry in entries:
            source=original[entry['id']]
            for key in ('title','event','startNode','nodes','segments'):self.assertEqual(entry[key],source[key])
            path=LOCAL/'radio-mw'/entry['file'];self.assertEqual(path.read_bytes()[:4],b'OggS');self.assertEqual(digest(path),entry['sha256'])
            frames=0
            for sample in entry['segments']:
                with wave.open(str(LOCAL/f'music-mw-source/segments/{sample}.wav'),'rb') as w:
                    self.assertEqual((w.getframerate(),w.getnchannels(),w.getsampwidth()),(entry['sampleRate'],2,2));frames+=w.getnframes()
            self.assertEqual(frames,entry['frames']);self.assertGreater(entry['seconds'],120)
    @unittest.skipUnless((LOCAL/'mw-research/font_mw_title.bin').is_file(),'private MW font absent')
    def test_native_font_and_art_keep_separate_layout(self):
        glyphs,line,scale=font_glyphs((LOCAL/'mw-research/font_mw_title.bin').read_bytes());self.assertEqual((glyphs[65]['w'],glyphs[65]['h']),(29,26));self.assertEqual(line,39)
        art=json.loads((LOCAL/'menu/mw-frontend.json').read_text());self.assertGreaterEqual(len(art),60)
        for key,t in art.items():
            raw=base64.b64decode(t['rgba']);self.assertEqual(len(raw),t['width']*t['height']*4);self.assertLessEqual(max(t['width'],t['height']),512)
            if key.startswith('nav_') or key=='wordmark':self.assertTrue(any(raw[3::4]),key)
        self.assertTrue((LOCAL/'menu/frontend.json').is_file(),'U2 artwork preserved')

if __name__=='__main__':unittest.main()
