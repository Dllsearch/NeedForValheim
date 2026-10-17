"""Model-neutral preview selection and transforms; usable without Blender."""
import json
import math
from pathlib import Path
from mw_payload import unpack_model


def load_vehicle(root, ident):
    root=Path(root)
    path=root/ident/'profile.json'
    if path.exists():
        profile=next(p for p in json.loads(path.read_text(encoding='utf-8-sig')) if p['id']==ident)
    else:
        catalog=root/'cars/cars.json'
        profiles=json.loads(catalog.read_text(encoding='utf-8-sig')) if catalog.exists() else []
        profile=next((p for p in profiles if p['id']==ident),None)
        if profile is None and ident=='350z':
            from vehicle_preset import profile as builtin
            profile=builtin()
        if profile is None:
            raise ValueError('No generated profile for vehicle: '+ident)
    model_path=root/profile['assets']['modelFile']
    if profile['assets']['modelFile']=='body.json' and not model_path.exists():model_path=root/'model/body.json'
    if not model_path.exists():model_path=root/'mw-export'/profile['assets']['modelFile']
    return profile,unpack_model(json.loads(model_path.read_text(encoding='utf-8-sig')))


def xyz(v):return tuple(v[k] for k in ('x','y','z'))


def apply_styles(root,ident,model,choices):
    """Use the same replacement targets/material tables as the runtime garage."""
    if not choices:return model
    path=Path(root)/'styling'/(ident+'.json')
    if not path.exists():path=Path(root)/'mw-export/styling'/(ident+'.json')
    library=unpack_model(json.loads(path.read_text(encoding='utf-8-sig')))
    for choice in choices:
        category,number=choice.split(':');number=int(number)
        if not any(o['category']==category and o['id']==number for o in library['styleOptions']):raise ValueError('Unknown part '+choice)
        removed={r.split('|',1)[1] for r in library['styleReplacements'] if r.startswith(category+'|')}
        model['parts']=[p for p in model['parts'] if p['name'] not in removed]
    material_offset=len(model['materials']);texture_offset=len(model['textures'])
    model['textures']+=library['textures']
    model['materials']+=[{**m,'texture':m['texture']+texture_offset if m['texture']>=0 else -1} for m in library['materials']]
    model['parts']+=[{**p,'optionGroup':'','material':p['material']+material_offset} for p in library['parts'] if p.get('option') in choices]
    for choice in choices:
        if choice.startswith('Bodykit:'):
            model['_wheelFit']=next(o for o in library['styleOptions'] if o['category']=='Bodykit' and o['id']==int(choice.split(':')[1]))
    return model


def rotate(v,q):
    x,y,z,w=(q[k] for k in ('x','y','z','w'))
    a,b,c=v
    # Unit-quaternion vector rotation, identical in Unity and Blender coordinates.
    tx,ty,tz=2*(y*c-z*b),2*(z*a-x*c),2*(x*b-y*a)
    return (a+w*tx+y*tz-z*ty,b+w*ty+z*tx-x*tz,c+w*tz+x*ty-y*tx)


def pose(v,animation,progress,normal=False):
    if not animation:return v
    frames=animation['frames'];position=max(0,min(1,progress))*(len(frames)-1)
    i=int(position);j=min(i+1,len(frames)-1);t=position-i
    a,b=frames[i],frames[j]
    av=[a['rotation'][k] for k in ('x','y','z','w')];bv=[b['rotation'][k] for k in ('x','y','z','w')]
    dot=sum(x*y for x,y in zip(av,bv))
    if dot<0:bv=[-v for v in bv];dot=-dot
    angle=math.acos(min(1,dot))
    weights=(1-t,t) if angle<.00001 else (math.sin((1-t)*angle)/math.sin(angle),math.sin(t*angle)/math.sin(angle))
    values=[weights[0]*x+weights[1]*y for x,y in zip(av,bv)]
    norm=math.sqrt(sum(x*x for x in values));q=dict(zip(('x','y','z','w'),(x/norm for x in values)))
    if normal:return rotate(v,q)
    pivot=xyz(animation['pivot']);offset=tuple(a['offset'][k]*(1-t)+b['offset'][k]*t for k in ('x','y','z'))
    turned=rotate(tuple(x-p for x,p in zip(v,pivot)),q)
    return tuple(p+d+x for p,d,x in zip(pivot,offset,turned))


