"""Standalone U2/MW layout preview using the same imported art and pivots.

Real IMGUI snapshots use tools/run_visual_audit.py.
This is not a Unity runtime test. Preview images remain in ignored artifacts/.
"""
import base64,json,math,os
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from project import LOCAL,OUTPUT

def image(t):return Image.frombytes('RGBA',(t['width'],t['height']),base64.b64decode(t['rgba']))

def render(design,charge=.65,rpm=4500,speed=86,mph=False,turbo_pressure=18,turbo_available=True):
    data=json.loads((LOCAL/'hud/hud.json').read_text());d=data['designs'][design]
    if d.get('kind')=='mw-tach':return render_mw(data,d,charge,rpm,speed,mph,turbo_pressure,turbo_available)
    canvas=Image.new('RGBA',(515,400),(19,25,31,255));offset=(190,50)
    def paste(im,box,tint=None):
        im=im.resize((round(box[2]),round(box[3])),Image.Resampling.LANCZOS)
        if tint:
            from PIL import ImageChops
            im=ImageChops.multiply(im,Image.new('RGBA',im.size,tint))
        canvas.alpha_composite(im,(round(box[0]+offset[0]),round(box[1]+offset[1])))
    accent=(60,222,255,255)
    paste(image(d['fill']),(0,30,280,280));paste(image(d['lines']),(12,42,256,256),accent)
    pointer=Image.new('RGBA',(280,280));art=image(d['needle']);height=116/d['needlePivot'];width=height*art.width/art.height
    pointer.alpha_composite(art.resize((round(width),round(height))),(round(140-width/2),24))
    pointer=pointer.rotate(-(180+240*rpm/7500),resample=Image.Resampling.BICUBIC,center=(140,140))
    nos_rect=(-53.33,-58.47,520.45,248.70)  # HudMath.U2Nos*: shared, reference U2 08
    paste(image(data['nitroBacking']),nos_rect,(165,165,165,255))
    nitro=image(data['nitro']);pixels=nitro.load()
    for y in range(nitro.height):
        for x in range(nitro.width):
            progress=0 if x<92.5 and y>=110.5 else max(0,min(1,(math.degrees(math.atan2(y-110.5,x-92.5))+180)/150))
            if charge==0 or progress>charge:pixels[x,y]=(*pixels[x,y][:3],0)
    paste(nitro,nos_rect,accent)
    draw=ImageDraw.Draw(canvas)
    paste(pointer,(0,30,280,280))
    def label(x,y,text,size,color):
        fontpath=Path(os.environ.get('WINDIR','C:/Windows'))/'Fonts/segoeuib.ttf'
        font=ImageFont.truetype(str(fontpath),size) if fontpath.exists() else ImageFont.load_default()
        draw.text((x+offset[0],y+offset[1]),text,font=font,fill=color,anchor='ra')
    label(260,199,str(round(speed)),34,'white');label(260,237,'MPH' if mph else 'KM/H',11,accent);label(249,164,'3',22,'white')
    label(285,316,d['name'],16,'white')
    if turbo_available:
        cx,cy=offset[0]-84,offset[1]+222
        if d.get('turboFill') and d.get('turboLines'):
            paste(image(d['turboFill']),(-152,154,136,136))
            paste(image(d['turboLines']),(-152,154,136,136),accent)
        else:
            draw.ellipse((cx-68,cy-68,cx+68,cy+68),fill=(6,9,11),outline=(77,89,102),width=3)
            for i in range(8):
                a=math.radians(-165+30*i)
                draw.line(((cx+math.sin(a)*54,cy-math.cos(a)*54),(cx+math.sin(a)*61,cy-math.cos(a)*61)),fill=accent,width=2)
            for value in [-30,0,30]:
                a=math.radians(-75+value*(3 if value<0 else 4))
                label(-84+math.sin(a)*43+10,222-math.cos(a)*43-9,str(value),11,'white')
        label(-49,291,'TURBO',11,'white')
        pressure=max(-30,min(30,turbo_pressure)) if math.isfinite(turbo_pressure) else 0
        a=math.radians(-75+pressure*(3 if pressure<0 else 4))
        endpoint=(cx+math.sin(a)*51,cy-math.cos(a)*51)
        draw.line(((cx,cy),endpoint),fill='black',width=6)
        draw.line(((cx,cy),endpoint),fill='white',width=3)
        draw.ellipse((cx-3,cy-3,cx+3,cy+3),fill=accent)
    return canvas

