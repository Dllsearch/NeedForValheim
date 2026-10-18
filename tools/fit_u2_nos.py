"""Measure the original NOS inner edge; keep one circular U2 HUD transform."""
import base64
import json
import math
import numpy as np
from project import LOCAL, OUTPUT


def fit():
    data = json.loads((LOCAL / 'hud/hud.json').read_text())
    art = data['nitroBacking']
    alpha = np.frombuffer(base64.b64decode(art['rgba']), dtype=np.uint8).reshape(art['height'], art['width'], 4)[:, :, 3]
    points = []
    # Exclude the N2O text and the terminal tip. Sample the continuous inner edge.
    for angle in np.linspace(-172, -52, 121):
        a = math.radians(angle)
        radii = np.arange(45, 100, .025)
        x, y = 92.5 + radii * math.cos(a), 110.5 + radii * math.sin(a)
        ix, iy = np.floor(x - .5).astype(int), np.floor(y - .5).astype(int)
        valid = (ix >= 0) & (ix + 1 < art['width']) & (iy >= 0) & (iy + 1 < art['height'])
        x, y, radii, ix, iy = [v[valid] for v in (x, y, radii, ix, iy)]
        fx, fy = x - .5 - ix, y - .5 - iy
        values = (alpha[iy, ix] * (1-fx) * (1-fy) + alpha[iy, ix+1] * fx * (1-fy)
                  + alpha[iy+1, ix] * (1-fx) * fy + alpha[iy+1, ix+1] * fx * fy)
        hits = np.flatnonzero(values >= 128)
        if not len(hits) or hits[0] == 0:
            raise ValueError('Original NOS edge could not be measured')
        i = hits[0]
        r = radii[i-1] + .025 * (128-values[i-1]) / (values[i]-values[i-1])
        points.append((92.5+r*math.cos(a), 110.5+r*math.sin(a)))
    p = np.asarray(points)
    cx, cy, c = np.linalg.lstsq(np.c_[2*p[:, 0], 2*p[:, 1], np.ones(len(p))], (p*p).sum(1), rcond=None)[0]
    radius = math.sqrt(c + cx*cx + cy*cy)
    error = np.sqrt(((p - [cx, cy])**2).sum(1)) - radius
    scale = 140 / radius
    if max(abs(error)) > 1:
        raise ValueError('Original NOS edge does not fit a circle within one source pixel')
    return dict(reference='Shared U2 tachometer circle: centre (140,170), radius 140',
        sourceCenter=[cx, cy], sourceRadius=radius, maxSourceError=float(max(abs(error))),
        rectangle=[140-cx*scale, 170-cy*scale, art['width']*scale, art['height']*scale],
        samples=len(points), limits='Original artwork fit only; real IMGUI acceptance remains required')


if __name__ == '__main__':
    record = fit()
    OUTPUT.mkdir(exist_ok=True)
    (OUTPUT / 'u2-nos-fit.json').write_text(json.dumps(record, indent=2) + '\n')
    print(json.dumps(record, indent=2))
