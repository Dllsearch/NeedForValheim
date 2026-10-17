"""Import NFSU2 vehicles through shared geometry conversion and visual presets.

Every part comes from the car's own geometry: the KIT families for the body, its
own wheel and brake meshes, and its four wheel locators from the decompressed
global record (`NAME\0` + 0x120, stride 0x30). Material keys are shared across
cars, so the authored finishes below apply to any of them; texture keys that no
local pack provides stay procedural. Shared wheel packs and showroom assets are resolved by the same pipeline.
"""
import argparse
import base64
import json
import math
import struct
from pathlib import Path

from asset_source import extract_matching
from export_geometry import parse
from vehicle_geometry import wheel_source_radius, sport_brake_geometry, exhaust_mouths, badge_uv, badge_rows_uv, BADGE_OVERLAY, source_attachment
import showroom_assets
from nfs_formats import TexturePack, bin_hash, jdlz
from project import LOCAL

ASSETS = LOCAL / 'nfs-assets'
LOD_SUFFIXES = ('_A', '_B', '_C', '_D')

RUBBER = 0x78743ca1
LIP = 0x010cb64a
PAINT = 0xd6d6080a
CALIPER = 0xd6640dff
LENS = 0x05bc3a3c
CLEAR = (0xa6348ee3, 0xd79597d6)
CHROME = (0xfd102a92, 0x54949afd, 0x2e65e067, 0x22719fa9)
GLASS = (0x471a1dca, 0x1ff8c329)
DARK = (0x12c9453c, 0x0fedee40, 0x2787edab, 0x3ed70c43)
HEAD_LENS = (0xf7fc7674, 0x9c645529)
TAIL_LENS = (LENS, 0xf7fc7674, 0x33a26cb6)
SKIP = ('DECAL', 'ENGINE', 'TRUNK_AUDIO')

# Mesh names in the geometry headers are truncated at 24 characters, so the
# wheel and brake families arrive under two spellings each.
WHEEL_SUFFIX = ('_WHEEL', '_WHEE', '_WHEEL_')
BRAKE_SUFFIX = ('_BRAKE', '_BRAK', '_BRAKE_')

# Stock engine bank and EngineData index per car, resolved from the game's own
# chain CarTypeMapping/<CAR>.ini -> CarDataMapping -> EngineData. Cars share
# banks, so audio extraction has to target the bank file, not the car name.
SOUND = {
    'skyline': ('GIN_Nissan_Skyline.gin', 9), 'rx7': ('GIN_Mazda_RX7.gin', 10),
    'supra': ('GIN_350Z_01d_shrt2.gin', 26), 'miata': ('GIN_Mazda_Miata.gin', 23),
    '240sx': ('GIN_Nissan_240SX.gin', 18), 'eclipse': ('GIN_MitsEclipse_2000.gin', 6),
    'mustanggt': ('GIN_MustSal_Ramp_V2_CD.gin', 14), '3000gt': ('GIN_MitsEclipse_2000.gin', 6),
    'a3': ('GIN_VW_Cabriolet.gin', 29), 'celica': ('GIN_Honda_S2000_APEX.gin', 22),
    'civic': ('GIN_Honda_S2000_APEX.gin', 22), 'corolla': ('GIN_Nissan_240SX.gin', 18),
    'corsa': ('GIN_FordFocus_Road.gin', 31), 'escalade': ('GIN_Porsche_Cayenne.gin', 39),
    'focus': ('GIN_FordFocus_Road.gin', 31), 'g35': ('GIN_G35_350ZX_Comp_v2_AB.gin', 25),
    'golf': ('GIN_VW_Cabriolet.gin', 29), 'gto': ('GIN_MustSal_Ramp_V2_CD.gin', 14),
    'imprezawrx': ('GIN_WRXSTi_Pull_09.gin', 15), 'is300': ('GIN_350Z_01d_shrt2.gin', 26),
    'lancerevo8': ('GIN_Honda_S2000_APEX.gin', 22), 'navigator': ('GIN_Porsche_Cayenne.gin', 39),
    'peugot': ('GIN_Mazda_Miata.gin', 23), 'peugot106': ('GIN_Mazda_Miata.gin', 23),
    'rsx': ('GIN_Honda_S2000_Spugen.gin', 4), 'rx8': ('GIN_Mazda_RX7.gin', 10),
    'sentra': ('GIN_Protege_Ramp_V2_CD.gin', 33), 'tiburon': ('GIN_VR6_Jetta_M3.gin', 28),
}

