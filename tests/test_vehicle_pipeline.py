import json
import base64
import copy
import math
import sys
import unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from project import BASE,LOCAL
from vehicle_preview import load_vehicle,instances,pose,apply_styles
from vehicle_geometry import wheel_geometry,wheel_source_radius,badge_uv,BADGE_OVERLAY,source_attachment,painted_arch_width
from export_geometry import parse
from export_car import SPECS,source_name,pick
from showroom_assets import animations


class PipelineChecks(unittest.TestCase):
    def test_widebody_preview_moves_both_wheel_axles(self):
        profile,stock=load_vehicle(LOCAL,'miata');wide=apply_styles(LOCAL,'miata',copy.deepcopy(stock),['Bodykit:2'])
        def centres(model):return [sum(p[0] for p in points)/len(points) for part,points,_ in instances(profile,model) if part['group']=='wheel']
        before,after=centres(stock),centres(wide)
        self.assertEqual(len(before),len(after))
        self.assertTrue(all(abs(a-b)>.1 for a,b in zip(before,after)))

    def test_factory_spoilers_are_at_their_source_rear_socket(self):
        for ident in ('3000gt','rx7','skyline','supra'):
            _,model=load_vehicle(LOCAL,ident)
            spoilers=[p for p in model['parts'] if p['group']=='body' and '_SPOILER' in p['name']]
            self.assertTrue(spoilers,ident)
            for part in spoilers:
                self.assertLess(max(v['z'] for v in part['vertices']),-.9,part['name'])
                self.assertGreater(min(v['y'] for v in part['vertices']),.6,part['name'])

    def test_socket_translation_never_affects_normals(self):
        matrix=(1,0,0,0,0,1,0,0,0,0,1,0,-2,0,.8,1)
        self.assertEqual(source_attachment((.1,.3,.2),matrix),(-1.9,.3,1.0))
        self.assertEqual(source_attachment((0,0,1),matrix,normal=True),(0,0,1))
        self.assertEqual(source_attachment((1,0,0),matrix,normal=True,exhaust_axes=True),(0,0,-1))

    def test_widebody_offsets_match_original_arch_deltas(self):
        for ident in ('miata','supra','skyline'):
            profile,_=load_vehicle(LOCAL,ident);car=source_name(ident)
            source=parse(LOCAL/f'nfs-assets/CARS/{car}/GEOMETRY.BIN');stock=pick(source,car+'_KIT00_BODY')
            library=json.loads((LOCAL/f'styling/{ident}.json').read_text())
            for option in library['styleOptions']:
                if option['category']!='Bodykit' or not option['title'].startswith('Widebody'):continue
                kit=pick(source,f'{car}_KITW{option["sourceKit"]:02}_BODY')
                for field,axle in [('frontWheelOffset',profile['rig']['frontAxleZ']),('rearWheelOffset',-profile['rig']['rearAxleZ'])]:
                    self.assertAlmostEqual(option[field],painted_arch_width(kit,axle)-painted_arch_width(stock,axle),places=4)
                    self.assertGreater(option[field],0,ident+' '+option['title'])

    def test_spaced_single_letters_reverse_word_order(self):
        vertices=[];uv=[]
        for letter in range(3):
            x=letter*3
            vertices += [(-2,x,0),(-2,x+1,0),(-2,x+1,1),(-2,x,1)]
            uv += [dict(u=letter/3,v=0),dict(u=(letter+1)/3,v=0),dict(u=(letter+1)/3,v=1),dict(u=letter/3,v=1)]
        part=dict(vertices=vertices,normals=[(-1,0,0)]*12)
        section=dict(indices=[j for i in range(0,12,4) for j in (i,i+1,i+2,i+2,i+3,i)])
        result=badge_uv(part,section,uv)
        for letter in range(3):self.assertAlmostEqual(sum(result[i]['u'] for i in range(letter*4,letter*4+4))/4,(2.5-letter)/3)

    def test_h2_hood_spaced_letters_keep_hummer_order(self):
        mesh=pick(parse(LOCAL/'nfs-assets/CARS/HUMMER/GEOMETRY.BIN'),'HUMMER_KIT00_HOOD')
        section=next(s for s in mesh['slices'] if s['materialKey']==BADGE_OVERLAY)
        uv=[dict(u=u,v=1-v) for u,v in mesh['uv']];result=badge_uv(mesh,section,uv)
        for a,b in ((range(559,563),range(571,575)),(range(563,567),range(567,571)),(range(575,579),range(579,583))):
            for axis in ('u','v'):self.assertAlmostEqual(sum(result[i][axis] for i in a)/4,sum(uv[i][axis] for i in b)/4,delta=.002)

    def test_duplicated_triangle_vertices_reflect_as_one_badge(self):
        ring=[(-2,-1,1),(-2,0,2),(-2,1,1),(-2,0,0)]
        vertices=[v for i in range(4) for v in [(-2,0,1),ring[i],ring[(i+1)%4]]]
        part=dict(vertices=vertices,normals=[(-1,0,0)]*12)
        section=dict(indices=list(range(12)))
        uv=[dict(u=(v[1]+1)/2,v=v[2]/2) for v in vertices]
        result=badge_uv(part,section,uv)
        for before,after in zip(uv,result):
            self.assertAlmostEqual(after['u'],1-before['u'])
            self.assertAlmostEqual(after['v'],before['v'])

    def test_overlapping_word_seams_keep_escalade_halves_in_order(self):
        parts=parse(LOCAL/'nfs-assets/CARS/ESCALADE/GEOMETRY.BIN')
        for name,pairs in (
            ('ESCALADE_KIT00_DOOR_LEFT_A',((107,108,109,110),(111,112,113,114))),
            ('ESCALADE_KIT00_TRUNK_A',((146,147,148,149),(150,151,152,153)))):
            with self.subTest(part=name):
                mesh=pick(parts,name.removesuffix('_A'));section=next(s for s in mesh['slices'] if s['materialKey']==BADGE_OVERLAY)
                uv=[dict(u=u,v=1-v) for u,v in mesh['uv']];result=badge_uv(mesh,section,uv)
                for a,b in (pairs,pairs[::-1]):
                    self.assertAlmostEqual(sum(result[i]['v'] for i in a)/4,sum(uv[i]['v'] for i in b)/4,delta=.001)

    def test_split_word_reflects_order_between_atlas_rows(self):
        mesh=pick(parse(LOCAL/'nfs-assets/CARS/IMPREZAWRX/GEOMETRY.BIN'),'IMPREZAWRX_KIT00_TRUNK')
        section=next(s for s in mesh['slices'] if s['materialKey']==BADGE_OVERLAY)
        uv=[dict(u=u,v=1-v) for u,v in mesh['uv']];result=badge_uv(mesh,section,uv)
        left=list(range(83,87));right=list(range(87,91))
        for a,b in ((left,right),(right,left)):
            self.assertAlmostEqual(sum(result[i]['v'] for i in a)/4,sum(uv[i]['v'] for i in b)/4,places=4)
        for indices in (list(range(91,95)),list(range(95,99)),list(range(99,103))):
            self.assertAlmostEqual(sum(result[i]['v'] for i in indices)/4,sum(uv[i]['v'] for i in indices)/4,delta=.001)

    def test_imported_u2_hubs_share_the_source_body_frame(self):
        for ident,spec in SPECS.items():
            if spec.get('presetAssembler') or 'visualLift' in spec:continue
            profile,model=load_vehicle(LOCAL,ident);source=json.loads((LOCAL/ident/'provenance.json').read_text())
            with self.subTest(car=ident):
                self.assertTrue(model['suspensionHubAtRest'])
                rig=profile['rig'];front,rear=source['wheelLocators'][0],source['wheelLocators'][2]
                self.assertAlmostEqual(source['bodyLift']+front[2],rig['wheelHeight'],places=4)
                self.assertAlmostEqual(source['bodyLift']+rear[2],rig['rearWheelHeight'],places=4)
                self.assertAlmostEqual(model['previewHubHeight'],rig['wheelHeight'],places=4)
                self.assertAlmostEqual(abs(rear[1]),rig['rearTrackWidth'],places=4)

    def test_mw_stock_and_accessory_rotors_have_alpha_artwork(self):
        import zlib
        root=LOCAL/'mw-export'
        profiles=json.loads((root/'cars.json').read_text());self.assertEqual(len(profiles),46)
        for profile in profiles:
            for path in (root/profile['assets']['modelFile'],root/'styling'/(profile['id']+'.json')):
                model=json.loads(path.read_text());rotors=[p for p in model['parts'] if p.get('rotatesWithWheel')]
                if not rotors and path.parent.name!='styling':
                    # Six source traffic solids have no separate stock brakes.
                    self.assertFalse(any(p['group']=='brake' for p in model['parts']))
                    continue
                with self.subTest(car=profile['id'],file=path.name):
                    self.assertTrue(rotors)
                    for part in rotors:
                        material=model['materials'][part['material']];self.assertEqual(material['alphaMode'],'mask')
                        self.assertGreaterEqual(material['texture'],0)
                        t=model['textures'][material['texture']]
                        pixels=zlib.decompress(base64.b64decode(t['rgbaDeflate']),-15) if t.get('rgbaDeflate') else base64.b64decode(t['rgba'])
                        self.assertEqual(min(pixels[3::4]),0);self.assertEqual(max(pixels[3::4]),255)
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

    def test_preset_trunk_corrects_nissan_emblem_as_well_as_wordmark(self):
        from vehicle_preset import visual_uv
        mesh=pick(parse(LOCAL/'nfs-assets/CARS/350Z/GEOMETRY.BIN'),'350Z_KIT00_TRUNK')
        section=next(s for s in mesh['slices'] if s['materialKey']==BADGE_OVERLAY)
        result=visual_uv(mesh,section,'350Z_KIT00_TRUNK')
        self.assertAlmostEqual(result[1]['u'],mesh['uv'][7][0],delta=.001)
        self.assertAlmostEqual(result[7]['u'],mesh['uv'][1][0],delta=.001)

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
        root=LOCAL
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
