"""Blender --background --python tools/render_mw_fleet.py -- [CAR] [stock|styled]."""
import bpy,json,math,sys,base64,zlib
from pathlib import Path
from mathutils import Vector
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parent))
from mw_payload import unpack_model
ROOT=Path(__file__).resolve().parents[1]/'.local/mw-export'
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
profiles=json.loads((ROOT/'cars.json').read_text());mode=args[1] if len(args)>1 else 'stock'
for profile in profiles:
 if args and args[0].lower() not in (profile['id'],profile['id'].removeprefix('mw-'),'all'):continue
 ident=profile['id'];rig=profile['rig'];model=unpack_model(json.loads((ROOT/profile['assets']['modelFile']).read_text()))
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.data.orphans_purge(do_recursive=True)
 parts=[(p,model) for p in model['parts']];kit=None
 if mode=='styled':
  library=unpack_model(json.loads((ROOT/'styling'/(ident+'.json')).read_text()));choices={}
  for category in ('Bodykit','Hood','Spoiler','Roof','Rims','Brakes'):
   options=[o for o in library['styleOptions'] if o['category']==category]
   if options:choices[category]=options[-1]
  kit=choices.get('Bodykit');hide={r.split('|',1)[1] for r in library['styleReplacements'] if r.split('|',1)[0] in choices}
  parts=[(p,d) for p,d in parts if p['name'] not in hide]+[(p,library) for p in library['parts'] if p['option'] in {c+':'+str(o['id']) for c,o in choices.items()}]
 parts=[(p,d) for p,d in parts if p.get('optionGroup')!='spoiler-support' or mode=='styled' and 'Spoiler' in choices]
 materials={};images={}
 def material(data,index):
  key=(id(data),index)
  if key in materials:return materials[key]
  m=data['materials'][index];mat=bpy.data.materials.new(m['name']);mat.use_nodes=True;mat.diffuse_color=m['color'];shader=mat.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=m['color'];shader.inputs['Metallic'].default_value=m['metallic'];shader.inputs['Roughness'].default_value=1-m['smoothness']
  if m.get('alphaMode') in ('blend','mask'):shader.inputs['Alpha'].default_value=m['color'][3];mat.surface_render_method='DITHERED'
  if m['texture']>=0:
   tk=(id(data),m['texture']);t=data['textures'][m['texture']]
   if tk not in images:
    raw=base64.b64decode(t.get('rgba',t.get('rgbaDeflate')));raw=zlib.decompress(raw,-15) if 'rgbaDeflate' in t else raw
    image=bpy.data.images.new(t['name'],t['width'],t['height'],alpha=True);pixels=np.frombuffer(raw,np.uint8).reshape(t['height'],t['width'],4)[::-1].astype(np.float32)/255;image.pixels.foreach_set(pixels.ravel());images[tk]=image
   node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=images[tk];mat.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color'])
   if m.get('alphaMode') in ('blend','mask'):mat.node_tree.links.new(node.outputs['Alpha'],shader.inputs['Alpha'])
  materials[key]=mat;return mat
 for p,data in parts:
  mesh=bpy.data.meshes.new(p['name']);mesh.from_pydata([(v['x'],v['z'],v['y']) for v in p['vertices']],[],[p['indices'][i:i+3][::-1] for i in range(0,len(p['indices']),3)]);mesh.update();uv=mesh.uv_layers.new()
  for loop in mesh.loops:v=p['uv'][loop.vertex_index];uv.data[loop.index].uv=(v['u'],v['v'])
  for poly in mesh.polygons:poly.use_smooth=True
  mesh.normals_split_custom_set_from_vertices([(v['x'],v['z'],v['y']) for v in p['normals']]);mesh.materials.append(material(data,p['material']))
  if p['group']=='body':positions=[(0,0,0,0,1)]
  else:
   positions=[]
   for axle in (0,1):
    if p.get('axle') and p['axle']!=('rear' if axle else 'front'):continue
    scale=kit.get('rearTyreScale' if axle else 'frontTyreScale',1) if kit else 1;offset=kit.get('rearWheelOffset' if axle else 'frontWheelOffset',0) if kit else 0
    width=rig.get('rearTyreWidth' if axle else 'tyreWidth',0);track=rig.get('rearTrackWidth',rig['trackWidth']) if axle else rig['trackWidth'];track+=offset-width*(scale-1)/2
    for side in (-1,1):
     y=-rig['rearAxleZ'] if axle else rig['frontAxleZ'];z=rig.get('rearWheelHeight',rig['wheelHeight']) if axle else rig['wheelHeight'];positions.append((side*track,y,z,side,scale if p['group']=='wheel' else 1))
     if axle and rig.get('extraRearAxleOffset',0):positions.append((side*track,y+rig['extraRearAxleOffset'],z,side,scale if p['group']=='wheel' else 1))
  for x,y,z,side,scale in positions:
   obj=bpy.data.objects.new(p['name'],mesh);bpy.context.collection.objects.link(obj);obj.location=(x,y,z);obj.scale.x=scale
   if side<0:obj.rotation_euler.z=math.pi
 bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;mat=bpy.data.materials.new('Ground');mat.diffuse_color=(.12,.13,.15,1);ground.data.materials.append(mat)
 for location,energy in (((-3,4,7),1800),((4,-3,5),1500)):
  bpy.ops.object.light_add(type='AREA',location=location);bpy.context.object.data.energy=energy;bpy.context.object.data.size=6
 target=Vector((0,0,max(.6,rig['boxCenterY']*.75)));extent=max(rig['boxSizeZ'],4.4)
 bpy.ops.object.camera_add(location=(-extent*1.5,extent*.75,extent*.7));camera=bpy.context.object;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=extent*1.35
 scene=bpy.context.scene;scene.camera=camera;scene.render.engine='CYCLES';scene.cycles.samples=8;scene.render.resolution_x=640;scene.render.resolution_y=400;scene.render.resolution_percentage=100;scene.world.color=(.25,.25,.25)
 output=ROOT/'qa'/mode/(ident+'.png');output.parent.mkdir(parents=True,exist_ok=True);scene.render.filepath=str(output);bpy.ops.render.render(write_still=True);print('MW_QA',ident,mode,flush=True)
