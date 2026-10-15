"""Shared original wheel, brake and exhaust mesh adaptation."""
import math
import numpy as np
from nfs_formats import bin_hash


BADGE_OVERLAY = 0x02a05578


def badge_uv(part, section, coords):
    """Reverse each badge's lateral mapping, including rotated atlas islands.

    Vehicle conversion retains the established chassis/driver handedness. Badge
    artwork must be reflected in the surface frame, not across the entire atlas.
    Source positions, outward normals and triangle winding remain unchanged.
    """
    result=[dict(value) for value in coords]
    islands=[]
    ids=section['indices']
    for offset in range(0,len(ids),3):
        group=set(ids[offset:offset+3]);joined=[other for other in islands if other & group]
        for other in joined:group|=other;islands.remove(other)
        islands.append(group)
    for island in islands:
        indices=sorted(island)
        normal=np.mean([part['normals'][i] for i in indices],axis=0)
        horizontal=1 if abs(normal[0])>=abs(normal[1]) else 0
        points=np.array([[part['vertices'][i][horizontal],part['vertices'][i][2],1] for i in indices])
        values=np.array([[coords[i]['u'],coords[i]['v']] for i in indices])
        coefficients,_,rank,_=np.linalg.lstsq(points,values,rcond=None)
        if rank<3:continue
        center=(points[:,0].min()+points[:,0].max())*.5
        mirrored=values+(2*(center-points[:,0]))[:,None]*coefficients[0]
        mirrored=np.clip(mirrored,values.min(axis=0),values.max(axis=0))
        for i,value in zip(indices,mirrored):result[i]=dict(u=float(value[0]),v=float(value[1]))
    return result


def badge_rows_uv(part,section,coords,rule):
    """Keep split atlas rows together when a word spans disconnected quads."""
    result=[dict(value) for value in coords]
    letters={i for i in section['indices'] if rule['height'][0]<part['vertices'][i][2]<rule['height'][1] and abs(part['vertices'][i][1])<rule['halfWidth']}
    upper={i for i in letters if part['uv'][i][1]<rule['splitV']};lower=letters-upper
    if len(upper)!=4 or len(lower)!=4:raise ValueError('Re-identify split badge atlas islands: '+part['name'])
    for i in letters:
        x,y,z=part['vertices'][i]
        j=min(lower if i in upper else upper,key=lambda j:sum((a-b)**2 for a,b in zip(part['vertices'][j],(x,-y,z))))
        result[i]=dict(coords[j])
    return result

def wheel_source_radius(part):
    # The INTERIOR slice contains a square atlas backing plane. Its diagonal
    # is not a tyre radius. Measure only vertices used by RUBBER triangles.
    ids={i for section in part['slices'] if section['materialKey']==0x78743ca1 for i in section['indices']}
    if not ids: raise ValueError('Wheel has no rubber surface to measure')
    radius=max(math.hypot(part['vertices'][i][0],part['vertices'][i][2]) for i in ids)
    if not math.isfinite(radius) or not .1 < radius < 1: raise ValueError('Invalid source tyre radius')
    return radius


def wheel_geometry(part,radius=.35,width=.28):
    ids={i for s in part['slices'] if s['materialKey']==0x78743ca1 for i in s['indices']}
    axial=[part['vertices'][i][1] for i in ids]
    source_width=max(axial)-min(axial)
    if not .05 < source_width < .5:raise ValueError('Invalid original tyre width')
    radial_scale=radius/wheel_source_radius(part)
    axial_scale=width/source_width
    outer_face=.165*radial_scale
    vertices=[{'x':outer_face-(v[1]-min(axial))*axial_scale,'y':v[2]*radial_scale,'z':v[0]*radial_scale} for v in part['vertices']]
    normals=[]
    for nx,ny,nz in part['normals']:
        n=(-ny/axial_scale,nz/radial_scale,nx/radial_scale)
        length=math.sqrt(sum(c*c for c in n))
        normals.append(dict(zip(('x','y','z'),(c/length for c in n))) if length else {'x':0,'y':1,'z':0})
    return vertices,normals


def wheel_finish_sections(part,section):
    if section['materialKey']!=0xfd102a92:return [(section,'')]
    groups={'wheel_chrome':[],'wheel_gold':[]}
    for j in range(0,len(section['indices']),3):
        tri=section['indices'][j:j+3]
        # The original material merges lip/barrel and spokes. Existing radial
        # loops divide the outer machined ring from the five-point star.
        outer=min(math.hypot(part['vertices'][i][0],part['vertices'][i][2]) for i in tri)>=.2319
        groups['wheel_chrome' if outer else 'wheel_gold'].extend(tri)
    return [({**section,'indices':ids},finish) for finish,ids in groups.items() if ids]


