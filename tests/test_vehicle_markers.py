import sys,unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from vehicle_markers import neon_segments,purge_emitters
from nfs_formats import bin_hash
class VehicleMarkerTests(unittest.TestCase):
    def test_native_endpoint_pair_and_axis_conversion(self):
        def matrix(x,y,z):return [0.0]*12+[x,y,z,1.0]
        mesh={'sockets':{bin_hash('SIDE_NEON_0_START'):matrix(1,2,3),bin_hash('SIDE_NEON_0_END'):matrix(4,5,6)}}
        result=neon_segments(mesh,lambda v:dict(x=v[1],y=v[2]+.2,z=v[0]))
        self.assertEqual(result,[dict(start=dict(x=2,y=3.2,z=1),end=dict(x=5,y=6.2,z=4))])
    def test_missing_endpoint_never_fabricates_a_tube(self):
        self.assertEqual(neon_segments({'sockets':{bin_hash('FRONT_NEON_0_START'):[0]*16}},list),[])
    def test_unrelated_markers_are_ignored(self):
        self.assertEqual(neon_segments({'sockets':{bin_hash('NOS_PURGE_LEFT'):[0]*16}},list),[])
    def test_purge_uses_native_position_and_direction_separately(self):
        matrix=[1,0,0,0,0,1,0,0,0,.1,.99,0,2,3,4,1]
        result=purge_emitters({'sockets':{bin_hash('NOS_PURGE_LEFT'):matrix}},lambda v:list(v),lambda v:list(v))
        self.assertEqual(result,[dict(role='purge',position=[2,3,4],direction=[0,.1,.99])])
    def test_missing_purge_marker_retains_caller_fallback(self):
        self.assertEqual(purge_emitters({'sockets':{}},list,list),[])
if __name__=='__main__':unittest.main()