SPECS = {
    'skyline': dict(display='Nissan Skyline', short='Skyline', kit='KIT00',
                    paint=(.60, .63, .68), preset='NFVU2_SKYLINE_SILVER', mass=1540,
                    gears=[3.55, 2.33, 1.48, 1.15, .86],
                    bands=[1500, 2500, 3500, 4400, 5400, 6400, 7300, 8300],
                    upShift=7000, downShift=2200, minRpm=1200, maxRpm=8300,
                    rim=(18, 15, 26), clearance=(.28, .45),
                    capabilities=dict(radio=True, drift=True)),
    'rx7': dict(display='Mazda RX-7', short='RX-7', kit='KIT00',
                paint=(.82, .16, .05), preset='NFVU2_RX7_RED', mass=1270,
                gears=[3.68, 2.19, 1.41, 1.00, .82],
                bands=[1200, 2000, 2800, 3600, 4400, 5300, 6200, 7239],
                upShift=6600, downShift=1700, minRpm=1100, maxRpm=7239,
                rim=(17, 15, 22), clearance=(.26, .42),
                capabilities=dict(radio=True, drift=True)),
    'supra': dict(display='Toyota Supra', short='Supra', kit='KIT00',
                  paint=(.70, .06, .06), preset='NFVU2_SUPRA_RED', mass=1570,
                  gears=[3.73, 2.33, 1.48, 1.00, .76],
                  bands=[1300, 2000, 2700, 3400, 4100, 4800, 5600, 6510],
                  upShift=5900, downShift=1700, minRpm=1200, maxRpm=6510,
                  rim=(18, 15, 22), clearance=(.26, .40),
                  capabilities=dict(radio=True, drift=True)),
    'miata': dict(display='Mazda MX-5 Miata', short='Miata', kit='KIT00',
                  paint=(.88, .76, .06), preset='NFVU2_MIATA_YELLOW', mass=1090,
                  gears=[3.56, 2.19, 1.48, 1.00, .84],
                  bands=[1600, 2400, 3200, 4000, 4800, 5600, 6400, 7158],
                  upShift=6600, downShift=2000, minRpm=1500, maxRpm=7158,
                  rim=(16, 14, 20), clearance=(.26, .42),
                  capabilities=dict(radio=True, drift=True)),
    '240sx': dict(display='Nissan 240SX', short='240SX', kit='KIT00',
                  paint=(.68, .70, .74), preset='NFVU2_240SX_SILVER', mass=1210,
                  gears=[3.53, 2.26, 1.53, 1.00, .81],
                  bands=[1600, 2400, 3200, 4000, 4800, 5600, 6400, 7037],
                  upShift=6500, downShift=1900, minRpm=1526, maxRpm=7037,
                  rim=(17, 15, 22), clearance=(.28, .44),
                  capabilities=dict(radio=True, drift=True)),
    'eclipse': dict(display='Mitsubishi Eclipse GSX', short='Eclipse', kit='KIT00',
                    paint=(.05, .32, .16), preset='NFVU2_ECLIPSE_GREEN', mass=1360,
                    gears=[3.42, 2.10, 1.36, 1.02, .80],
                    bands=[1100, 1900, 2700, 3500, 4300, 5100, 6000, 6888],
                    upShift=6300, downShift=1700, minRpm=1000, maxRpm=6888,
                    rim=(18, 15, 22), clearance=(.26, .42),
                    capabilities=dict(radio=True, drift=True)),
    'mustanggt': dict(display='Ford Mustang GT', short='Mustang GT', kit='KIT00',
                      paint=(.05, .22, .56), preset='NFVU2_MUSTANG_BLUE', mass=1570,
                      gears=[3.37, 2.32, 1.55, 1.00, .74],
                      bands=[800, 1400, 2000, 2600, 3200, 3800, 4400, 5121],
                      upShift=4700, downShift=1500, minRpm=750, maxRpm=5121,
                      rim=(18, 15, 24), clearance=(.28, .44),
                      capabilities=dict(radio=True, drift=True)),
    '3000gt': dict(display='Mitsubishi 3000GT', short='3000GT', kit='KIT00',
                   paint=(.72, .08, .08), preset='NFVU2_3000GT_RED', mass=1640,
                   gears=[3.73, 2.33, 1.48, 1.00, .76],
                   bands=[1000, 1840, 2680, 3520, 4360, 5210, 6050, 6888],
                   upShift=5800, downShift=1400, minRpm=1000, maxRpm=6888,
                   rim=(18, 15, 22), clearance=(.26, .40),
                   capabilities=dict(radio=True, drift=True)),
    'a3': dict(display='Audi A3', short='A3', kit='KIT00',
               paint=(.85, .86, .88), preset='NFVU2_A3_SILVER', mass=1350,
               gears=[3.42, 2.10, 1.36, 1.02, .80],
               bands=[1350, 1965, 2580, 3195, 3810, 4424, 5039, 5653],
               upShift=4800, downShift=1750, minRpm=1350, maxRpm=5653,
               rim=(17, 15, 20), clearance=(.26, .42),
               capabilities=dict(radio=True, drift=True)),
    'celica': dict(display='Toyota Celica', short='Celica', kit='KIT00',
                   paint=(.80, .82, .84), preset='NFVU2_CELICA_SILVER', mass=1250,
                   gears=[3.56, 2.19, 1.48, 1.00, .84],
                   bands=[1250, 2255, 3260, 4265, 5270, 6274, 7279, 8284],
                   upShift=7000, downShift=1650, minRpm=1250, maxRpm=8284,
                   rim=(17, 15, 20), clearance=(.26, .42),
                   capabilities=dict(radio=True, drift=True)),
    'civic': dict(display='Honda Civic', short='Civic', kit='KIT00',
                  paint=(.88, .90, .92), preset='NFVU2_CIVIC_SILVER', mass=1160,
                  gears=[3.23, 1.99, 1.33, 1.06, .84],
                  bands=[1250, 2255, 3260, 4265, 5270, 6274, 7279, 8284],
                  upShift=7200, downShift=1650, minRpm=1250, maxRpm=8284,
                  rim=(16, 14, 20), clearance=(.25, .40),
                  capabilities=dict(radio=True, drift=True)),
    'corolla': dict(display='Toyota Corolla', short='Corolla', kit='KIT00',
                    paint=(.86, .87, .89), preset='NFVU2_COROLLA_SILVER', mass=1160,
                    gears=[3.53, 2.26, 1.53, 1.00, .81],
                    bands=[1526, 2313, 3100, 3887, 4675, 5462, 6249, 7037],
                    upShift=6000, downShift=1930, minRpm=1526, maxRpm=7037,
                    rim=(16, 14, 20), clearance=(.25, .40),
                    capabilities=dict(radio=True, drift=True)),
    'corsa': dict(display='Opel Corsa', short='Corsa', kit='KIT00',
                  paint=(.10, .30, .60), preset='NFVU2_CORSA_BLUE', mass=1180,
                  gears=[3.42, 2.10, 1.36, 1.02, .80],
                  bands=[1150, 1924, 2698, 3472, 4246, 5020, 5794, 6566],
                  upShift=5600, downShift=1550, minRpm=1150, maxRpm=6566,
                  rim=(16, 14, 20), clearance=(.25, .40),
                  capabilities=dict(radio=True, drift=True)),
    'escalade': dict(display='Cadillac Escalade', short='Escalade', kit='KIT00',
                     paint=(.12, .12, .13), preset='NFVU2_ESCALADE_BLACK', mass=2500,
                     gears=[2.80, 1.55, 1.00, .72],
                     bands=[1000, 1865, 2729, 3594, 4458, 5323, 6187, 7052],
                     upShift=5200, downShift=1400, minRpm=1000, maxRpm=7052,
                     rim=(20, 18, 24), clearance=(.34, .50),
                     capabilities=dict(radio=True, drift=True)),
    'focus': dict(display='Ford Focus', short='Focus', kit='KIT00',
                  paint=(.20, .45, .80), preset='NFVU2_FOCUS_BLUE', mass=1250,
                  gears=[3.42, 2.10, 1.36, 1.02, .80],
                  bands=[1150, 1924, 2698, 3472, 4246, 5020, 5794, 6566],
                  upShift=5600, downShift=1550, minRpm=1150, maxRpm=6566,
                  rim=(17, 15, 20), clearance=(.26, .42),
                  capabilities=dict(radio=True, drift=True)),
    'g35': dict(display='Infiniti G35', short='G35', kit='KIT00',
               paint=(.15, .15, .18), preset='NFVU2_G35_BLACK', mass=1590,
               gears=[3.37, 2.24, 1.51, 1.00, .76],
               bands=[1200, 1971, 2742, 3513, 4284, 5055, 5826, 6599],
               upShift=5600, downShift=1600, minRpm=1200, maxRpm=6599,
               rim=(18, 15, 22), clearance=(.27, .43),
               capabilities=dict(radio=True, drift=True)),
    'golf': dict(display='Volkswagen Golf', short='Golf', kit='KIT00',
                 paint=(.85, .85, .87), preset='NFVU2_GOLF_SILVER', mass=1320,
                 gears=[3.42, 2.10, 1.36, 1.02, .80],
                 bands=[1350, 1965, 2580, 3195, 3810, 4424, 5039, 5653],
                 upShift=4800, downShift=1750, minRpm=1350, maxRpm=5653,
                 rim=(17, 15, 20), clearance=(.26, .42),
                 capabilities=dict(radio=True, drift=True)),
    'gto': dict(display='Pontiac GTO', short='GTO', kit='KIT00',
               paint=(.10, .10, .12), preset='NFVU2_GTO_BLACK', mass=1700,
               gears=[3.37, 2.32, 1.55, 1.00, .74],
               bands=[800, 1400, 2000, 2600, 3200, 3800, 4400, 5121],
               upShift=4700, downShift=1150, minRpm=750, maxRpm=5121,
               rim=(18, 15, 22), clearance=(.28, .44),
               capabilities=dict(radio=True, drift=True)),
    'imprezawrx': dict(display='Subaru Impreza WRX', short='Impreza WRX', kit='KIT00',
                       paint=(.85, .87, .90), preset='NFVU2_IMPREZA_SILVER', mass=1500,
                       gears=[3.60, 2.20, 1.51, 1.14, .89],
                       bands=[1000, 1854, 2707, 3561, 4415, 5268, 6122, 6975],
                       upShift=6000, downShift=1400, minRpm=1000, maxRpm=6975,
                       rim=(18, 15, 20), clearance=(.27, .43),
                       capabilities=dict(radio=True, drift=True)),
    'is300': dict(display='Lexus IS300', short='IS300', kit='KIT00',
                  paint=(.70, .72, .75), preset='NFVU2_IS300_SILVER', mass=1510,
                  gears=[3.73, 2.33, 1.48, 1.00, .76],
                  bands=[1200, 1959, 2717, 3476, 4234, 4993, 5751, 6510],
                  upShift=5600, downShift=1600, minRpm=1200, maxRpm=6510,
                  rim=(18, 15, 22), clearance=(.27, .43),
                  capabilities=dict(radio=True, drift=True)),
    'lancerevo8': dict(display='Mitsubishi Lancer Evo 8', short='Lancer Evo 8', kit='KIT00',
                       paint=(.85, .87, .90), preset='NFVU2_LANCER_SILVER', mass=1540,
                       gears=[3.63, 2.24, 1.51, 1.18, .87],
                       bands=[1250, 2255, 3260, 4265, 5270, 6274, 7279, 8284],
                       upShift=7000, downShift=1650, minRpm=1250, maxRpm=8284,
                       rim=(18, 15, 20), clearance=(.27, .43),
                       capabilities=dict(radio=True, drift=True)),
    'navigator': dict(display='Lincoln Navigator', short='Navigator', kit='KIT00',
                      paint=(.20, .20, .22), preset='NFVU2_NAVIGATOR_GREY', mass=2600,
                      gears=[2.80, 1.55, 1.00, .72],
                      bands=[1000, 1865, 2729, 3594, 4458, 5323, 6187, 7052],
                      upShift=5200, downShift=1400, minRpm=1000, maxRpm=7052,
                      rim=(20, 18, 24), clearance=(.34, .50),
                      capabilities=dict(radio=True, drift=True)),
    'peugot': dict(display='Peugeot 206', short='206', kit='KIT00',
                   paint=(.80, .82, .85), preset='NFVU2_PEUGOT_SILVER', mass=1080,
                   gears=[3.56, 2.19, 1.48, 1.00, .84],
                   bands=[1500, 2308, 3116, 3925, 4733, 5541, 6350, 7158],
                   upShift=6100, downShift=1900, minRpm=1500, maxRpm=7158,
                   rim=(16, 14, 18), clearance=(.25, .40),
                   capabilities=dict(radio=True, drift=True)),
    'peugot106': dict(display='Peugeot 106', short='106', kit='KIT00',
                      paint=(.82, .84, .86), preset='NFVU2_PEUGOT106_SILVER', mass=970,
                      gears=[3.56, 2.19, 1.48, 1.00, .84],
                      bands=[1500, 2308, 3116, 3925, 4733, 5541, 6350, 7158],
                      upShift=6100, downShift=1900, minRpm=1500, maxRpm=7158,
                      rim=(15, 14, 18), clearance=(.25, .40),
                      capabilities=dict(radio=True, drift=True)),
    'rsx': dict(display='Acura RSX', short='RSX', kit='KIT00',
               paint=(.86, .87, .89), preset='NFVU2_RSX_SILVER', mass=1290,
               gears=[3.23, 1.99, 1.33, 1.06, .84],
               bands=[1000, 2119, 3239, 4358, 5478, 6597, 7717, 8836],
               upShift=7600, downShift=1400, minRpm=1000, maxRpm=8836,
               rim=(17, 15, 20), clearance=(.26, .42),
               capabilities=dict(radio=True, drift=True)),
    'rx8': dict(display='Mazda RX-8', short='RX-8', kit='KIT00',
               paint=(.70, .10, .10), preset='NFVU2_RX8_RED', mass=1470,
               gears=[3.68, 2.19, 1.41, 1.00, .82, .70],
               bands=[1100, 1977, 2854, 3731, 4608, 5485, 6363, 7239],
               upShift=6600, downShift=1500, minRpm=1100, maxRpm=7239,
               rim=(19, 16, 22), clearance=(.26, .42),
               capabilities=dict(radio=True, drift=True)),
    'sentra': dict(display='Nissan Sentra', short='Sentra', kit='KIT00',
                   paint=(.75, .77, .80), preset='NFVU2_SENTRA_SILVER', mass=1250,
                   gears=[3.53, 2.26, 1.53, 1.00, .81],
                   bands=[1200, 2000, 2800, 3600, 4400, 5200, 6000, 6786],
                   upShift=5800, downShift=1600, minRpm=1200, maxRpm=6786,
                   rim=(17, 15, 20), clearance=(.26, .42),
                   capabilities=dict(radio=True, drift=True)),
    'tiburon': dict(display='Hyundai Tiburon', short='Tiburon', kit='KIT00',
                    paint=(.60, .10, .15), preset='NFVU2_TIBURON_MAROON', mass=1420,
                    gears=[3.42, 2.10, 1.36, 1.02, .80],
                    bands=[1000, 1825, 2649, 3474, 4298, 5123, 5947, 6773],
                    upShift=5700, downShift=1400, minRpm=1000, maxRpm=6773,
                    rim=(18, 15, 20), clearance=(.27, .43),
                    capabilities=dict(radio=True, drift=True)),
}


