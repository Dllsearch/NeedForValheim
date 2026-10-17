"""Import original U2/MW custom tachometers; local output only.

Texture names are truncated in the archive, so keys identify the exact variant.
Layout, colours and live values are adapted by the Valheim HUD, not NFS scripts.
"""
import base64,json
from PIL import Image
from project import LOCAL,config,mw_root
from nfs_formats import TexturePack,bin_hash

def rgba(name,image):
    image=image.convert('RGBA')
    return dict(name=name,width=image.width,height=image.height,rgba=base64.b64encode(image.tobytes()).decode())

def mw_designs(root):
    designs=[]
    for i in range(11):
        suffix=f'{i:02d}';pack=TexturePack(root/'GLOBAL'/f'HUDS_Custom_{suffix}.bin')
        def read(prefix):return pack.decode(bin_hash(prefix+suffix))[1].convert('RGBA')
        def art(prefix):
            image=read(prefix);image.thumbnail((256,256),Image.Resampling.LANCZOS)
            return rgba(prefix+suffix,image)
        # Native needle points DOWN from (64,256). Rotate around its authored
        # pivot, rather than rotating a stretched rectangle around its end.
        needle=read('TACH_NEEDLE_').crop((44,208,84,512)).transpose(Image.Transpose.ROTATE_180)
        needle=needle.resize((20,152),Image.Resampling.LANCZOS)
        designs.append(dict(name=f'MW {i+1:02d}',kind='mw-tach',fill=art('TACH_FILL_'),
            lines=art('8000_LINES_'),needle=rgba('TACH_NEEDLE_'+suffix,needle),needlePivot=255/304,
            turboLines=art('TURBO_LINES_')))
    return designs

def export():
    base=LOCAL/'nfs-assets/GLOBAL';designs=[]
    for i in range(11):
        suffix=f'{i:02d}';pack=TexturePack(base/f'HUD_CustomTextures_{suffix}.bin')
        def read(prefix):return pack.decode(bin_hash(prefix+suffix))[1].convert('RGBA')
        # The original UI puts the artwork in the top-left 140px (70px for 10)
        # of a padded texture. Preserve that authored square, not the empty pad.
        size=70 if i==10 else 140
        fill=read('TACH_FILL_CUSTOM_').crop((0,0,size,size))
        lines=read('3RDPERSON_7500LINES_CUSTOM_')
        needle=read('3RDPERSON_RPMDIAL_CUSTOM_').crop((0,56,16,128)).transpose(Image.Transpose.ROTATE_180)
        # Turbo artwork uses the top-left 72px square in every style. The
        # authored dial runs -30..0..30; it is not a second speedometer.
        turbo_fill=read('3RDPERSON_TURBOFILL_CUSTOM_').crop((0,0,72,72))
        turbo_lines=read('3RDPERSON_TURBOLINES_CUSTOM_').crop((0,0,72,72))
        designs.append(dict(name=f'NFSU2 {i+1:02d}',fill=rgba('fill',fill),lines=rgba('lines',lines),needle=rgba('needle',needle),needlePivot=63/72,
                            turboFill=rgba('turboFill',turbo_fill),turboLines=rgba('turboLines',turbo_lines)))
    race=TexturePack(base/'InGameRace.bun')
    if config().get('mw_root'):designs+=mw_designs(mw_root())
    data=dict(schemaVersion=1,designs=designs,
              nitro=rgba('TACH_NOS_ALPHA',race.decode(bin_hash('TACH_NOS_ALPHA'))[1]),
              nitroBacking=rgba('TACH_NOS_BACKING',race.decode(bin_hash('TACH_NOS_BACKING'))[1]))
    if config().get('mw_root'):
        meter=TexturePack(mw_root()/'GLOBAL/HUDTEXRACE.BIN').decode(bin_hash('METER_BACKING2'))[1].convert('RGBA')
        meter.thumbnail((256,256),Image.Resampling.LANCZOS)
        data['mwNitro']=rgba('METER_BACKING2',meter)
    out=LOCAL/'hud';out.mkdir(exist_ok=True)
    (out/'hud.json').write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
    (out/'provenance.json').write_text(json.dumps({'source':'GLOBAL/HUD_CustomTextures_00..10.bin and GLOBAL/InGameRace.bun',
        'art':'Original TACH_FILL_CUSTOM, 7500LINES, RPMDIAL, TURBOFILL, TURBOLINES, TACH_NOS_ALPHA and TACH_NOS_BACKING',
        'mwSource':'GLOBAL/HUDS_Custom_00..10.bin: TACH_FILL, 8000_LINES, TACH_NEEDLE, TURBO_LINES' if len(designs)>11 else None,
        'adaptation':'Original tachometer faces, authored tick labels and needles with preserved pivots. NFVU2 places live numeric speed/gear and auxiliary NOS/boost telemetry; MW NOS uses METER_BACKING2 and HUD_SingleRace FNG object 931705: rotation 140 degrees, size 152x150 relative to 140px tach face. Live meter clipping and telemetry remain NFVU2 adaptations.'},indent=2),encoding='utf-8')
    print(f'Exported {len(designs)} U2/MW gauge designs to .local/hud/hud.json')
    return data

if __name__=='__main__':export()
