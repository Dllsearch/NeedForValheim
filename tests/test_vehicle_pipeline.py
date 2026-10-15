import json
import base64
import copy
import math
import sys
import unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from project import BASE,LOCAL
from vehicle_preview import load_vehicle,instances,pose
from vehicle_geometry import wheel_geometry,wheel_source_radius,badge_uv,BADGE_OVERLAY
from export_geometry import parse
from export_car import SPECS,source_name,pick
from showroom_assets import animations


class PipelineChecks(unittest.TestCase):
    def test_rotated_badge_atlas_reflects_on_surface_not_global_u(self):
        part=dict(vertices=[(-2,-1,0),(-2,1,0),(-2,1,1),(-2,-1,1)],normals=[(-1,0,0)]*4)
        section=dict(indices=[0,1,2,2,3,0])
        uv=[dict(u=0,v=0),dict(u=0,v=1),dict(u=1,v=1),dict(u=1,v=0)]
        original=copy.deepcopy(part)
        result=badge_uv(part,section,uv)
        for before,after in zip(uv,result):
            self.assertAlmostEqual(after['u'],before['u'])
            self.assertAlmostEqual(after['v'],1-before['v'])
        self.assertEqual(part,original)

    def test_supra_badges_keep_transparency_and_outward_normals(self):
        profile,model=load_vehicle(LOCAL,'supra')
        self.assertTrue(any(p.get('lightRole')=='brakelight' for p in model['parts']))
        self.assertFalse(any(light['role']=='brakelight' for light in model.get('lights',[])))
        badges=[p for p in model['parts'] if p['group']=='body' and p['name'].startswith('SUPRA_KIT00_TRUNK') and model['materials'][p['material']]['name'].startswith(f'{BADGE_OVERLAY:08x}')]
        self.assertTrue(badges)
        for part in badges:
            material=model['materials'][part['material']]
            self.assertEqual(material['alphaMode'],'blend')
            pixels=base64.b64decode(model['textures'][material['texture']]['rgba'])
            self.assertEqual(min(pixels[3::4]),0)
            self.assertEqual(max(pixels[3::4]),255)
            for v,n in zip(part['vertices'],part['normals']):
                self.assertLess(v['z'],-2)
                self.assertLess(n['z'],-.8)

    def test_original_animation_banks_across_roster(self):
        for ident in SPECS:
            car=source_name(ident);path=LOCAL/f'nfs-assets/CARS/{car}/PARTS_ANIMATIONS.bin'
            with self.subTest(car=car):
                tracks=animations(path,.1,car)
                self.assertEqual(len(tracks),8)
                for track in tracks:
                    self.assertLess(math.dist(pose((.7,.4,-.6),track,0),(.7,.4,-.6)),.00001)
                    for frame in track['frames']:
                        self.assertAlmostEqual(sum(v*v for v in frame['rotation'].values()),1,places=5)

    def test_shared_wheel_fit_accepts_other_vehicle_dimensions(self):
        wheel=pick(parse(LOCAL/'nfs-assets/CARS/WHEELS/GEOMETRY_SPINNER.BIN'),'SPINNER_STYLE01_26_34')
        rubber={i for s in wheel['slices'] if s['materialKey']==0x78743ca1 for i in s['indices']}
        for radius,width in ((.30,.22),(.4364,.34)):
            vertices,normals=wheel_geometry(wheel,radius,width)
            self.assertAlmostEqual(max(math.hypot(vertices[i]['y'],vertices[i]['z']) for i in rubber),radius)
            self.assertAlmostEqual(max(vertices[i]['x'] for i in rubber)-min(vertices[i]['x'] for i in rubber),width)
            self.assertTrue(all(abs(sum(v*v for v in n.values())-1)<.00001 for n in normals))

    def test_generated_preview_selects_variants_and_keeps_audio_on_floor(self):
        root=BASE/'artifacts/unified-vehicles'
        for ident in SPECS:
            profile,model=load_vehicle(root,ident)
            with self.subTest(car=ident):
                closed=list(instances(profile,model,opening=0))
                opened=list(instances(profile,model,opening=1))
                self.assertEqual(len(closed),len(opened))
                moved=0
                for (part,a,_),(_,b,_) in zip(closed,opened):
                    if part.get('optionGroup')=='trunk' or part['group']=='audio':self.assertEqual(a,b)
                    if part.get('movingPart') and a!=b:moved+=1
                    if part.get('optionGroup')=='engine':self.assertEqual(part['option'],'0')
                    if part.get('optionGroup')=='exhaust':self.assertEqual(part['option'],'8')
                self.assertGreater(moved,0)


if __name__=='__main__':unittest.main()