# Identity and verified visual fit are data, shared conversion lives below.
SPECS['h2'] = dict(display='Hummer H2',short='H2',source='HUMMER',kit='KIT00',
    paint=(1,.72,.015),preset='NFVU2_H2_YELLOW_SPINNER26',mass=2900,
    gears=[3.06,1.63,1,.7],bands=[1000,1500,2000,2500,3000,3600,4200,4782],
    upShift=4100,downShift=1500,minRpm=1000,maxRpm=4782,rim=(26,22,28),clearance=(.22,.375),
    capabilities=dict(radio=True,flight=True,suvEngine=True,suvTuning=True),
    wheelPack='SPINNER',wheelFamily='SPINNER_STYLE01_26_34',spinner=True,
    brakeFamily='BRAKE_STYLE03',brakeScale=1.12,visualLift=.30,visualRadius=.4364,tyreWidth=.34,
    spinnerOffset=.185,paintMetallic=.25,chromeMetallic=.28,rubberColor=(.13,.13,.14),
    glassSmoothness=.9,paintWithoutTexture=True,chromeWithoutTexture=True,prefabName='H2_Local_Prototype',sectionNames='{name}_A_{section}_{role}',
    lampBands=[('taillight',.55,100),('reverse',-100,.32)],
    badgeRows=dict(family='HUMMER_KIT00_REAR_BUMPER',texture='HUMMER_BADGING',
                   height=(.30,.41),halfWidth=.42,splitV=.511),
    rigOverrides=dict(boxCenterY=1.02,boxSizeX=1.92,boxSizeY=1.42,boxSizeZ=4.35,
        trackWidth=.881,wheelHeight=.5,frontAxleZ=1.521,rearAxleZ=1.58538,
        spawnLift=.3,clearanceY=-.01,trunkApproachZ=1.8,spinners=True))

