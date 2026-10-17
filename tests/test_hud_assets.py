"""Check the exported tachometers against the actual local game artwork."""
import base64,json,sys,unittest
from pathlib import Path
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from project import LOCAL,mw_root
from nfs_formats import TexturePack,bin_hash

@unittest.skipUnless((LOCAL/'hud/hud.json').is_file(),'Local HUD import required')
class HudAssets(unittest.TestCase):
    @classmethod
    def setUpClass(cls):cls.data=json.loads((LOCAL/'hud/hud.json').read_text())
    def pixels_equal(self,texture,image):
        self.assertEqual((texture['width'],texture['height']),image.size)
        self.assertEqual(base64.b64decode(texture['rgba']),image.convert('RGBA').tobytes())
    def test_u2_needles_retain_authored_tail_and_point_outward(self):
        for i,design in enumerate(self.data['designs'][:11]):
            with self.subTest(style=i):
                pack=TexturePack(LOCAL/f'nfs-assets/GLOBAL/HUD_CustomTextures_{i:02d}.bin')
                art=pack.decode(bin_hash(f'3RDPERSON_RPMDIAL_CUSTOM_{i:02d}'))[1].convert('RGBA')
                self.pixels_equal(design['needle'],art.crop((0,56,16,128)).transpose(Image.Transpose.ROTATE_180))
                self.assertAlmostEqual(design['needlePivot']*72,63)
    def test_mw_nos_retains_native_meter_art(self):
        if len(self.data['designs'])==11:self.skipTest('MW source not configured')
        _,art,_=TexturePack(mw_root()/'GLOBAL/HUDTEXRACE.BIN').decode(bin_hash('METER_BACKING2'))
        art=art.convert('RGBA');art.thumbnail((256,256),Image.Resampling.LANCZOS)
        self.pixels_equal(self.data['mwNitro'],art)

    def test_mw_uses_labeled_native_tachometers_for_all_variants(self):
        if len(self.data['designs'])==11:self.skipTest('MW source not configured')
        self.assertEqual(len(self.data['designs']),22)
        for i,design in enumerate(self.data['designs'][11:]):
            with self.subTest(style=i):
                pack=TexturePack(mw_root()/f'GLOBAL/HUDS_Custom_{i:02d}.bin')
                self.assertEqual(design['kind'],'mw-tach')
                for field,prefix in [('fill','TACH_FILL_'),('lines','8000_LINES_')]:
                    name,art,_=pack.decode(bin_hash(f'{prefix}{i:02d}'))
                    art=art.convert('RGBA');art.thumbnail((256,256),Image.Resampling.LANCZOS)
                    self.assertEqual(design[field]['name'],name)
                    self.pixels_equal(design[field],art)
                self.assertAlmostEqual(design['needlePivot']*304,255)

if __name__=='__main__':unittest.main()
