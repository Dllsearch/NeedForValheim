"""Installed MW roster, original attribute links and reusable asset dependencies."""
import struct
from functools import lru_cache
from pathlib import Path
from mw_geometry import parse, leaf_ranges
from mw_vault import Vault, vlt_hash
from nfs_formats import TexturePack


class Source:
    def __init__(self,root):
        self.root=Path(root).resolve()
        self.vault=Vault(self.root/'GLOBAL/attributes.bin')
        data=(self.root/'GLOBAL/GlobalB.bun').read_bytes();self.cars=[]
        for tag,a,b in leaf_ranges(data,0,len(data)):
            if tag!=0x34600:continue
            if (b-a-8)%0xd0:raise ValueError('Invalid MW CarTypeInfo table')
            for p in range(a+8,b,0xd0):
                name=data[p:p+16].split(b'\0')[0].decode('ascii')
                self.cars.append(dict(car=name,usage=struct.unpack_from('<I',data,p+0x94)[0],
                                     manufacturer=data[p+0x40:p+0x50].split(b'\0')[0].decode('ascii'),
                                     rimRange=list(data[p+0x5c:p+0x5f])))
        self.vehicles={}
        for (cls,key),row in self.vault.rows.items():
            if self.vault.name(cls)!='pvehicle':continue
            name=self.vault.name(key);fields=self.vault.get('pvehicle',name);model=fields.get('MODEL')
            if isinstance(model,str) and model and (model not in self.vehicles or name==model.lower()):
                self.vehicles[model]=dict(collection=name,fields=fields)

    @lru_cache(maxsize=12)
    def meshes(self,folder):return parse(self.root/'CARS'/folder/'GEOMETRY.BIN')

    @lru_cache(maxsize=64)
    def pack(self,path):return TexturePack(self.root/path)

    def roster(self):return [r for r in self.cars if r['usage']==0]

    def attributes(self,car):
        vehicle=self.vehicles.get(car)
        if not vehicle:raise ValueError('No MW pvehicle dependency graph: '+car)
        pv=vehicle['fields'];result=dict(pvehicle=vehicle['collection'],pvehicleFields=pv,mass=pv.get('MASS'),ecar=self.vault.get('ecar',car.lower()))
        for cls in ('engine','engineaudio','transmission','chassis','tires','brakes','nos','induction'):
            refs=pv.get(cls,[])
            result[cls]=[dict(collection=r['collection'],fields=self.vault.get(cls,r['collection'])) for r in refs if isinstance(r,dict)]
        return result

    def audio(self,attributes):
        result=[]
        for row in attributes['engineaudio']:
            fields=row['fields'];accel=fields.get('0x2cee0a54');decel=fields.get('0x3a7c2f3d')
            banks=[x for x in fields.values() if isinstance(x,str) and x.lower().endswith(('.gin','.abk'))]
            if accel:
                for name in banks:
                    if Path(name).name!=name or not (self.root/'SOUND/ENGINE'/name).is_file():raise ValueError('Missing original MW sound dependency: '+name)
                result.append(dict(collection=row['collection'],accel=accel,decel=decel,banks=banks))
        if not result:raise ValueError('No original MW engine bank mapping')
        return result
