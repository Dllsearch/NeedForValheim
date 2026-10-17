"""Shared original wheel, brake and exhaust mesh adaptation."""
import math
import numpy as np
from nfs_formats import bin_hash


BADGE_OVERLAY = 0x02a05578


def source_attachment(vector, matrix, normal=False, exhaust_axes=False):
    """Apply a source socket before the shared NFS-to-Unity axis conversion."""
    x,y,z=vector
    if exhaust_axes:x,y,z=z,y,-x
    return tuple(x*matrix[a]+y*matrix[4+a]+z*matrix[8+a]+(0 if normal else matrix[12+a]) for a in range(3))


def painted_arch_width(mesh, axle):
    """Sample the outer body arch at the original axle, in source metres.

    This is a geometric fit, like the MW arch sampler, not decoded U2 tuning.
    Only lateral kit deltas are used; source wheelbase, radius and height stay.
    """
    vertices=np.asarray(mesh['vertices'])
    # Wide kits may use carbon or pretextured body materials instead of paint.
    # Glass, badge overlays, bright trim and inner black panels are not fenders.
    trim={0xa6348ee3,0xd79597d6,0x471a1dca,0x1ff8c329,BADGE_OVERLAY,
          0xfd102a92,0x54949afd,0x2e65e067,0x22719fa9,0x12c9453c,0x0fedee40,0x2787edab,0x3ed70c43,0x010cb64a}
    ids=[i for section in mesh['slices'] if section['materialKey'] not in trim for i in section['indices']]
    if not ids:raise ValueError('No exterior body shell: '+mesh['name'])
    faces=vertices[np.asarray(ids).reshape(-1,3)]
    a=faces.reshape(-1,3);b=np.roll(faces,-1,axis=1).reshape(-1,3);delta=b-a
    samples=[]
    for x in np.linspace(axle-.12,axle+.12,13):
        crossing=((a[:,0]-x)*(b[:,0]-x)<=0)&(abs(delta[:,0])>1e-8)
        points=a[crossing]+delta[crossing]*((x-a[crossing,0])/delta[crossing,0])[:,None]
        if not len(points):continue
        outer=points[:,1].max();points=points[points[:,1]>outer*.83]
        if not len(points):continue
        crown=points[:,2].min();rim=points[points[:,2]<=crown+.04]
        samples.append(float(rim[:,1].max()))
    if len(samples)<7:raise ValueError('Insufficient painted wheel arch: '+mesh['name'])
    return float(np.median(samples))


def badge_uv(part, section, coords):
    """Reverse each badge's lateral mapping, including rotated atlas islands.

    Vehicle conversion retains the established chassis/driver handedness. Badge
    artwork must be reflected in the surface frame, not across the entire atlas.
    Source positions, outward normals and triangle winding remain unchanged.
    """
    result=[dict(value) for value in coords]
    islands=[]
    ids=section['indices']
    # Some source solids duplicate every triangle's vertices. Reconstruct
    # continuous geometry/UV topology before reflecting an emblem as a whole.
    # UV is part of the key: adjoining halves in separate atlas rows must stay
    # independent patches for the word-order correction below.
    identities={};aliases={}
    for i in set(ids):
        key=tuple(round(v,5) for v in part['vertices'][i])+tuple(round(coords[i][k],6) for k in ('u','v'))
        identities.setdefault(key,i);aliases[i]=identities[key]
    for offset in range(0,len(ids),3):
        group={aliases[i] for i in ids[offset:offset+3]};joined=[other for other in islands if other & group]
        for other in joined:group|=other;islands.remove(other)
        islands.append(group)
    patches=[]
    for island in islands:
        indices=sorted(i for i in set(ids) if aliases[i] in island)
        normal=np.mean([part['normals'][i] for i in indices],axis=0)
        horizontal=1 if abs(normal[0])>=abs(normal[1]) or abs(normal[2])>.7 else 0
        vertical=0 if abs(normal[2])>.7 else 2
        points=np.array([[part['vertices'][i][horizontal],part['vertices'][i][vertical],1] for i in indices])
        values=np.array([[coords[i]['u'],coords[i]['v']] for i in indices])
        coefficients,_,rank,_=np.linalg.lstsq(points,values,rcond=None)
        if rank<3:continue
        patches.append(dict(ids=indices,points=points,values=values,fit=coefficients,normal=normal,
                            center=np.mean([part['vertices'][i] for i in indices],axis=0),axes=(horizontal,vertical)))
    # A word can span adjacent quads and even different atlas rows (SUB/ARU).
    # Reflect their order as well as each quad's mapping. Separate emblems and
    # badges on opposite faces must retain their own artwork and positions.
    groups=[]
    for p in patches:
        linked=[]
        for group in groups:
            for q in group:
                if p['axes']!=q['axes'] or np.dot(p['normal'],q['normal'])<.8:continue
                a,b=p['points'][:,:2],q['points'][:,:2]
                span=np.minimum(np.ptp(a,axis=0),np.ptp(b,axis=0))
                overlap=min(a[:,1].max(),b[:,1].max())-max(a[:,1].min(),b[:,1].min())
                gap=max(a[:,0].min(),b[:,0].min())-min(a[:,0].max(),b[:,0].max())
                depth=abs(np.dot(p['center']-q['center'],q['normal']))
                # Source quads can overlap slightly at a word seam (Escalade
                # overlaps by 0.47 mm; other seams differ only by float noise).
                # Keep that seam together without merging stacked overlays.
                seam=min(span[0]*.05,.002)
                sizes_a,sizes_b=np.ptp(a,axis=0),np.ptp(b,axis=0)
                letters=all(size[1]>0 and size[0]/size[1]<1.6 for size in (sizes_a,sizes_b))
                # Spaced single-letter badges (HUMMER, LOTUS) are still one
                # word; full wordmarks retain the tighter adjacency limit.
                reach=span[1]*4 if letters else min(span[0]*.3,span[1]*3)
                if span[1]>0 and overlap>span[1]*.8 and -seam<=gap<reach and depth<.025:
                    linked.append(group);break
        merged=[p]
        for group in linked:merged+=group
        groups=[group for group in groups if all(group is not other for other in linked)]
        groups.append(merged)
    for group in groups:
        ordered=sorted(group,key=lambda p:np.mean(p['points'][:,0]))
        for p,q in zip(ordered,reversed(ordered)):
            a,b=p['points'],q['points'];sample=a.copy()
            for axis in range(2):
                span=np.ptp(a[:,axis]);fraction=(a[:,axis]-a[:,axis].min())/span if span else np.zeros(len(a))
                sample[:,axis]=b[:,axis].min()+((1-fraction) if axis==0 else fraction)*np.ptp(b[:,axis])
            residual=p['values']-a@p['fit'] if p is q else 0
            mirrored=np.clip(sample@q['fit']+residual,q['values'].min(axis=0),q['values'].max(axis=0))
            for i,value in zip(p['ids'],mirrored):result[i]=dict(u=float(value[0]),v=float(value[1]))
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

