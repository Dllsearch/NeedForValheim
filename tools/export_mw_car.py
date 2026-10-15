"""Import stock PC Most Wanted (2005) cars into NFVU2 schema 3.

Reads the user's installed files without modifying MW or the installed mod.
Wheel placement is fitted to original arch geometry, not imported MW physics. Audio uses
the existing embedded engine as an explicit fallback. See docs/MW_IMPORT.md.
"""
import argparse
import base64
import hashlib
import json
import math
import re
from pathlib import Path

from mw_geometry import parse
from mw_wheel_fit import fit_arch
from nfs_formats import TexturePack, bin_hash
from project import LOCAL, mw_root

SPECS = {
    'BMWM3GTR': dict(display='BMW M3 GTR Street', short='M3 GTR Street', mass=1450,
                     front=1.38, rear=1.35, rim=19),
    'BMWM3GTRE46': dict(display='BMW M3 GTR Race', short='M3 GTR Race', mass=1350,
                       front=1.38, rear=1.35, rim=19),
    '997S': dict(display='Porsche 911 Carrera S', short='911 Carrera S', mass=1420,
                 front=1.17, rear=1.18, rim=19),
}


def source_folder(root, car):
    if not re.fullmatch(r'[A-Z0-9_]{1,29}', car):
        raise ValueError('Car must be a folder name under CARS')
    folder = root / 'CARS' / car
    for name in ('GEOMETRY.BIN', 'TEXTURES.BIN'):
        if not (folder / name).is_file():
            raise ValueError(f'MW {car}: missing CARS/{car}/{name}')
    return folder


def paint_surface(name):
    return name.startswith('BODY') and not any(x in name for x in ('CHROME', 'MOLDING', 'PLASTIC', 'CARBON', 'BOTTOM', 'BADGING'))


