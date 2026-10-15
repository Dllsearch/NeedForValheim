"""Blender --background --python tools/render_vehicle.py -- <id> [options]."""
import argparse
import base64
import json
import sys
import zlib
from pathlib import Path

sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
import numpy as np
from mathutils import Vector
from vehicle_preview import load_vehicle,instances


def main():
    base=Path(__file__).resolve().parents[1]
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('car')
    parser.add_argument('--root',type=Path,default=base/'.local')
    parser.add_argument('--output',type=Path,default=base/'artifacts')
    parser.add_argument('--open',type=float,default=0,dest='opening')
    parser.add_argument('--door-style',choices=('REG','SUICIDE','SCISSOR'),default='REG')
    parser.add_argument('--layout',type=int,choices=range(4),default=2)
    parser.add_argument('--engine',type=int,choices=range(3),default=0)
    parser.add_argument('--exhaust',type=int,choices=range(1,11),default=8)
    parser.add_argument('--view',choices=('all','front','rear','side','wheel'),default='all')
    parser.add_argument('--samples',type=int,default=24)
    parser.add_argument('--width',type=int,default=1200)
    parser.add_argument('--paint',type=Path,help='optional composed body-paint PNG for any vehicle')
    parser.add_argument('--audio',default='1,1,1,1,1,1',help='component choice per trunk socket, comma separated; 0 selects its cover')
    parser.add_argument('--save-blend',action='store_true',help='save an editable scene with embedded textures')
    parser.add_argument('--camera',type=float,nargs=3,metavar=('X','Y','Z'),help='inspection camera position in vehicle coordinates')
    parser.add_argument('--target',type=float,nargs=3,metavar=('X','Y','Z'),help='inspection target in vehicle coordinates')
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    if not 0<=args.opening<=1:parser.error('--open must be between 0 and 1')
    try:audio_options=[int(value) for value in args.audio.split(',')]
    except ValueError:parser.error('--audio requires comma-separated integers')
    if len(audio_options)>6 or any(value<0 for value in audio_options):parser.error('--audio accepts up to six nonnegative choices')
    args.root=args.root.resolve();args.output=args.output.resolve()
    profile,model=load_vehicle(args.root,args.car.lower())
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    images=[]
    for texture in model['textures']:
        data=zlib.decompress(base64.b64decode(texture['rgbaDeflate']),-15) if texture.get('rgbaDeflate') else base64.b64decode(texture['rgba'])
        if len(data)!=texture['width']*texture['height']*4:raise ValueError('Invalid texture: '+texture['name'])
        image=bpy.data.images.new(texture['name'],width=texture['width'],height=texture['height'],alpha=True)
        pixels=np.frombuffer(data,dtype=np.uint8).reshape(texture['height'],texture['width'],4)[::-1].astype(np.float32)/255
        image.pixels.foreach_set(pixels.ravel());image.pack();images.append(image)
    materials=[]
    paint=bpy.data.images.load(str(args.paint.resolve())) if args.paint else None
    for source in model['materials']:
        material=bpy.data.materials.new(source['name']);material.use_nodes=True
        shader=material.node_tree.nodes.get('Principled BSDF')
        shader.inputs['Base Color'].default_value=source['color']
        shader.inputs['Metallic'].default_value=source['metallic']
        shader.inputs['Roughness'].default_value=1-source['smoothness']
        if source['texture']>=0:
            node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=images[source['texture']]
            multiply=material.node_tree.nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1
            multiply.inputs[2].default_value=source['color'];material.node_tree.links.new(node.outputs['Color'],multiply.inputs[1]);material.node_tree.links.new(multiply.outputs[0],shader.inputs['Base Color'])
            if source.get('alphaMode') in ('blend','mask'):material.node_tree.links.new(node.outputs['Alpha'],shader.inputs['Alpha'])
        if source.get('alphaMode')=='blend' or source['name'].startswith(('471a1dca','1ff8c329')):
            is_glass=source['name'].startswith(('471a1dca','1ff8c329')) or source['color'][3]<.9
            shader.inputs['Alpha'].default_value=min(source['color'][3],.32) if is_glass else source['color'][3]
            if is_glass:shader.inputs['Transmission Weight'].default_value=.2
        if paint and source.get('bodyPaint'):
            for link in list(shader.inputs['Base Color'].links):material.node_tree.links.remove(link)
            node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=paint
            material.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color'])
        materials.append(material)
    bounds=[];count=0
    for part,points,normals in instances(profile,model,args.layout,args.engine,args.exhaust,args.opening,args.door_style,audio_options):
        bounds.extend(points);count+=1
        convert=lambda v:(v[0],v[2],v[1])
        mesh=bpy.data.meshes.new(part['name']);mesh.from_pydata([convert(v) for v in points],[],[part['indices'][i:i+3][::-1] for i in range(0,len(part['indices']),3)]);mesh.update()
        uv=mesh.uv_layers.new()
        for loop in mesh.loops:
            value=part['uv'][loop.vertex_index];uv.data[loop.index].uv=value['u'],value['v']
        for polygon in mesh.polygons:polygon.use_smooth=True
        mesh.normals_split_custom_set_from_vertices([convert(v) for v in normals]);mesh.materials.append(materials[part['material']])
        obj=bpy.data.objects.new(part['name'],mesh);bpy.context.collection.objects.link(obj)
    if not bounds:raise ValueError('No visible geometry: '+args.car)
    low=[min(v[i] for v in bounds) for i in range(3)];high=[max(v[i] for v in bounds) for i in range(3)]
    center=Vector(((low[0]+high[0])/2,(low[2]+high[2])/2,(low[1]+high[1])/2))
    length=max(high[2]-low[2],2);scale=length/4.5
    bpy.ops.mesh.primitive_plane_add(size=max(200,length*8),location=(0,0,low[1]-.01))
    ground=bpy.data.materials.new('Ground');ground.diffuse_color=(.12,.14,.16,1);bpy.context.object.data.materials.append(ground)
    for location,energy in (((2,3,6),1700),((-4,-3,4),1200),((3,-4,3),850)):
        bpy.ops.object.light_add(type='AREA',location=center+Vector(location)*scale);bpy.context.object.data.energy=energy*scale*scale;bpy.context.object.data.size=5*scale
    bpy.ops.object.camera_add();camera=bpy.context.object;camera.data.lens=48
    scene=bpy.context.scene;scene.camera=camera;scene.render.engine='CYCLES';scene.cycles.samples=args.samples
    scene.render.resolution_x=args.width;scene.render.resolution_y=round(args.width*2/3);scene.render.resolution_percentage=100
    scene.world.color=(.2,.2,.2)
    args.output.mkdir(parents=True,exist_ok=True)
    views=('front','rear','side','wheel') if args.view=='all' else (args.view,)
    for view in views:
        target=center.copy();camera.data.type='PERSP'
        direction={'front':(-6,7,2.5),'rear':(6,-7,2.5),'side':(-7,0,.3),'wheel':(-2,2,.6)}[view]
        if view=='wheel':
            rig=profile['rig'];hub=model.get('previewHubHeight',rig['wheelRadius'])+model.get('wheelVisualLift',0)
            target=Vector((-rig['trackWidth'],rig['frontAxleZ']+model.get('frontWheelVisualOffset',0),hub))
            camera.location=target+Vector(direction)*rig['wheelRadius']*2
        else:camera.location=target+Vector(direction)*scale
        if view=='side':camera.data.type='ORTHO';camera.data.ortho_scale=length*1.2
        if args.target:target=Vector((args.target[0],args.target[2],args.target[1]))
        if args.camera:camera.location=Vector((args.camera[0],args.camera[2],args.camera[1]))
        camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(args.output/f'{args.car}-{view}.png');bpy.ops.render.render(write_still=True)
    if args.save_blend:bpy.ops.wm.save_as_mainfile(filepath=str(args.output/f'{args.car}-preview.blend'))
    (args.output/f'{args.car}-scene.json').write_text(json.dumps(dict(vehicle=args.car,sections=count,bounds=[low,high],opening=args.opening,views=list(views)),indent=2))


if __name__=='__main__':main()