def exhaust_mouths(part):
    # Connected dark mouth faces identify one or two actual openings, including
    # slanted, oval and twin styles. Keep the emitter beyond the outermost lip.
    sections=[s for s in part['slices'] if s['materialKey']==0x010cb64a]
    if len(sections)!=1:raise ValueError('Re-identify original exhaust mouth material')
    ids=sections[0]['indices'];groups=[]
    for i in range(0,len(ids),3):
        group=set(ids[i:i+3]);merged=[g for g in groups if g & group]
        for other in merged:
            group |= other;groups.remove(other)
        groups.append(group)
    if not 1<=len(groups)<=2 or any(len(g)<8 for g in groups):raise ValueError('Invalid original exhaust mouth topology')
    return sorted(((min(part['vertices'][i][0] for i in g),
                   sum(part['vertices'][i][1] for i in g)/len(g),
                   sum(part['vertices'][i][2] for i in g)/len(g)) for g in groups),key=lambda mouth:mouth[1])


def sport_brake_geometry(part):
    # Original STYLE03 uses an alpha-masked square for the drilled rotor.
    # Rebuild only that square as a round, thin rotor so opaque Unity materials
    # cannot expose black square corners; keep its original atlas and caliper.
    part={**part,'vertices':part['vertices'][:],'normals':part['normals'][:],'uv':part['uv'][:],
          'slices':[s for s in part['slices'] if s['materialKey']!=0xb1aae6bb]}
    # The detail atlas contains black-backed billboard rings over two already
    # closed red bosses. Opaque rendering makes these look like exposed pistons.
    # Remove only those overlays. Replace hub/bolt billboards with solid metal
    # discs; retain the pad and retaining clips from the original detail slice.
    details=next(s for s in part['slices'] if s['materialKey']==0x1fbd2378)
    expected=[i for start in range(12,100,4) for i in (start,start+1,start+2,start+2,start+3,start)]
    if details['indices']!=expected:
        raise ValueError('Re-identify STYLE03 caliper ring and hardware UV islands')
    rings=set(range(60,68))
    round_quads={16:32, **{i:6 for i in (*range(20,60,4),68,72,84,88,96)}}
    replaced=rings | {i for start in round_quads for i in range(start,start+4)}
    kept=[i for j in range(0,len(details['indices']),3)
          if not replaced.intersection(details['indices'][j:j+3])
          for i in details['indices'][j:j+3]]
    part['slices']=[{**s,'indices':kept} if s is details else s for s in part['slices']]
    metal=[]
    for start,count in round_quads.items():
        corners=np.array(part['vertices'][start:start+4]);center=corners.mean(axis=0)
        a=(corners[1]-corners[0])*.5;b=(corners[3]-corners[0])*.5
        normal=np.cross(a,b);normal/=np.linalg.norm(normal)
        first=len(part['vertices'])
        for point in [center]+[center+a*math.cos(2*math.pi*i/count)+b*math.sin(2*math.pi*i/count) for i in range(count)]:
            part['vertices'].append(tuple(point));part['normals'].append(tuple(normal));part['uv'].append((0,0))
        for i in range(count):metal.extend((first,first+1+i,first+1+(i+1)%count))
    # The source shares red paint between caliper and the rotor's centre hat.
    # Give only the centred hat its metal finish; the closed caliper is intact.
    painted=next(s for s in part['slices'] if s['materialKey']==0xd6640dff)
    caliper=[]
    for j in range(0,len(painted['indices']),3):
        tri=painted['indices'][j:j+3]
        target=metal if all(math.hypot(part['vertices'][i][0],part['vertices'][i][2])<.1 for i in tri) else caliper
        target.extend(tri)
    part['slices']=[{**s,'indices':caliper} if s is painted else s for s in part['slices']]
    part['slices'].append({'materialKey':0xf0a43b67,'textureKey':0,'indices':metal})
    indices=[];count=64
    for back,axial in ((False,-.008),(True,.006)):
        center=len(part['vertices'])
        part['vertices'].append((0,axial,0));part['normals'].append((0,1 if back else -1,0));part['uv'].append((.5,.5))
        for i in range(count):
            a=2*math.pi*i/count;x=.18*math.cos(a);z=.18*math.sin(a)
            part['vertices'].append((x,axial,z));part['normals'].append((0,1 if back else -1,0));part['uv'].append((.5+x/.36,.5-z/.36))
        for i in range(count):
            tri=[center,center+1+i,center+1+(i+1)%count]
            indices.extend(tri[::-1] if back else tri)
    part['slices'].append({'materialKey':0xb1aae6bb,'textureKey':bin_hash('BRAKE_STYLE03'),'indices':indices})
    return part