def export(root, car, spec):
    folder = source_folder(root, car)
    meshes = parse(folder / 'GEOMETRY.BIN')
    stock = [m for m in meshes if m['name'].endswith('_A') and
             (m['name'] == car + '_BASE_A' or m['name'].startswith(car + '_KIT00_')) and
             not any(x in m['name'] for x in ('DECAL', 'DRIVER', '_DAMAGE', '_HEADLIGHT_GLASS_', '_UNIVERSAL_'))]
    body = [m for m in stock if not any(x in m['name'] for x in ('_TIRE_', '_BRAKE_'))]
    wheel = next((m for m in stock if m['name'] == car + '_KIT00_FRONT_TIRE_A'), None)
    brake = next((m for m in stock if m['name'] == car + '_KIT00_FRONT_BRAKE_A'), None)
    rear_brake = next((m for m in stock if m['name'] == car + '_KIT00_REAR_BRAKE_A'), brake)
    if not body or wheel is None or brake is None:
        raise ValueError(f'MW {car}: stock body, FRONT_TIRE and FRONT_BRAKE meshes required')
    minimum = [min(v[i] for m in body for v in m['vertices']) for i in range(3)]
    maximum = [max(v[i] for m in body for v in m['vertices']) for i in range(3)]
    radius = max(math.hypot(v[0], v[2]) for s in wheel['slices']
                 if s['materialName'].startswith('TIRE') and 'BACK' not in s['materialName']
                 for i in s['indices'] for v in [wheel['vertices'][i]])
    if not .2 < radius < .65:
        raise ValueError(f'MW {car}: unsupported tyre radius {radius}')
    shell = next(m for m in body if m['name'] == car + '_KIT00_BODY_A')
    fits = [fit_arch(shell, seed) for seed in (spec['front'], -spec['rear'])]
    source_hub = sum(f['hub'] for f in fits) / 2
    lift = radius - source_hub
    if minimum[2]+lift < .015 or abs(fits[0]['hub']-fits[1]['hub']) > .04:
        raise ValueError('MW arch fit requires reviewed ride height: ' + car)
    hub = radius
    axial = [v[1] for v in wheel['vertices']]
    wheel_mid = (min(axial) + max(axial)) / 2
    tyre_width = max(axial)-min(axial)
    tracks = [f['outer']-tyre_width/2-.015 for f in fits]
    if any(f['radius']-radius < .02 for f in fits):
        raise ValueError('MW tyre does not clear the measured wheel arch: ' + car)
    sources = [folder / 'TEXTURES.BIN', folder / 'PREVINYL.BIN', root / 'CARS/TEXTURES.BIN',
               root / 'CARS/WHEELS/TEXTURES.BIN', root / 'GLOBAL/GlobalB.bun']
    packs = [TexturePack(p) for p in sources if p.is_file()]
    textures, materials, parts, lights = [], [], [], []
    texture_cache, material_cache, decoded_images = {}, {}, {}
    unresolved = set()

    def texture(key):
        if key in texture_cache:
            return texture_cache[key]
        for pack in packs:
            if key in pack.slots or key in pack.records:
                name, image, _ = pack.decode(key)
                # MW alpha often encodes shader masks rather than transparency.
                image = image.convert('RGB').convert('RGBA')
                name = f'mw_{key:08x}'
                decoded_images[name] = image
                texture_cache[key] = len(textures)
                textures.append(dict(name=name, width=image.width, height=image.height,
                                     rgba=base64.b64encode(image.tobytes()).decode('ascii')))
                return texture_cache[key]
        unresolved.add(f'{key:08x}')
        texture_cache[key] = -1
        return -1

    def material(section, group):
        name = section['materialName'].upper()
        identity = (name, section['textureKey'], group)
        if identity in material_cache:
            return material_cache[identity]
        key = section['textureKey']
        # MW switches lamp atlas references at runtime. Export the original ON
        # atlases for emissive lamps; do not invent a procedural lamp texture.
        if 'HEADLIGHT' in name:
            key = bin_hash(car + '_KIT00_HEADLIGHT_ON')
        elif 'BRAKELIGHT' in name:
            key = bin_hash(car + '_KIT00_BRAKELIGHT_ON')
        tex = -1 if 'GLASS' in name or 'WINDOW' in name else texture(key)
        paint = paint_surface(name)
        color, metal, smooth = [1., 1., 1., 1.], .08, .45
        if paint:
            metal, smooth = .55, .7
            if tex < 0:
                color = [.68, .7, .73, 1.]
        elif 'GLASS' in name or 'WINDOW' in name:
            color, metal, smooth, tex = [.055, .08, .1, 1.], .4, .95, -1
        elif any(x in name for x in ('CHROME', 'SILVER', 'ROTOR')):
            metal, smooth = .85, .75
        elif 'TIRE' in name:
            smooth = .12
            if tex < 0:
                color = [.055, .055, .055, 1.]
        elif 'CALIPER' in name:
            metal, smooth = .4, .55
        elif tex < 0:
            color = [.13, .13, .14, 1.]
        index = len(materials)
        material_cache[identity] = index
        materials.append(dict(name='mw_' + name.lower() + '_' + group, texture=tex, color=color,
                              metallic=metal, smoothness=smooth, bodyPaint=paint,
                              wheelRubber=group=='wheel' and name.startswith('TIRE'),
                              rimPaint=group == 'wheel' and name.startswith('RIM') and 'PLASTIC' not in name))
        return index

    def vertex(v, group, normal=False):
        x, y, z = v
        # NFS +X forward, +Z up. Wheels use +Y as their axial direction.
        if group == 'body':
            xyz = (y, z + (0 if normal else lift), x)
        elif group == 'wheel':
            xyz = (-y, z, x) if normal else (wheel_mid - y, z, x)
        else:
            xyz = (-y, z, x) if normal else (.04 - y, z, x)
        if normal:
            length = math.sqrt(sum(c * c for c in xyz))
            xyz = tuple(c / length for c in xyz) if length else (0, 1, 0)
        return dict(zip(('x', 'y', 'z'), xyz))

    for mesh, group, axle in [(m, 'body', '') for m in body] + [(wheel, 'wheel', ''), (brake, 'brake', 'front'), (rear_brake, 'brake', 'rear')]:
        for number, section in enumerate(mesh['slices']):
            # TIRE_BACK is a square alpha-mask billboard, not a physical wheel
            # surface. The current opaque material pipeline cannot render it.
            if not section['indices'] or section['materialName'] in ('TIRE_KIT00_BACK', 'DOORLINE'):
                continue
            role = ('headlight' if '_HEADLIGHT_' in mesh['name'] and '_BULB' in section['materialName']
                    else 'taillight' if '_BRAKELIGHT_' in mesh['name'] and '_BULB' in section['materialName'] else '')
            ids = section['indices'][:]
            if group != 'body':
                for i in range(0, len(ids), 3):
                    ids[i + 1], ids[i + 2] = ids[i + 2], ids[i + 1]
            used = sorted(set(ids))
            remap = {old: new for new, old in enumerate(used)}
            parts.append(dict(name=f'{mesh["name"]}_{number}', group=group, axle=axle, material=material(section, group),
                              lightRole=role, rotatesWithWheel=group == 'brake' and 'ROTOR' in section['materialName'],
                              vertices=[vertex(mesh['vertices'][i], group) for i in used],
                              normals=[vertex(mesh['normals'][i], group, True) for i in used],
                              uv=[dict(u=mesh['uv'][i][0], v=1-mesh['uv'][i][1]) for i in used],
                              indices=[remap[i] for i in ids]))
        role = ('headlight' if '_HEADLIGHT_' in mesh['name'] and 'GLASS' not in mesh['name']
                else 'taillight' if '_BRAKELIGHT_' in mesh['name'] else '')
        if role:
            points = [mesh['vertices'][i] for i in set(mesh['indices'])]
            centre = tuple(sum(v[a] for v in points) / len(points) for a in range(3))
            lights.append(dict(role=role, position=vertex(centre, 'body'), direction=dict(x=0, y=-.12, z=1 if role == 'headlight' else -1)))
    ident = 'mw-' + car.lower()
    generated_skin = f'{bin_hash(car + "_SKIN1"):08x}'
    unresolved.discard(generated_skin)
    if unresolved:
        raise ValueError(f'MW {car}: unresolved texture keys: {", ".join(sorted(unresolved))}')
    model = dict(schemaVersion=3, preset='NFVU2_MW_' + car, parts=parts, materials=materials,
                 textures=textures, lights=lights, previewHubHeight=hub,
                 wheelVisualLift=0)
    if abs(model['wheelVisualLift']) > .2:
        raise ValueError('MW chassis needs a revised hub preset: ' + car)
    profile = dict(id=ident, sourceGame='mw', displayName=spec['display'], shortName=spec['short'],
                   summary='Most Wanted stock model; NFVU2 chassis and fallback engine audio.',
                   rig=dict(boxCenterY=(minimum[2]+maximum[2])/2+lift,
                            boxSizeX=(maximum[1]-minimum[1])*.95,
                            boxSizeY=(maximum[2]-minimum[2])*.65,
                            boxSizeZ=(maximum[0]-minimum[0])*.95, mass=spec['mass'],
                            trackWidth=tracks[0], rearTrackWidth=tracks[1], wheelHeight=radius, wheelRadius=radius,
                            frontAxleZ=fits[0]['axle'], rearAxleZ=-fits[1]['axle'], spawnLift=lift,
                            clearanceX=.28, clearanceZ=.45, rimNominal=spec['rim'], rimMin=16, rimMax=22),
                   capabilities=dict(radio=True, drift=True),
                   sound=dict(gearRatios=[4.15,2.32,1.62,1.27,1.,.79],
                              rpmBands=[1400,2100,2800,3500,4300,5100,5900,6500],
                              upShiftRpm=5600, downShiftRpm=2000, minRpm=950, maxRpm=6700),
                   assets=dict(modelFile=f'{ident}/{ident}.json', audioFolders=['audio/effects']))
    provenance = dict(sourceGame='Most Wanted (2005), PC', car=car,
                      geometrySha256=hashlib.sha256((folder/'GEOMETRY.BIN').read_bytes()).hexdigest(),
                      textureSha256=hashlib.sha256((folder/'TEXTURES.BIN').read_bytes()).hexdigest(),
                      body=[m['name'] for m in body], wheel=wheel['name'], brake=brake['name'], rearBrake=rear_brake['name'],
                      wheelArchFits=fits, sourceHubHeight=source_hub,
                      tyreRadius=radius, bodyLift=lift, unresolvedTextures=sorted(unresolved),
                      proceduralMaterials=['MW runtime-generated SKIN1 paint', 'Opaque window tint'],
                      chassis='Wheel centres/tracks fitted to original arch contours; body raised to tyre contact at y=0. Mass/rim range remain authored NFVU2 values, not MW collision/VLT physics.',
                      audio='Existing embedded 350Z engine fallback, not MW audio.',
                      excluded='Decals, driver, damage overlays (including DAMAGE0), DOORLINE alpha overlays, headlamp glass overlays, alternate kits, TIRE_BACK alpha billboards, rear-specific VLT tyre scaling, generated liveries, MW physics and engine banks.')
    out = LOCAL / ident
    out.mkdir(parents=True, exist_ok=True)
    for name, image in decoded_images.items():
        image.save(out/(name+'.png'))
    for filename, value in ((ident+'.json', model), ('profile.json', [profile]), ('provenance.json', provenance)):
        (out/filename).write_text(json.dumps(value, separators=(',', ':')), encoding='utf-8')
    print(f'{car}: {len(parts)} sections, {len(textures)} textures; id={ident}; unresolved textures={len(unresolved)}')
    return profile, model


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('car', nargs='?', help='CARS folder, e.g. BMWM3GTR')
    parser.add_argument('--source', type=Path, help='MW 2005 installation (overrides mw_root)')
    parser.add_argument('--list', action='store_true', help='List source cars and supported chassis presets')
    parser.add_argument('--all', action='store_true', help='Run the complete fleet/parts/vinyl importer and promote verified results')
    args = parser.parse_args()
    root = (args.source or mw_root()).resolve()
    if not (root/'CARS').is_dir() or not (root/'GLOBAL').is_dir():
        parser.error('Expected a PC Most Wanted (2005) installation with CARS and GLOBAL')
    if args.all:
        import subprocess,sys
        subprocess.run([sys.executable,str(Path(__file__).with_name('import_mw.py')),'--source',str(root),'--promote'],check=True)
        return
    if args.list:
        for folder in sorted((root/'CARS').iterdir()):
            if folder.is_dir() and (folder/'GEOMETRY.BIN').is_file():
                print(folder.name + (' [supported]' if folder.name.upper() in SPECS else ' [chassis preset needed]'))
        return
    cars = [c for c in SPECS if (root/'CARS'/c).is_dir()] if args.all else [args.car.upper()] if args.car else []
    if not cars:
        parser.error('Choose a car, --all, or --list')
    for car in cars:
        if car not in SPECS:
            parser.error(car + ': add a reviewed NFVU2 chassis preset to SPECS first')
        try:
            export(root, car, SPECS[car])
        except (ValueError, OSError) as error:
            parser.exit(1, f'MW {car}: {error}\n')


if __name__ == '__main__':
    main()