SPECS['350z']=dict(source='350Z',presetAssembler='dday')


def source_name(ident):
    return SPECS.get(ident.lower(),{}).get('source',ident.upper())


def compact(part):
    used=sorted(set(part['indices'])); lookup={old:new for new,old in enumerate(used)}
    for field in ('vertices','normals','uv'):part[field]=[part[field][i] for i in used]
    part['indices']=[lookup[i] for i in part['indices']]
    return part


def moving_part(name):
    if 'TRUNK_AUDIO' in name:return ''
    if 'DOOR_LEFT' in name or name.endswith('LEFT_SIDE_MIRROR'):return 'leftDoor'
    if 'DOOR_RIGHT' in name or name.endswith('RIGHT_SIDE_MIRROR'):return 'rightDoor'
    if 'HOOD' in name:return 'hood'
    if 'TRUNK' in name:return 'trunk'
    return ''


def family(name):
    for suffix in LOD_SUFFIXES:
        if name.endswith(suffix):
            return name[:-len(suffix)]
    return name


def pick(parts, name):
    # Geometry headers truncate long mesh names, so families are matched by name
    # prefix and the densest LOD is the one the game shows up close.
    matches = [p for p in parts if p['key']==bin_hash(name+'_A')] or [p for p in parts if family(p['name']) == name]
    if not matches:
        raise ValueError('Missing original mesh: ' + name)
    return max(matches, key=lambda p: len(p['vertices']))


