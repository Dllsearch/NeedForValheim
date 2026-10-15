"""Export original U2 vinyls into compressed, per-preset optional menu catalogs.

Keeps original per-car UV layouts, coverage, RGB colour channels and artist art.
Does not change models, MW imports, shared catalogs or build output.
"""
import argparse,base64,json,re,subprocess,sys,zlib
from pathlib import Path
from PIL import Image
import numpy as np
from asset_source import extract_matching
from nfs_formats import bin_hash,TexturePack
from project import BASE,LOCAL
from build_support import compile_cs,test_refs,runtime_config

COLOUR_MASKS={'TRIBAL','FLAME','WILD','LIGHTNG','TEAR','STRIPE','STRIPES','LIQUID','LIQUIDS','MODERN','MODERNS','TEARS','BODY','HOOD'}
CATEGORY={'TRIBAL':'Tribal','FLAME':'Flames','LIGHTNG':'Lightning','TEAR':'Tears','TEARS':'Tears','STRIPE':'Stripes','STRIPES':'Stripes','LIQUID':'Liquid','LIQUIDS':'Liquid','MODERN':'Modern','MODERNS':'Modern','WILD':'Wild','FLAG':'Flags','FLAGS':'Flags','ART':'Artwork','UNIQUE':'Artwork','TOP':'Artwork','CONTEST':'Artwork','BODY':'Body','HOOD':'Hood'}
def packed(data):return base64.b64encode(zlib.compress(data,6,wbits=-15)).decode('ascii')
def unpacked(data):return zlib.decompress(base64.b64decode(data),wbits=-15)

def export(car,decoder):
    extract_matching(LOCAL/'nfs-assets',lambda n:n==f'CARS/{car}/VINYLS.BIN')
    source=LOCAL/f'nfs-assets/CARS/{car}/VINYLS.BIN'
    if not source.is_file():return
    temp=LOCAL/'vinyl-decoded.json';subprocess.run(['dotnet',str(decoder),str(source),str(temp)],check=True)
    records={r['key']:r for r in json.loads(temp.read_text())}
    entries=[]
    prefix=car+'_'
    for key,record in records.items():
        name=record['name']
        if not name.startswith(prefix) or name.endswith('_MASK') or bin_hash(name)!=key:continue
        mask=records.get(bin_hash(name+'_MASK'))
        if mask is None:continue
        if (record['width'],record['height'])!=(mask['width'],mask['height']):raise ValueError('Coverage size mismatch: '+name)
        w,h=record['width'],record['height']
        if w!=512 or h!=512:continue
        rgb=np.frombuffer(unpacked(record['data']),np.uint8).reshape(h,w,4).copy()
        coverage=np.frombuffer(unpacked(mask['data']),np.uint8).reshape(h,w,4)[:,:,0]
        rgb[:,:,3]=coverage
        if not np.any(coverage):continue
        suffix=name[len(prefix):];group=suffix.split('_')[0]
        thumb=Image.fromarray(rgb,'RGBA').resize((64,64),Image.Resampling.LANCZOS)
        ident=100+(bin_hash(suffix)&0x3fffffff)
        entries.append(dict(id=ident,name=suffix,title=suffix.replace('_',' '),category=CATEGORY.get(group,'Sponsors'),
                            fixedColors=group not in COLOUR_MASKS,width=w,height=h,data=packed(rgb.tobytes()),thumbnail=packed(thumb.tobytes())))
    if len({e['id'] for e in entries})!=len(entries):raise ValueError('Vinyl id collision')
    folder='model' if car=='350Z' else 'h2' if car=='HUMMER' else car.lower()
    model=LOCAL/folder/('body.json' if car=='350Z' else 'h2.json' if car=='HUMMER' else car.lower()+'.json')
    preset=json.loads(model.read_text())['preset']
    if not re.fullmatch(r'[A-Za-z0-9_-]{1,100}',preset):raise ValueError('Unsafe preset')
    data=dict(schemaVersion=1,preset=preset,sourceGame='u2',entries=sorted(entries,key=lambda e:(e['category'],e['name'])))
    out=LOCAL/'menu'/f'vinyl-{preset}.json';out.parent.mkdir(exist_ok=True)
    out.write_text(json.dumps(data,separators=(',',':')))
    print(car,len(entries),'original vinyl variants',round(out.stat().st_size/1048576,1),'MiB',flush=True)
    return out

def main():
    parser=argparse.ArgumentParser();parser.add_argument('cars',nargs='*');args=parser.parse_args()
    refs,tfm,version=test_refs();decoder=BASE/'artifacts/vinyl-checks/vinyl-decode.dll'
    compile_cs(decoder,[BASE/'tools/vinyl_decode.cs'],refs);runtime_config(decoder,tfm,version)
    from mod_files import CARS
    for car in args.cars or ('350z','h2',*CARS):export(car.upper(),decoder)

if __name__=='__main__':main()
