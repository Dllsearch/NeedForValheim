"""Authored U2 neon endpoints; only exact source marker-name hashes are used."""
from nfs_formats import bin_hash

def neon_segments(mesh,convert):
    markers=mesh.get('sockets',{});result=[]
    for area,count in (('FRONT',3),('SIDE',2),('REAR',2)):
        for i in range(count):
            start=markers.get(bin_hash(f'{area}_NEON_{i}_START'))
            end=markers.get(bin_hash(f'{area}_NEON_{i}_END'))
            if start is None or end is None:continue
            result.append(dict(start=convert(start[12:15]),end=convert(end[12:15])))
    return result

def purge_emitters(mesh,position,direction):
    result=[]
    for side in ('LEFT','RIGHT'):
        matrix=mesh.get('sockets',{}).get(bin_hash('NOS_PURGE_'+side))
        if matrix is not None:
            result.append(dict(role='purge',position=position(matrix[12:15]),direction=direction(matrix[8:11])))
    return result