def extract(car):
    extract_matching(ASSETS, lambda upper: upper.startswith(
        (f'CARS/{car}/', f'CARS/MIRRORS_{car}/', 'CARS/SPOILER/', 'CARS/AUDIO/', 'CARS/EXHAUST/', 'CARS/WHEELS/', 'CARS/BRAKES/', 'CARS/NEON/', 'CARS/MIRRORS/', 'GLOBAL/'))
        or upper == 'CARS/TEXTURES.BIN')


def global_record():
    path = LOCAL / 'global-decompressed.bin'
    data = path.read_bytes() if path.is_file() else jdlz((ASSETS / 'GLOBAL/GlobalB.lzc').read_bytes())
    path.write_bytes(data)
    return data


def wheel_locators(data, car):
    record = data.find(f'{car}\0'.encode())
    if record < 0:
        raise ValueError(f'No global record for {car}')
    group = [struct.unpack_from('<3f', data, record + 0x120 + i * 0x30) for i in range(4)]
    xs, ys, zs = ([g[i] for g in group] for i in range(3))
    if not (all(1.0 < abs(x) < 2.3 for x in xs) and all(0.55 < abs(y) < 1.25 for y in ys)
            and all(-0.2 < z < 0.45 for z in zs) and len({x > 0 for x in xs}) == 2):
        raise ValueError(f'Re-identify {car} wheel locators at {record:#x}+0x120')
    return group


