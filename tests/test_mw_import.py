import json
import math
import struct
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from mw_geometry import parse
from mw_wheel_fit import fit_arch
from export_mw_car import SPECS, source_folder
from mod_files import optional_assets, payload_files, sync_optional_assets
from project import LOCAL


def fixture(bad_index=False, bad_slot=False, effect=5):
    data = bytearray(struct.pack('<II', 0x80134010, 0))

    def chunk(tag, value, alignment=1):
        start = len(data) + 8
        padding = b'\x11' * ((-start) % alignment)
        data.extend(struct.pack('<II', tag, len(value) + len(padding)) + padding + value)

    header = bytearray(160)
    header[12] = 0x16
    struct.pack_into('<I', header, 16, 123)
    chunk(0x134011, header + b'CAR_KIT00_BODY_A\0', 16)
    chunk(0x134012, struct.pack('<II', 99, 0))
    groups = bytearray()
    for shader in (4, effect):
        g = bytearray(104)
        g[24] = 2 if bad_slot else 0
        struct.pack_into('<I', g, 48, shader)
        struct.pack_into('<II', g, 60, 3, 1)
        struct.pack_into('<I', g, 92, 3)
        groups.extend(g)
    chunk(0x134b02, groups, 16)
    chunk(0x134b03, struct.pack('<6H', 0, 1, 2, 0, 1, 9 if bad_index else 2), 16)
    for number in (0, 1):
        vb = b''.join(struct.pack('<6fI2f', number + x, y, 0, 0, 0, 1, 0xffffffff, 0, 0)
                      for x, y in ((0, 0), (1, 0), (0, 1)))
        chunk(0x134b01, vb, 128)
    chunk(0x134c02, b'BODY\0')
    chunk(0x134c02, b'WINDOW\0')
    struct.pack_into('<I', data, 4, len(data) - 8)
    return data