def instances(profile,model,layout=2,engine=0,exhaust=8,opening=0,door_style='REG',audio_options=None):
    """Yield mesh sections with posed vertices and normals in Unity coordinates."""
    rig=profile['rig'];hub=model.get('previewHubHeight',rig['wheelRadius'])+model.get('wheelVisualLift',0)
    tracks={a['name']:a for a in model.get('animations',[])}
    roles=dict(leftDoor='LEFT_DOOR_'+door_style,rightDoor='RIGHT_DOOR_'+door_style,hood='HOOD_REG',trunk='TRUNK_REG')
    layouts=model.get('trunkLayouts',[])
    layout=min(layout,len(layouts)-1) if layouts else 0
    selected=dict(trunk=str(layout),engine=str(engine),exhaust=str(exhaust))
    def transformed(part,slot=None):
        points=[xyz(v) for v in part['vertices']];normals=[xyz(v) for v in part['normals']]
        if slot:
            basis=[xyz(slot[k]) for k in ('right','up','forward')];origin=xyz(slot['position'])
            def turn(v):return tuple(sum(v[i]*basis[i][j] for i in range(3)) for j in range(3))
            points=[tuple(a+b for a,b in zip(origin,turn(v))) for v in points];normals=[turn(v) for v in normals]
        animation=tracks.get(roles.get(part.get('movingPart'),''))
        return ([pose(v,animation,opening) for v in points],[pose(n,animation,opening,True) for n in normals])
    for part in model['parts']:
        group=part['group'];option=part.get('optionGroup','')
        if group=='audio' or option in ('styling','decal'):continue
        if option in selected and part['option']!=selected[option]:continue
        points,normals=transformed(part)
        if group=='body':yield part,points,normals;continue
        axles=[('front',rig['frontAxleZ']+model.get('frontWheelVisualOffset',0)),('rear',-rig['rearAxleZ']+model.get('rearWheelVisualOffset',0))]
        if rig.get('extraRearAxleOffset'):axles.append(('rear',-rig['rearAxleZ']+rig['extraRearAxleOffset']))
        for axle,z in axles:
            if part.get('axle') and part['axle']!=axle:continue
            track=rig.get('rearTrackWidth',0) or rig['trackWidth'] if axle=='rear' else rig['trackWidth']
            fit=model.get('_wheelFit',{});offset=fit.get('frontWheelOffset' if axle=='front' else 'rearWheelOffset',0)
            width_scale=fit.get('frontTyreScale' if axle=='front' else 'rearTyreScale',1)
            width=(rig.get('rearTyreWidth',0) or rig.get('tyreWidth',0)) if axle=='rear' else rig.get('tyreWidth',0)
            track+=offset-width*(width_scale-1)/2
            adjusted=[]
            for x,y,z in normals:
                normal=(x/width_scale,y,z);length=math.sqrt(sum(c*c for c in normal))
                adjusted.append(tuple(c/length for c in normal) if length>1e-8 else (0,1,0))
            axle_hub=hub+((rig.get('rearWheelHeight',0) or rig.get('wheelHeight',rig['wheelRadius']))-rig.get('wheelHeight',rig['wheelRadius']) if axle=='rear' else 0)
            for side in (-1,1):
                yield part,[(side*(track+x*width_scale),y+axle_hub,side*depth+z) for x,y,depth in points],[(side*x,y,side*z) for x,y,z in adjusted]
    if layouts:
        for index,slot in enumerate(layouts[layout]['sockets']):
            choices=[c for c in model.get('audioComponents',[]) if c['size']==slot['size']]
            choice=audio_options[index] if audio_options and index<len(audio_options) else 1
            component=choices[max(0,min(len(choices)-1,choice))] if choices else None
            if component:
                for part in model['parts']:
                    if part['group']=='audio' and part.get('option')==component['id']:
                        points,normals=transformed(part,slot);yield part,points,normals