def export(ident, output_root=LOCAL):
    ident=ident.lower()
    car=source_name(ident)
    spec = SPECS.get(ident)
    if spec is None:
        raise SystemExit('No importer spec for ' + car + '; add one to SPECS')
    if spec.get('presetAssembler'):
        extract(car)
        from vehicle_preset import export_dday, profile
        export_dday(Path(output_root)/'model')
        folder=Path(output_root)/ident;folder.mkdir(parents=True,exist_ok=True)
        (folder/'profile.json').write_text(json.dumps([profile()],indent=2),encoding='utf-8')
        return
    extract(car)
    out = Path(output_root) / ident
    out.mkdir(parents=True, exist_ok=True)
    meshes = parse(ASSETS / f'CARS/{car}/GEOMETRY.BIN')
    kit = f'{car}_{spec["kit"]}_'
    families = sorted({family(p['name']) for p in meshes
                       if (p['name'].startswith(kit) or family(p['name']) == f'{car}_BASE')
                       and not any(word in p['name'] for word in SKIP)})
    # Geometry headers truncate mesh names at 24 characters, so `_FRONT_WHEEL`
    # arrives as `_FRONT_WHEE` and `_FRONT_BRAKE` as `_FRONT_BRAK`. Match on the
    # ending only: `_BRAKELIGHT` is a tail lamp panel, part of the body.
    body = [n for n in families if not n.endswith(WHEEL_SUFFIX) and not n.endswith(BRAKE_SUFFIX)]
    brakes = [n for n in families if n.endswith(BRAKE_SUFFIX)]
    # A few cars ship no wheel mesh of their own; NFSU2 renders them from the
    # shared wheel packs, which the importer does not resolve.
    wheel_family = spec.get('wheelFamily') or next((n for n in families if n.endswith(WHEEL_SUFFIX)), None)
    if wheel_family:
        wheel = pick(parse(ASSETS/f'CARS/WHEELS/GEOMETRY_{spec["wheelPack"]}.BIN') if spec.get('wheelPack') else meshes, wheel_family)
    else:
        raise SystemExit(f'{car} has no wheel mesh in its own geometry; NFSU2 renders it '
                         'from a shared wheel pack, which the importer does not resolve')
    mounts = next((p['sockets'] for n in body for p in [pick(meshes,n)]
                   if 'REAR_BUM' in n and any(k in p['sockets'] for k in (0xbcf8a18b,0xbd7cf15e))), {})
    exhaust_mounts=[mounts[k] for k in (0xbcf8a18b,0xbd7cf15e) if k in mounts]
    selected=[]
    spoiler_socket=pick(meshes,car+'_BASE').get('sockets',{}).get(0xc93b73fd)
    for n in body:
        if 'EXHAUST' in n:
            if not exhaust_mounts:raise ValueError('Missing original exhaust attachment for '+car)
            selected.extend((pick(meshes,n),'body',n if i==0 else n+'_DUAL',socket) for i,socket in enumerate(exhaust_mounts))
        elif 'SPOILER' in n:
            if spoiler_socket is None:raise ValueError('Missing original spoiler attachment for '+car)
            selected.append((pick(meshes,n),'body',n,spoiler_socket))
        else:selected.append((pick(meshes,n),'body',n,None))
    selected.append((wheel, 'wheel', wheel_family,None))
    if spec.get('spinner'):
        selected.append((pick(parse(ASSETS/f'CARS/WHEELS/GEOMETRY_{spec["wheelPack"]}.BIN'),wheel_family+'_SPIN'),'spinner',wheel_family+'_SPIN',None))
    if spec.get('brakeFamily'):
        brakes=[spec['brakeFamily']]
        selected.append((sport_brake_geometry(pick(parse(ASSETS/'CARS/BRAKES/GEOMETRY.BIN'),brakes[0])),'brake',brakes[0],None))
    else:selected.extend((pick(meshes, n), 'brake', n,None) for n in brakes)

    exhaust_variants=[]
    if exhaust_mounts:
        exhaust_meshes=parse(ASSETS/'CARS/EXHAUST/GEOMETRY.BIN')
        selected=[entry for entry in selected if 'EXHAUST' not in entry[2]]
        for style in range(1,11):
            name=f'EXHAUST_STYLE{style:02d}_LEVEL1';mesh=pick(exhaust_meshes,name)
            selected.extend((mesh,'body',name if i==0 else name+'_DUAL',socket) for i,socket in enumerate(exhaust_mounts))
            exhaust_variants.append((style,mesh))

    locators = wheel_locators(global_record(), car)
    front, rear = locators[0], locators[2]

    def bounds(names):
        points = [v for n in names for v in pick(meshes, n)['vertices']]
        return [(min(p[a] for p in points), max(p[a] for p in points)) for a in range(3)]

    length, width, height = bounds(body)
    # NFSU2 hangs the exhaust from the underfloor and on some cars its pipe sits far
    # below the chassis floor, so lifting by the lowest body point leaves the tyres
    # above the origin. The chassis floor is what has to clear the ground.
    chassis = [n for n in body if 'EXHAUST' not in n] or body
    floor = min(min(p[2] for p in pick(meshes, n)['vertices']) for n in chassis)
    lift = spec.get('visualLift',max(0.0, -floor) + 0.02)

    source_radius=wheel_source_radius(wheel)
    radius = spec.get('visualRadius',source_radius)
    # Original locators are hub centres in the body frame. Align the rolling
    # surface with the ground, not the lowest chassis/exhaust vertex.
    if 'visualLift' not in spec:lift=radius-front[2]
    radial=radius/source_radius
    rubber = {i for s in wheel['slices'] if s['materialKey'] == RUBBER for i in s['indices']}
    axial_positions = [wheel['vertices'][i][1] for i in rubber]
    source_width=max(axial_positions)-min(axial_positions)
    tyre_width = spec.get('tyreWidth',source_width)
    axial=tyre_width/source_width
    inner = min(axial_positions)
    rotor_radius = max((math.hypot(v[0], v[2]) for p,g,_,_ in selected if g=='brake' for v in p['vertices']),default=radius)
    brake_scale = spec.get('brakeScale',(.5 * radius) / rotor_radius)

    packs = [TexturePack(ASSETS / f'CARS/{car}/TEXTURES.BIN'), TexturePack(ASSETS / 'CARS/TEXTURES.BIN'),
             TexturePack(ASSETS / 'CARS/WHEELS/TEXTURES.BIN'), TexturePack(LOCAL / 'global-decompressed.bin'),
             TexturePack(ASSETS / 'GLOBAL/InGameCommon.bun')]
    textures = []
    cache = {}

    def texture(key):
        if key in cache:
            return cache[key]
        for pack in packs:
            if key in pack.slots or key in pack.records:
                _, image, _ = pack.decode(key)
                image = image.convert('RGBA') if key in (bin_hash('SFX_SMOKE_B_CS'),bin_hash(f'{car}_NEON'),bin_hash(f'{car}_BADGING')) else image.convert('RGB').convert('RGBA')
                name = f'{ident}_{key:08x}'
                cache[key] = len(textures)
                textures.append(dict(name=name, width=image.width, height=image.height,
                                     rgba=base64.b64encode(image.tobytes()).decode()))
                image.save(out / (name + '.png'))
                return cache[key]
        return -1

    materials = []
    material_ids = {}

    def material(key, tex, group,showroom=False,badge=False):
        identity = (key, tex, group,showroom,badge)
        if identity in material_ids:
            return material_ids[identity]
        color, metal, smooth = [1., 1., 1., 1.], .08, .45
        if key == PAINT:
            color = [*spec['paint'], 1.]
            metal, smooth = spec.get('paintMetallic',.55), .7
            if spec.get('paintWithoutTexture'):tex=-1
        elif key in CHROME or group=='spinner':
            color, metal, smooth = [.94, .96, 1., 1.], spec.get('chromeMetallic',.85), .8
            if group in ('wheel','spinner') and tex>=0 and not spec.get('chromeWithoutTexture'):color=[1.,1.,1.,1.]
            else:tex=-1
        elif key in GLASS:
            color, metal, smooth = [.04, .08, .1, 1.], .5, .95
            tex = -1
            smooth=spec.get('glassSmoothness',smooth)
        elif key == RUBBER:
            color, smooth = [*spec.get('rubberColor',(.8,.8,.8)), 1.], .12
        elif key in DARK:
            color, smooth = [.16, .16, .18, 1.], .15
        elif key == CALIPER:
            color, metal, smooth = [.8, .025, .018, 1.], .45, .65
            tex = -1
        elif tex < 0:
            color = [.12, .12, .14, 1.]
        if key==LENS and tex<0 and group=='body':color,metal,smooth=[.32,.015,.02,1.],.05,.75
        if showroom:
            color=[1.,1.,1.,1.] if tex>=0 else [.24,.25,.27,1.]
            metal,smooth=.18,.4
        material_ids[identity] = len(materials)
        materials.append(dict(name=f'{key:08x}_{group}_{len(materials)}', color=color,
                              metallic=metal, smoothness=smooth, texture=tex, bodyPaint=key == PAINT and group=='body' and not showroom,alphaMode='blend' if badge else 'opaque'))
        return len(materials) - 1

    def vertex(v, group, normal=False, socket=None,exhaust_axes=True):
        x, y, z = v
        if socket:
            x,y,z=source_attachment(v,socket,normal,exhaust_axes)
        if group == 'body':
            result = (y, z + (0 if normal else lift), x)
        elif group == 'wheel':
            # Source axis +Y is the tyre's axial direction; the mesh keeps its
            # native radius and width, so only the normal needs the sign flip.
            result = (-y/axial, z/radial, x/radial) if normal else (tyre_width / 2 - (y - inner)*axial, z*radial, x*radial)
        elif group=='spinner':
            result=(-y,z,x) if normal else (spec.get('spinnerOffset',tyre_width/2+.015)-y*radial,z*radial,x*radial)
        elif group=='audio':
            result=(x,y,z)
        else:
            result = (-y, z, x) if normal else ((-y + .04) * brake_scale, z * brake_scale, x * brake_scale)
        if normal:
            length = math.sqrt(sum(c * c for c in result))
            result = tuple(c / length for c in result) if length else (0, 1, 0)
        return dict(zip(('x', 'y', 'z'), result))

    animations=showroom_assets.animations(ASSETS/f'CARS/{car}/PARTS_ANIMATIONS.bin',lift,car)
    def transform_audio(v,m,normal=False):
        point=tuple(sum(v[b]*m[b*4+a] for b in range(3))+(0 if normal else m[12+a]) for a in range(3))
        return vertex(point,'body',normal)
    audio_meshes=parse(ASSETS/'CARS/AUDIO/GEOMETRY.BIN')
    showroom,layouts,components=showroom_assets.catalog(meshes,audio_meshes,pick,transform_audio,car)
    selected.extend((part,group,name,None) for part,group,_,name in showroom)
    engine_names=[f'{car}_{style}_ENGINE{suffix}' for style in ('KIT00','STYLE01','STYLE02') for suffix in ('','_UNLIT') if any(p['key']==bin_hash(f'{car}_{style}_ENGINE{suffix}_A') for p in meshes)]
    selected.extend((pick(meshes,n),'body',n,None) for n in engine_names)
    parts = []
    lamps = {}

    def uv(value):
        # Some NFSU2 meshes tile their atlas far outside the unit square (the
        # Skyline front bumper reaches u=203). Unity repeats textures, so the
        # same pixels are addressed inside it, and the runtime loader only
        # accepts bounded coordinates.
        return value - math.floor(value) if abs(value) > 100 else value

    for part, group, name, socket in selected:
        keys={s['materialKey'] for s in part['slices']}
        head_keys=keys.intersection(HEAD_LENS) or keys.intersection((0x54949afd,))
        tail_keys=keys.intersection(TAIL_LENS) or keys.intersection((0x12c9453c,))
        for number, section in enumerate(part['slices']):
            key = section['materialKey']
            if key in CLEAR:
                continue
            texture_key=section['textureKey']
            for label in ('HEADLIGHT','BRAKELIGHT'):
                if label in name:
                    candidate=bin_hash(name.split(label)[0]+label+'_OFF')
                    if any(candidate in pack.slots or candidate in pack.records for pack in packs):texture_key=candidate
            tex = texture(texture_key)
            role = ('headlight' if 'HEADLIGHT' in name and key in head_keys
                    else 'taillight' if 'BRAKELIGHT' in name and key in tail_keys else 'brakelight' if 'TRUNK' in name and 'AUDIO' not in name and key==LENS else '')
            ids = section['indices'][:]
            if group == 'wheel' and key == LIP:
                ids = [i for j in range(0, len(ids), 3)
                       if all(math.hypot(part['vertices'][k][0], part['vertices'][k][2]) <= source_radius * 1.01
                              for k in ids[j:j + 3]) for i in ids[j:j + 3]]
            if not ids:
                continue
            if group in ('wheel','spinner','brake'):
                for i in range(0, len(ids), 3):
                    ids[i + 1], ids[i + 2] = ids[i + 2], ids[i + 1]
            groups={role:ids}
            if 'BRAKELIGHT' in name and role and spec.get('lampBands'):
                groups={'':[]}
                for i in range(0,len(ids),3):
                    tri=ids[i:i+3];u=sum(part['uv'][j][0] for j in tri)/3
                    label=next((label for label,lo,hi in spec['lampBands'] if lo<u<hi),'')
                    groups.setdefault(label,[]).extend(tri)
            coords=[dict(u=uv(u),v=uv(1-v)) for u,v in part['uv']]
            badge=spec.get('badgeRows')
            is_badge=texture_key==bin_hash(f'{car}_BADGING') and key==BADGE_OVERLAY
            if is_badge and not (badge and name==badge['family']):coords=badge_uv(part,section,coords)
            if badge and name==badge['family'] and texture_key==bin_hash(badge['texture']):
                coords=badge_rows_uv(part,section,coords,badge)
            option_group='audio' if group=='audio' else 'trunk' if 'TRUNK_AUDIO' in name else 'engine' if 'ENGINE' in name else 'exhaust' if name.startswith('EXHAUST_STYLE') else ''
            option=name if group=='audio' else str(int(name.split('_')[1][-2:])) if option_group else ''
            for label,triangles in groups.items():
                if not triangles:continue
                if label and label!='brakelight':
                    for index in triangles:
                        x,y,z=part['vertices'][index];lamps.setdefault((label,y>0),[]).append((x,y,z))
                parts.append(compact(dict(name=spec.get('sectionNames','{name}_A_{section}').format(name=name,section=number,role=label),group=group,lightRole=label,
                    movingPart=moving_part(name) if group=='body' else '',optionGroup=option_group,option=option,
                    material=material(key,tex,group,group=='audio' or option_group in ('engine','trunk'),is_badge),rotatesWithWheel=group=='brake' and (key==0xb1aae6bb if spec.get('brakeFamily') else key!=CALIPER),
                    vertices=[vertex(v,group,socket=socket,exhaust_axes='SPOILER' not in name) for v in part['vertices']],
                    normals=[vertex(v,group,True,socket,exhaust_axes='SPOILER' not in name) for v in part['normals']],uv=coords,indices=triangles)))

    lights = []
    for (role, right), points in lamps.items():
        side = 1 if right else -1
        x = sum(p[0] for p in points) / len(points)
        y = side * sum(abs(p[1]) for p in points) / len(points)
        z = sum(p[2] for p in points) / len(points)
        lights.append(dict(role=role, position=dict(x=y, y=z + lift, z=x),
                           direction=dict(x=0, y=-.12, z=1 if role == 'headlight' else -1)))

    model = dict(schemaVersion=3, preset=spec['preset'], parts=parts, materials=materials,
                 textures=textures, lights=lights, neonTexture=texture(bin_hash(f'{car}_NEON')),
                 nitroFlameTexture=texture(bin_hash('SFX_FIREB')), nitroPurgeTexture=texture(bin_hash('SFX_SMOKE_B_CS')),
                 previewHubHeight=front[2] + lift,suspensionHubAtRest='visualLift' not in spec,
                 animations=animations,trunkLayouts=layouts,audioComponents=components)
    from car_effects import nitro_emitters
    fallback=[vertex((socket[12],socket[13],socket[14]),'body') for socket in exhaust_mounts]
    model['effectEmitters']=nitro_emitters([p for p in parts if p.get('optionGroup')!='exhaust'],abs(front[1]),abs(front[0]),fallback)
    if exhaust_variants:
        variants=[]
        for style,mesh in exhaust_variants:
            emitters=[]
            for socket in exhaust_mounts:
                for mouth in exhaust_mouths(mesh):
                    position=vertex((mouth[0]-.006,mouth[1],mouth[2]),'body',socket=socket)
                    direction=vertex((-1,0,0),'body',True,socket)
                    emitters.append(dict(role='exhaust',position=position,direction=direction))
            variants.append(dict(style=style,emitters=emitters))
        model['exhaustVariants']=variants
        model['effectEmitters']=max(variants,key=lambda v:len(v['emitters']))['emitters']+[e for e in model['effectEmitters'] if e['role']!='exhaust']
    (out / f'{ident}.json').write_text(json.dumps(model, separators=(',', ':')), encoding='utf-8')
    rig = dict(trackWidth=round(abs(front[1]), 4), frontAxleZ=round(abs(front[0]), 4),
               rearAxleZ=round(abs(rear[0]), 4), wheelRadius=round(radius, 4),
               boxSizeX=round(width[1] - width[0], 4), boxSizeZ=round(length[1] - length[0], 4),
               boxSizeY=round(height[1] - height[0], 4), spawnLift=round(lift, 4))
    profile = dict(id=ident, displayName=spec['display'], shortName=spec['short'],
                   rig=dict(boxCenterY=round((height[0] + height[1]) / 2 + lift, 4),
                            boxSizeX=round((width[1] - width[0]) * .95, 4),
                            boxSizeY=round((height[1] - height[0]) * .65, 4),
                            boxSizeZ=round((length[1] - length[0]) * .95, 4), mass=spec['mass'],
                            trackWidth=rig['trackWidth'], wheelHeight=rig['wheelRadius'],
                            rearWheelHeight=round(rear[2]+lift,4) if 'visualLift' not in spec else 0,
                            rearTrackWidth=round(abs(rear[1]),4),
                            frontAxleZ=rig['frontAxleZ'], rearAxleZ=rig['rearAxleZ'],
                            wheelRadius=rig['wheelRadius'], spawnLift=rig['spawnLift'],
                            clearanceX=spec['clearance'][0], clearanceZ=spec['clearance'][1],
                            rimNominal=spec['rim'][0], rimMin=spec['rim'][1], rimMax=spec['rim'][2]),
                   capabilities=dict(spec['capabilities'],nitro=True,neon=True,showroom=True,exhaust=bool(exhaust_variants)),
                   sound=dict(gearRatios=spec['gears'], rpmBands=spec['bands'],
                              upShiftRpm=spec['upShift'], downShiftRpm=spec['downShift'],
                              minRpm=spec['minRpm'], maxRpm=spec['maxRpm']),
                   assets=dict(modelFile=f'{ident}/{ident}.json',
                               audioFolders=['audio/effects', f'audio/{ident}'],
                               engineFolder=f'audio/{ident}'))
    profile['rig'].update(spec.get('rigOverrides',{}))
    profile['summary']='Original vehicle parts, opening panels, engine bay and trunk audio.'
    if 'prefabName' in spec:profile['prefabName']=spec['prefabName']
    (out / 'profile.json').write_text(json.dumps([profile], indent=2), encoding='utf-8')
    (out / 'provenance.json').write_text(json.dumps(dict(
        body=body, wheel=wheel_family, brakes=brakes, wheelLocators=locators, bodyLift=lift,
        tyreRadius=radius, tyreWidth=tyre_width, brakeScale=brake_scale, rig=rig,
        paint='Authored body colour; NFSU2 stores paint as a material tint, not a texture',
        excluded='Decals and body-kit variants use the separate customization catalog.'), indent=2),
        encoding='utf-8')
    print(f'{car}: {len(parts)} sections; {len(textures)} textures; locators {locators}')
    print('rig', json.dumps(rig))


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('car',help='vehicle id (e.g. h2, skyline); use all for all stock imports')
    parser.add_argument('--output',type=Path,default=LOCAL,help='generated model/profile root')
    args=parser.parse_args()
    for ident in sorted(SPECS) if args.car.lower()=='all' else [args.car.lower()]:
        export(ident,args.output)
        if args.output.resolve()==LOCAL.resolve() and (LOCAL/'styling'/f'{ident}.json').exists():
            from export_customization import export as styling_export
            styling_export(json.loads((LOCAL/ident/'profile.json').read_text())[0])


if __name__ == '__main__':main()
