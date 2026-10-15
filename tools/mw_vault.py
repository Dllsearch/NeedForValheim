"""Read-only MW PC VPAK/legacy AttribSys reader.

Layouts verified against NFSTools/VaultLib (MIT) and local attributes.bin.
Unknown types are retained as hex; no original file is patched or rewritten.
"""
import json
import struct
from pathlib import Path


def vlt_hash(name):
    if name.startswith('0x'):return int(name,16)
    data=name.encode('ascii');a=b=0x9e3779b9;c=0xabcdef00;mask=0xffffffff
    def mix(a,b,c):
        for target,shift,left in ((0,13,False),(1,8,True),(2,13,False),(0,12,False),(1,16,True),(2,5,False),(0,3,False),(1,10,True),(2,15,False)):
            if target==0:a=((a-b-c)&mask)^(c>>shift)
            elif target==1:b=(((b-c-a)&mask)^((a<<shift)&mask))&mask
            else:c=((c-a-b)&mask)^(b>>shift)
        return a&mask,b&mask,c&mask
    pos=0
    while len(data)-pos>=12:
        x,y,z=struct.unpack_from('<3I',data,pos);a=(a+x)&mask;b=(b+y)&mask;c=(c+z)&mask
        a,b,c=mix(a,b,c);pos+=12
    c=(c+len(data))&mask
    for i,x in enumerate(data[pos:]):
        if i<4:a=(a+(x<<(8*i)))&mask
        elif i<8:b=(b+(x<<(8*(i-4))))&mask
        else:c=(c+(x<<(8*(i-7))))&mask
    return mix(a,b,c)[2] if name else 0


KNOWN=('ecar pvehicle chassis engine engineaudio transmission tires brakes induction nos frontend '
       'WHEEL_BASE TORQUE FLYWHEEL_MASS SPEED_LIMITER '
       'TireOffsets TireSkidWidth TireSkidWidthKitScale KitWheelOffsetFront KitWheelOffsetRear '
       'ExtraRearTireOffset RideHeight CamberFront CamberRear CollectionName MODEL engineaudio engine chassis '
       'transmission tires brakes induction nos MASS MAX_RPM RED_LINE IDLE GEAR_RATIO '
       'GinsuAccel GinsuDecel MainRAMBankName MainROMBankName SweetnerRAMBankName '
       'EA::Reflection::Float EA::Reflection::Double EA::Reflection::Int32 EA::Reflection::UInt32 '
       'EA::Reflection::Int16 EA::Reflection::UInt16 EA::Reflection::UInt8 EA::Reflection::Int8 '
       'EA::Reflection::Bool EA::Reflection::Text Attrib::Types::Vector2 Attrib::Types::Vector3 '
       'Attrib::Types::Vector4 Attrib::RefSpec Attrib::StringKey').split()


def blocks(data):
    pos=0
    while pos+8<=len(data):
        tag,size=struct.unpack_from('<II',data,pos)
        if size<8 or pos+size>len(data):raise ValueError('Invalid VLT chunk range')
        yield tag,pos+8,pos+size
        pos+=size


