"""Infer axle centres from the original painted wheel-arch contours.

This is a geometric fit, not a claim to decode MW's collision/VLT mounts.
Sampling triangle edges avoids biased fits to unevenly spaced source vertices.
"""
import numpy as np


def fit_arch(body, seed):
    vertices = np.asarray(body['vertices'])
    triangles = np.asarray([s['indices'][i:i+3] for s in body['slices']
                            if s['materialName'].startswith('BODY') and not any(x in s['materialName'] for x in ('CHROME','ALUMINUM','PLASTIC','MOLDING','BOTTOM','BADGING'))
                            for i in range(0, len(s['indices']), 3)])
    if not len(triangles):
        raise ValueError('No painted wheel-arch geometry')
    faces = vertices[triangles]
    a, b = faces.reshape(-1, 3), np.roll(faces, -1, axis=1).reshape(-1, 3)
    delta = b - a
    side = np.max(vertices[:, 1]) * .83

    def contour(x):
        mask = ((a[:, 0]-x)*(b[:, 0]-x) <= 0) & (np.abs(delta[:, 0]) > 1e-8)
        points = a[mask] + delta[mask] * ((x-a[mask, 0])/delta[mask, 0])[:, None]
        points = points[points[:, 1] > side]
        if not len(points):
            return None
        p = points[np.argmin(points[:, 2])]
        return [float(x), float(p[2]), float(p[1])]

    samples = [p for x in np.arange(seed-.45, seed+.451, .01) if (p := contour(x)) is not None]
    if not samples:raise ValueError('No wheel-arch contour at this axle')
    peak = max(samples, key=lambda p: p[1])[0]
    samples = np.asarray([p for x in np.arange(peak-.23, peak+.231, .01) if (p := contour(x)) is not None])
    if len(samples) < 30:
        raise ValueError('Insufficient wheel-arch contour')
    # x²+z² = 2*cx*x + 2*cz*z + (r²-cx²-cz²).
    for _ in range(3):
        x, z = samples[:, 0], samples[:, 1]
        cx, cz, constant = np.linalg.lstsq(np.column_stack((2*x, 2*z, np.ones(len(x)))), x*x+z*z, rcond=None)[0]
        radius = float(np.sqrt(constant+cx*cx+cz*cz))
        errors = np.abs(np.hypot(x-cx, z-cz)-radius)
        keep = errors < .012
        if keep.all():
            break
        samples = samples[keep]
        if len(samples) < 25:
            raise ValueError('Wheel arch is not a reliable circular contour')
    if not .3 < radius < .5 or abs(cx-seed) > .4 or not -.05 < cz < .3:
        raise ValueError('Wheel-arch fit outside supported chassis bounds')
    return dict(axle=float(cx), hub=float(cz), radius=radius,
                outer=float(np.median(samples[:, 2])), error=float(errors.max()), samples=len(samples))