class MWImport(unittest.TestCase):
    def test_arch_fit_recovers_centres_from_uneven_triangle_mesh(self):
        vertices=[]
        for i in range(81):
            angle=math.pi*(i/80)**1.3
            for radius in (.37,.43):
                vertices.append((1.62+radius*math.cos(angle),.9,.16+radius*math.sin(angle)))
        indices=[v for i in range(80) for v in (2*i,2*i+1,2*i+2,2*i+1,2*i+3,2*i+2)]
        fitted=fit_arch(dict(vertices=vertices,slices=[dict(materialName='BODY',indices=indices)]),1.38)
        self.assertAlmostEqual(fitted['axle'],1.62,delta=.003)
        self.assertAlmostEqual(fitted['hub'],.16,delta=.003)
        self.assertAlmostEqual(fitted['radius'],.37,delta=.003)
    def read_fixture(self, **kwargs):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'GEOMETRY.BIN'
            path.write_bytes(fixture(**kwargs))
            return parse(path)

    def test_material_streams_keep_local_index_bases(self):
        mesh = self.read_fixture()[0]
        self.assertEqual(mesh['indices'], [0, 1, 2, 3, 4, 5])
        self.assertEqual([s['materialName'] for s in mesh['slices']], ['BODY', 'WINDOW'])
        self.assertEqual(len(mesh['vertices']), 6)

    def test_reject_invalid_index_slot_and_shader(self):
        for args in ({'bad_index': True}, {'bad_slot': True}, {'effect': 999}):
            with self.subTest(args=args), self.assertRaises(ValueError):
                self.read_fixture(**args)

    def test_source_folder_rejects_traversal(self):
        with self.assertRaises(ValueError):
            source_folder(Path('.'), '../350Z')

    def test_catalog_payload_includes_only_generated_mw_model(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            local, output = root/'local', root/'output'
            (local/'cars').mkdir(parents=True)
            (local/'mw-test').mkdir()
            entry = dict(id='mw-test', sourceGame='mw', assets=dict(modelFile='mw-test/mw-test.json'))
            (local/'cars/cars.json').write_text(json.dumps([entry]))
            for name in ('mw-test.json', 'profile.json', 'provenance.json', 'GEOMETRY.BIN'):
                (local/'mw-test'/name).write_text('{}')
            # Avoid the radio compressor: only the MW optional tuple is exercised.
            from unittest.mock import patch
            with patch('mod_files.OPTIONAL', ()):
                sync_optional_assets(local, output)
                output.mkdir(exist_ok=True)
                for name in ('cars.json', 'body.json', 'NeedForValheim.dll'):
                    (output/name).write_text(json.dumps([entry]) if name == 'cars.json' else '{}')
                names = {p.as_posix() for p in payload_files(output)}
            self.assertEqual(names, {'cars.json','body.json','NeedForValheim.dll','mw-test/mw-test.json'})
            entry['assets']['modelFile'] = '../private.json'
            (local/'cars/cars.json').write_text(json.dumps([entry]))
            with self.assertRaises(ValueError):
                optional_assets(local/'cars/cars.json')

    def test_full_local_exports_keep_original_axles_and_tyres(self):
        from mw_payload import unpack_model
        root=LOCAL/'mw-export'
        if not (root/'cars.json').exists():self.skipTest('Full MW staging export absent')
        for profile in json.loads((root/'cars.json').read_text()):
            ident=profile['id'];model=unpack_model(json.loads((root/ident/(ident+'.json')).read_text()));provenance=json.loads((root/ident/'provenance.json').read_text())
            self.assertEqual(profile['sourceGame'],'mw');self.assertEqual(provenance['unresolvedTextures'],[])
            # A misplaced source glass pivot once expanded Cayman to 9.7 m wide
            # and put its hull 4.1 m below the tyre contact plane.
            rig=profile['rig'];self.assertGreater(rig['boxCenterY']-rig['boxSizeY']/2,.12)
            self.assertLess(rig['boxSizeX'],4)
            self.assertEqual(model['wheelVisualLift'],0)
            self.assertFalse(any('_DAMAGE' in p['name'] for p in model['parts']))
            offsets=provenance['attributes']['ecar']['TireOffsets']
            for axle,index in enumerate((0,2)):
                self.assertAlmostEqual(profile['rig']['frontAxleZ'] if axle==0 else -profile['rig']['rearAxleZ'],offsets[index][0])
                wheels=[v for p in model['parts'] if p['group']=='wheel' and p['axle']==('rear' if axle else 'front') and model['materials'][p['material']]['wheelRubber'] for v in p['vertices']]
                self.assertAlmostEqual(max(math.hypot(v['y'],v['z']) for v in wheels),offsets[index][3],places=4)
                self.assertAlmostEqual(max(v['x'] for v in wheels)-min(v['x'] for v in wheels),provenance['tyreWidths'][axle],places=4)
            for part in model['parts']:self.assertTrue(all(0<=i<len(part['vertices']) for i in part['indices']))

    def test_every_available_car_lod_a_part_is_exported(self):
        from mw_source import Source
        from project import mw_root
        root=LOCAL/'mw-export'
        if not (root/'decal-manifest.json').exists():self.skipTest('Full native decal export absent')
        source=Source(mw_root())
        for row in source.roster():
            car=row['car'];ident='mw-'+car.lower();model=json.loads((root/ident/(ident+'.json')).read_text());style=json.loads((root/'styling'/(ident+'.json')).read_text());decal=root/'decals'/(ident+'.json')
            decal_data=json.loads(decal.read_text()) if decal.is_file() else None
            names=[p['name'] for data in (model,style,*([decal_data] if decal_data else [])) for p in data['parts']]
            for mesh in source.meshes(car):
                name=mesh['name']
                if name.endswith('_A') and not any(t in name for t in ('_DAMAGE','_DRIVER')):
                    self.assertTrue(any(name+'_' in n for n in names),'Missing native part '+name)
                    if '_DECAL_' in name:
                        from mw_payload import unpack_model
                        unpack_model(decal_data)
                        native={p['name']:p for p in decal_data['parts']}
                        lift=json.loads((root/ident/'provenance.json').read_text())['bodyLift']
                        for number,section in enumerate(mesh['slices']):
                            if not section['indices']:continue
                            part=native[name+'_'+str(number)]
                            for old,converted in zip(sorted(set(section['indices'])),part['vertices']):
                                from mw_assets import body_position
                                v=body_position(mesh,mesh['vertices'][old])
                                self.assertAlmostEqual(converted['x'],v[1],places=5);self.assertAlmostEqual(converted['y'],v[2]+lift,places=5);self.assertAlmostEqual(converted['z'],v[0],places=5)


    def test_vlt_known_hashes_and_truncated_archive(self):
        from mw_vault import vlt_hash,Vault
        self.assertEqual(vlt_hash('engine'),0xf1f5fbc7)
        self.assertEqual(vlt_hash('TireOffsets'),0x5e39d209)
        self.assertEqual(vlt_hash('ExtraRearTireOffset'),0xb1304fde)
        self.assertEqual(vlt_hash('0xb1304fde'),0xb1304fde)
        with tempfile.TemporaryDirectory() as td:
            path=Path(td)/'bad.bin';path.write_bytes(b'VPAK')
            with self.assertRaises((ValueError,struct.error)):Vault(path)


if __name__ == '__main__':
    unittest.main()
