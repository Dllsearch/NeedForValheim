import sys
import unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from export_geometry import restore_mesh_name,parse
from nfs_formats import bin_hash
from project import LOCAL

class GeometryNames(unittest.TestCase):
    def test_separate_headlamps_at_both_header_lengths(self):
        for car in ('IMPREZAWRX','LANCEREVO8'):
            for side in ('LEFT','RIGHT'):
                for lod in 'ABCD':
                    full=f'{car}_KIT00_HEADLIGHT_{side}_{lod}'
                    for limit in (24,27):
                        self.assertEqual(restore_mesh_name(full[:limit],bin_hash(full)),full)
    def test_keep_complete_and_unknown_names(self):
        full='MIATA_KIT00_HEADLIGHT_RIGHT_A'
        self.assertEqual(restore_mesh_name(full,bin_hash(full)),full)
        self.assertEqual(restore_mesh_name('CUSTOM_LONG_MESH',bin_hash('CUSTOM_LONG_MESH_A')),'CUSTOM_LONG_MESH_A')
        self.assertEqual(restore_mesh_name('IMPREZAWRX_KIT00_HEADLIGHT_',123),'IMPREZAWRX_KIT00_HEADLIGHT_')
    @unittest.skipUnless((LOCAL/'nfs-assets/CARS/IMPREZAWRX/GEOMETRY.BIN').exists(),'local source absent')
    def test_distinct_original_stock_lamp_geometry(self):
        for car in ('IMPREZAWRX','LANCEREVO8'):
            parts=parse(LOCAL/f'nfs-assets/CARS/{car}/GEOMETRY.BIN')
            lamps=[next(p for p in parts if p['name']==f'{car}_KIT00_HEADLIGHT_{side}_A') for side in ('LEFT','RIGHT')]
            self.assertNotEqual(lamps[0]['key'],lamps[1]['key'])
            self.assertGreater(sum(v[1] for v in lamps[0]['vertices']),0)
            self.assertLess(sum(v[1] for v in lamps[1]['vertices']),0)

if __name__=='__main__':unittest.main()
