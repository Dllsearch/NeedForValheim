import sys,unittest,base64
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from export_hud import export
from project import config

class HudImportTests(unittest.TestCase):
    def test_original_catalog_alpha_and_canvas(self):
        data=export()
        u2=data['designs'][:11];mw=data['designs'][11:]
        self.assertEqual(len(u2),11)
        self.assertEqual(len(mw),11 if config().get('mw_root') else 0)
        self.assertEqual(len({d['fill']['rgba'] for d in u2}),11)
        for i,d in enumerate(u2):
            self.assertEqual(d['fill']['width'],70 if i==10 else 140)
            for part in ('fill','lines','needle','turboFill','turboLines'):
                t=d[part];pixels=base64.b64decode(t['rgba'])
                self.assertEqual(len(pixels),t['width']*t['height']*4)
                self.assertEqual(min(pixels[3::4]),0)
                self.assertGreater(max(pixels[3::4]),128)
        self.assertEqual(len({d['turboFill']['rgba'] for d in u2}),11)
        self.assertTrue(all(d['turboLines']['width']==72 for d in u2))
        for i,d in enumerate(mw):
            self.assertEqual(d['name'],f'MW {i+1:02d}')
            self.assertEqual(d['kind'],'mw-tach')
            self.assertAlmostEqual(d['needlePivot'],255/304)
            for part in ('fill','lines','needle','turboLines'):
                t=d[part];pixels=base64.b64decode(t['rgba'])
                self.assertEqual(len(pixels),t['width']*t['height']*4)
                self.assertLessEqual(max(t['width'],t['height']),256)
                self.assertEqual(min(pixels[3::4]),0)
                self.assertGreater(max(pixels[3::4]),128)
        if mw:self.assertEqual(data['mwNitro']['name'],'METER_BACKING2')
        self.assertEqual(data['nitro']['name'],'TACH_NOS_ALPHA')
        pixels=base64.b64decode(data['nitro']['rgba'])
        self.assertEqual(pixels[3],0) # no opaque black rectangle behind the arc