def render_mw(data,d,charge,rpm,speed,mph,pressure,turbo_available):
    canvas=Image.new('RGBA',(360,400),(19,25,31,255));offset=(36,6)
    def paste(im,xy,size):canvas.alpha_composite(im.resize(size,Image.Resampling.LANCZOS),(xy[0]+offset[0],xy[1]+offset[1]))
    from PIL import ImageChops
    accent=(60,222,255,255)
    def tint(im,color):return ImageChops.multiply(im,Image.new('RGBA',im.size,color))
    paste(tint(image(d['fill']),accent),(0,0),(288,288));paste(tint(image(d['lines']),accent),(0,0),(288,288))
    if data.get('mwNitro'):
        def arc(im,center,color):
            im=tint(im,color).resize((313,309),Image.Resampling.LANCZOS).rotate(-140,resample=Image.Resampling.BICUBIC,expand=True)
            canvas.alpha_composite(im,(round(offset[0]+center[0]-im.width/2),round(offset[1]+center[1]-im.height/2)))
        arc(image(data['mwNitro']),(145.03,149.52),(255,255,255,60))
        im=image(data['mwNitro']);pixels=im.load()
        for y in range(im.height):
            for x in range(im.width):
                progress=1-max(0,min(1,(math.degrees(math.atan2((y+.5)/im.height-.5,(x+.5)/im.width-.5))+180)/180))
                if charge==0 or progress>charge:pixels[x,y]=(*pixels[x,y][:3],0)
        arc(im,(145.34,150.08),accent)
    needle=Image.new('RGBA',(576,576));art=image(d['needle']);height=124/d['needlePivot'];width=height*art.width/art.height
    needle.alpha_composite(art.resize((round(width),round(height))),(round(288-width/2),164))
    needle=needle.rotate(-(-115+230*max(0,min(1,rpm/8000))),resample=Image.Resampling.BICUBIC,center=(288,288))
    canvas.alpha_composite(needle,(offset[0]+144-288,round(offset[1]+148.5-288)))
    draw=ImageDraw.Draw(canvas)
    def text(x,y,label,size,fill='white'):
        font=ImageFont.truetype(str(Path(os.environ.get('WINDIR','C:/Windows'))/'Fonts/arialbd.ttf'),size)
        draw.text((offset[0]+x,offset[1]+y),label,anchor='mm',font=font,fill=fill)
    text(144,98,'3',23,'black');text(144,194,str(round(speed)),32,'black');text(144,238,'MPH' if mph else 'KM/H',12)
    text(144,273,'RPM ×1000',10)
    text(17,287,'N2O',10,(60,222,255))
    if turbo_available:
        paste(image(d['turboLines']),(0,312),(288,72));text(144,337,f'{pressure} PSI',12)
        a=math.radians(-40+80*max(0,min(1,(pressure+30)/60)));p=(offset[0]+144+math.sin(a)*201,offset[1]+172+math.cos(a)*201)
        draw.line((p,(p[0]+math.sin(a)*12,p[1]+math.cos(a)*12)),fill='white',width=2)
    text(144,385,d['name'],13)
    return canvas

if __name__=='__main__':
    OUTPUT.mkdir(exist_ok=True)
    sheet=Image.new('RGBA',(515*4,400*3),(12,16,22,255))
    for i in range(11):sheet.alpha_composite(render(i),((i%4)*515,(i//4)*400))
    sheet.convert('RGB').save(OUTPUT/'hud-designs.png')
    render(0).save(OUTPUT/'hud-preview.png')
    states=Image.new('RGBA',(515*4,400),(12,16,22,255))
    for i,pressure in enumerate([-30,0,30]):states.alpha_composite(render(0,turbo_pressure=pressure),(i*515,0))
    states.alpha_composite(render(0,turbo_available=False),(3*515,0))
    states.convert('RGB').save(OUTPUT/'hud-turbo-states.png')
    mw_sheet=Image.new('RGBA',(360*4,400*3),(12,16,22,255))
    for i in range(11):mw_sheet.alpha_composite(render(11+i),((i%4)*360,(i//4)*400))
    mw_sheet.convert('RGB').save(OUTPUT/'hud-mw-designs.png');render(11,rpm=0,speed=0).save(OUTPUT/'hud-mw-idle.png')
    print('HUD previews written to artifacts/')