class Vault:
    def __init__(self,path):
        data=Path(path).read_bytes()
        if data[:4]!=b'VPAK':raise ValueError('MW PC VPAK required')
        count,string_pos,string_size=struct.unpack_from('<3I',data,4)
        if count!=1:raise ValueError('Expected MW single-vault pack')
        name,bs,vs,bo,vo=struct.unpack_from('<5I',data,16)
        if bo+bs>len(data) or vo+vs>len(data):raise ValueError('Invalid VPAK streams')
        self.bin=bytearray(data[bo:bo+bs]);self.vlt=bytearray(data[vo:vo+vs])
        self.names={vlt_hash(s):s for s in KNOWN};self.classes={};self.rows={}
        _,ss=struct.unpack_from('<II',self.bin,0)
        for value in bytes(self.bin[8:ss]).split(b'\0'):
            try:s=value.decode('ascii')
            except UnicodeDecodeError:continue
            if s:self.names[vlt_hash(s)]=s
        chunks={tag:(a,b) for tag,a,b in blocks(self.vlt)}
        a,b=chunks[0x5074724e];target=self.bin
        for p in range(a,b,12):
            q,kind,index,dest=struct.unpack_from('<IHHI',self.vlt,p)
            if kind==0:break
            if kind==2:target=self.vlt if index==0 else self.bin
            elif kind in (1,3):
                if q+4>len(target) or kind==3 and dest>=len(self.bin):raise ValueError('Invalid VLT fixup')
                struct.pack_into('<I',target,q,dest if kind==3 else 0)
        a,b=chunks[0x4578704e];n=struct.unpack_from('<I',self.vlt,a)[0]
        exports=[struct.unpack_from('<5I',self.vlt,a+4+20*i) for i in range(n)]
        for key,typ,_,size,p in exports:
            if p+size>len(self.vlt):raise ValueError('Invalid VLT export')
            if typ!=0x5e970cbc:continue
            cls,reserve,num,ptr=struct.unpack_from('<4I',self.vlt,p)
            self.classes[cls]={}
            for i in range(num):
                f,t,off,size,cap,flags,align=struct.unpack_from('<IIHHHBB',self.bin,ptr+16*i)
                self.classes[cls][f]=dict(type=t,offset=off,size=size,capacity=cap,flags=flags,alignment=1<<align)
        for key,typ,_,size,p in exports:
            if typ!=0x8e112eb7:continue
            row,cls,parent,reserve,_,num,ntypes,layout=struct.unpack_from('<8I',self.vlt,p)
            fields={};defs=self.classes[cls]
            if layout:
                for f,definition in defs.items():
                    if definition['flags']&2:fields[f]=self.value(definition,self.bin,layout+definition['offset'])
            q=p+32+ntypes*4
            for i in range(num):
                f,val,ti,flags=struct.unpack_from('<IIHH',self.vlt,q+12*i);definition=defs[f]
                inline=definition['size']<=4 and not definition['flags']&1
                fields[f]=self.value(definition,self.vlt,q+12*i+4) if inline else self.value(definition,self.bin,val)
            self.rows[(cls,row)]=dict(parent=parent,fields=fields)

    def name(self,key):return self.names.get(key,f'0x{key:08x}')

    def value(self,definition,data,p,array=False):
        if p<0 or p+definition['size']>len(data):raise ValueError('VLT field outside stream')
        if definition['flags']&1 and not array:
            cap,num,stride,_=struct.unpack_from('<4H',data,p);q=p+8;items=[]
            if num>cap or cap>2048:raise ValueError('Invalid VLT array')
            for i in range(num):
                alignment=definition['alignment'];q=(q+alignment-1)&-alignment
                items.append(self.value(dict(definition,size=stride),data,q,True));q+=stride
            return items
        typ=self.name(definition['type']);size=definition['size']
        if typ.endswith('::Float'):return struct.unpack_from('<f',data,p)[0]
        if typ.endswith('::Double'):return struct.unpack_from('<d',data,p)[0]
        for suffix,fmt in (('Int32','i'),('UInt32','I'),('Int16','h'),('UInt16','H'),('UInt8','B'),('Int8','b'),('Bool','B')):
            if typ.endswith('::'+suffix):return struct.unpack_from('<'+fmt,data,p)[0]
        if typ=='EA::Reflection::Text':
            q=struct.unpack_from('<I',data,p)[0]
            if not q:return ''
            end=self.bin.find(0,q)
            if end<q:raise ValueError('Invalid VLT text pointer')
            return self.bin[q:end].decode('ascii',errors='replace')
        if 'Vector' in typ:return list(struct.unpack_from('<'+str(size//4)+'f',data,p))
        if typ=='Attrib::StringKey' and size==16:
            q=struct.unpack_from('<I',data,p+12)[0]
            if q:
                end=self.bin.find(0,q)
                if end<q:raise ValueError('Invalid VLT StringKey pointer')
                return self.bin[q:end].decode('ascii',errors='replace')
            return ''
        if typ=='Attrib::RefSpec':
            cls,key,_=struct.unpack_from('<3I',data,p);return dict(className=self.name(cls),collection=self.name(key))
        return dict(type=typ,hex=bytes(data[p:p+size]).hex())

    def get(self,cls,row,field=None):
        key=(vlt_hash(cls),vlt_hash(row));values={};seen=set()
        while key in self.rows:
            if key in seen:raise ValueError('VLT inheritance cycle')
            seen.add(key);r=self.rows[key]
            for f,v in r['fields'].items():values.setdefault(self.name(f),v)
            key=(key[0],r['parent'])
        return values.get(field) if field else values


if __name__=='__main__':
    import sys
    v=Vault(sys.argv[1]);print('classes',len(v.classes),'collections',len(v.rows))
    for cls in ('ecar','pvehicle','engineaudio','engine'):
        print(cls,[(v.name(row),v.get(cls,v.name(row))) for c,row in v.rows if v.name(c)==cls][:2])
