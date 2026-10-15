"""Strict portable expansion of MW schema-3 packed geometry (also used by QA)."""
import base64,struct,zlib

def unpack_model(model):
 for part in model['parts']:
  if 'packedGeometry' not in part:continue
  count=part['vertexCount'];indices=part['indexCount'];expected=count*32+indices*4
  if not 3<=count<=65535 or not 3<=indices<=393210 or indices%3:raise ValueError('Packed MW dimensions')
  stream=zlib.decompressobj(-15);data=stream.decompress(base64.b64decode(part['packedGeometry']),expected+1)
  if len(data)!=expected or not stream.eof or stream.unused_data:raise ValueError('Packed MW length')
  values=list(struct.iter_unpack('<8f',data[:count*32]));part['vertices']=[dict(zip(('x','y','z'),v[:3])) for v in values];part['normals']=[dict(zip(('x','y','z'),v[3:6])) for v in values];part['uv']=[dict(zip(('u','v'),v[6:8])) for v in values]
  part['indices']=list(struct.unpack_from('<'+str(indices)+'I',data,count*32))
 return model
