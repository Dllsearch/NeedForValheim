"""Complete an open inner tyre using its authored outer sidewall geometry."""


def complete_inner_sidewall(part):
    vertices, normals = part['vertices'], part['normals']
    middle = (min(v['x'] for v in vertices) + max(v['x'] for v in vertices)) * .5
    if any(v['x'] < middle and n['x'] < -.15 for v, n in zip(vertices, normals)):
        return 0
    selected = []
    for i in range(0, len(part['indices']), 3):
        tri = part['indices'][i:i + 3]
        if all(vertices[j]['x'] > middle for j in tri) and any(normals[j]['x'] > .15 for j in tri):
            selected.append(tri)
    if not selected:
        return 0
    remap = {}
    for index in sorted({j for tri in selected for j in tri}):
        remap[index] = len(vertices)
        v, n = vertices[index], normals[index]
        vertices.append(dict(x=2 * middle - v['x'], y=v['y'], z=v['z']))
        normals.append(dict(x=-n['x'], y=n['y'], z=n['z']))
        part['uv'].append(dict(part['uv'][index]))
    for a, b, c in selected:
        part['indices'].extend((remap[a], remap[c], remap[b]))
    return len(selected)
