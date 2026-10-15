"""Fit NOS effects to the imported exhaust geometry, using NFVU2 timing."""
def nitro_emitters(parts, track, front, exhaust_fallback=()):
    points=[p['vertices'][i] for p in parts if p.get('group')=='body' and 'EXHAUST' in p['name'] for i in set(p['indices'])]
    result=[]
    if points:
        tail=min(p['z'] for p in points);mouth=[p for p in points if p['z']<=tail+.035]
        left=[p for p in mouth if p['x']<0];right=[p for p in mouth if p['x']>=0]
        groups=[left,right] if left and right and max(p['x'] for p in mouth)-min(p['x'] for p in mouth)>.4 else [mouth]
        for group in groups:
            position={axis:sum(p[axis] for p in group)/len(group) for axis in ('x','y','z')};position['z']-=.025
            result.append(dict(role='exhaust',position=position,direction=dict(x=0,y=0,z=-1)))
    else:
        for point in exhaust_fallback:
            position=dict(point);position['z']-=.025
            result.append(dict(role='exhaust',position=position,direction=dict(x=0,y=0,z=-1)))
    for side in (-1,1):result.append(dict(role='purge',position=dict(x=side*track*.65,y=.65,z=front+.2),direction=dict(x=side*.25,y=1,z=.1)))
    return result
