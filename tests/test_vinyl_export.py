import base64,json,struct,subprocess,sys,tempfile,unittest,zlib
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from project import BASE,LOCAL
from nfs_formats import TexturePack,bin_hash
from mod_files import payload_files

class VinylExport(unittest.TestCase):
    def test_indexed_palette_is_bgra_in_both_decoders(self):
        # Independent known colours catch a shared channel-order bug that parity cannot.
        key=bin_hash('TEST');palette=bytes([19,73,211,127,241,89,7,33])+bytes(1016)
        expected=bytes([211,73,19,127,7,89,241,33])
        raw=bytearray(bytes([0,1])+palette+bytes(12)+b'TEST'+bytes(20)+bytes(40));p=1062
        struct.pack_into('<I',raw,p,key);struct.pack_into('<HH',raw,p+0x20,2,1);raw[p+0x26]=8
        literals=bytearray()
        for i in range(0,len(raw),8):
            literals.append(0)
            if i==0:literals.append(0)
            literals.extend(raw[i:i+8])
        encoded=b'JDLZ'+bytes(4)+struct.pack('<II',len(raw),16+len(literals))+literals
        slot=struct.pack('<6I',key,40,len(encoded),len(raw),len(raw)+0x88-p,0)
        compressed=struct.pack('<4I',0xb3310000,32,0x33310003,24)+slot+encoded
        record=bytearray(124);q=12;record[q:q+4]=b'TEST';struct.pack_into('<I',record,q+24,key)
        struct.pack_into('<4I',record,q+0x24,0,2,2,1024);struct.pack_into('<HH',record,q+0x38,2,1);record[q+0x3e]=8
        uncompressed=struct.pack('<4I',0xb3310000,132,0x33310004,124)+record+struct.pack('<II',0x33320002,108+1026)+bytes([0x11])*108+bytes([0,1])+palette
        with tempfile.TemporaryDirectory() as temp:
            source=Path(temp)/'indexed.bin'
            for blob in (compressed,uncompressed):
                source.write_bytes(blob);_,image,_=TexturePack(source).decode(key)
                self.assertEqual(image.convert('RGBA').tobytes(),expected)
            decoder=BASE/'artifacts/vinyl-checks/vinyl-decode.dll'
            self.assertTrue(decoder.exists(),'build vinyl decoder before running export tests')
            source.write_bytes(compressed);out=Path(temp)/'decoded.json'
            subprocess.run(['dotnet',str(decoder),str(source),str(out)],check=True)
            entry=json.loads(out.read_text())[0]
            self.assertEqual(zlib.decompress(base64.b64decode(entry['data']),wbits=-15),expected)
    @unittest.skipUnless((LOCAL/'nfs-assets/CARS/350Z/VINYLS.BIN').exists(),'private source absent')
    def test_batch_decoder_matches_existing_original_decoder(self):
        decoder=BASE/'artifacts/vinyl-checks/vinyl-decode.dll'
        if not decoder.exists():self.skipTest('build vinyl decoder first')
        source=LOCAL/'nfs-assets/CARS/350Z/VINYLS.BIN'
        with tempfile.TemporaryDirectory() as temp:
            out=Path(temp)/'decoded.json';subprocess.run(['dotnet',str(decoder),str(source),str(out)],check=True)
            decoded={r['key']:r for r in json.loads(out.read_text())};pack=TexturePack(source)
            for name in ('350Z_TRIBAL_000A','350Z_TRIBAL_000A_MASK','350Z_ART_F_6','350Z_ART_F_6_MASK'):
                key=bin_hash(name);_,image,_=pack.decode(key)
                self.assertEqual(zlib.decompress(base64.b64decode(decoded[key]['data']),wbits=-15),image.convert('RGBA').tobytes(),name)
    def test_vinyl_catalogs_use_existing_asset_allowlist(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);(root/'menu').mkdir()
            for n in ('NeedForValheim.dll','body.json','cars.json'):(root/n).write_text('[]')
            (root/'menu/vinyl-TEST.json').write_text('{}');(root/'menu/VINYLS.BIN').write_bytes(b'private source')
            names={p.as_posix() for p in payload_files(root)}
            self.assertIn('menu/vinyl-TEST.json',names);self.assertNotIn('menu/VINYLS.BIN',names)

if __name__=='__main__':unittest.main()
