from PIL import Image, ImageDraw, ImageFont, ImageFilter
import os

RAW='raw'; CUT='cut'
os.makedirs(CUT, exist_ok=True)
FONT='/System/Library/Fonts/Supplemental/Arial Rounded Bold.ttf'

def cutout(name, out, extra_seeds=()):
    im = Image.open(os.path.join(RAW,name)).convert('RGB')
    w,h = im.size
    probe = im.copy()
    seeds = [(0,0),(w-1,0),(0,h-1),(w-1,h-1),(w//2,0),(w//2,h-1),(0,h//2),(w-1,h//2)]
    seeds += [(int(x*w),int(y*h)) for x,y in extra_seeds]
    for s in seeds:
        try: ImageDraw.floodfill(probe, s, (255,0,255), thresh=38)
        except Exception: pass
    mask = Image.new('L',(w,h),0)
    px=probe.load(); mp=mask.load()
    for y in range(h):
        for x in range(w):
            mp[x,y] = 0 if px[x,y]==(255,0,255) else 255
    mask = mask.filter(ImageFilter.GaussianBlur(1.2))
    im = im.convert('RGBA'); im.putalpha(mask)
    im = im.crop(im.getbbox())
    im.save(os.path.join(CUT,out))
    return im

def nine(im, W, H, fx, fy, dbx, dby):
    """fx/fy = border as FRACTION of source. dbx/dby = border in DEST px."""
    w,h = im.size
    bx, by = int(w*fx), int(h*fy)
    dbx, dby = min(dbx, W//2-1), min(dby, H//2-1)
    out = Image.new('RGBA',(W,H),(0,0,0,0))
    cols=[(0,bx),(bx,w-bx),(w-bx,w)]; rows=[(0,by),(by,h-by),(h-by,h)]
    dcols=[(0,dbx),(dbx,W-dbx),(W-dbx,W)]; drows=[(0,dby),(dby,H-dby),(H-dby,H)]
    for r,(y0,y1) in enumerate(rows):
        for c,(x0,x1) in enumerate(cols):
            dx0,dx1=dcols[c]; dy0,dy1=drows[r]
            tw,th=max(1,dx1-dx0),max(1,dy1-dy0)
            out.alpha_composite(im.crop((x0,y0,x1,y1)).resize((tw,th), Image.LANCZOS),(dx0,dy0))
    return out

def circle(path, size):
    im = Image.open(path).convert('RGBA').resize((size,size), Image.LANCZOS)
    m = Image.new('L',(size,size),0)
    ImageDraw.Draw(m).ellipse((0,0,size-1,size-1), fill=255)
    im.putalpha(m); return im

def txt(d,xy,s,px,fill,stroke,sw,anchor='mm'):
    d.text(xy,s,font=ImageFont.truetype(FONT,px),fill=fill,stroke_fill=stroke,stroke_width=sw,anchor=anchor)

P = {
 'frame': cutout('frame_b.png','frame.png'),
 'panel': cutout('panel_b.jpg','panel.png'),
 'pill':  cutout('pill_b.png','pill.png'),
 'med':   cutout('medallion_b.jpg','medallion.png', extra_seeds=[(0.50,0.62)]),
 'rays':  cutout('rays.jpg','rays.png'),
 'star':  cutout('icon_star.jpg','icon_star.png'),
 'book':  cutout('icon_book.jpg','icon_book.png'),
}
for k,v in P.items(): print(k, v.size)

W,H = 1179, 480
NAVY=(24,48,104); CREAM=(255,247,226)

bg = Image.open('../Assets/JellyGarden/Resources/Story/Art/area_001_state3.jpg').convert('RGBA')
bg = bg.resize((W,int(bg.height*W/bg.width)), Image.LANCZOS).crop((0,0,W,H))
c = bg.copy()

# wooden frame bar
c.alpha_composite(nine(P['frame'], W, 290, 0.11, 0.20, 78, 52), (0,160))

# blue panels
pw,ph = 350, 232
pnl = nine(P['panel'], pw, ph, 0.11, 0.13, 34, 30)
c.alpha_composite(pnl,(42,186)); c.alpha_composite(pnl,(W-42-pw,186))

# pills
def pill(w,h): return nine(P['pill'], w, h, 0.14, 0.34, 26, 22)
c.alpha_composite(pill(196,72),(168,262))
c.alpha_composite(pill(196,72),(168,344))
c.alpha_composite(pill(214,110),(W-42-pw+68,282))

# goal icons
for ic0,y in ((P['star'],262),(P['book'],344)):
    ic = ic0.copy(); ic.thumbnail((78,78), Image.LANCZOS)
    c.alpha_composite(ic,(92, y+36-ic.height//2))

# medallion group
cx = W//2
med = P['med']; MW = 340; MH = int(med.height*MW/med.width)
med = med.resize((MW,MH), Image.LANCZOS)
mx, my = cx-MW//2, 42
hole_cy = my + int(MH*0.625); hole_d = int(MW*0.60)
c.alpha_composite(P['rays'].resize((hole_d,hole_d), Image.LANCZOS),(cx-hole_d//2, hole_cy-hole_d//2))
c.alpha_composite(circle('../Assets/JellyGarden/Resources/Story/Art/avatar_leo_00.png', hole_d),(cx-hole_d//2, hole_cy-hole_d//2))
c.alpha_composite(med,(mx,my))

d = ImageDraw.Draw(c)
txt(d,(42+pw//2,226),'TARGETS',42,CREAM,NAVY,5)
txt(d,(W-42-pw//2,226),'MOVES',42,CREAM,NAVY,5)
txt(d,(266,298),'100',46,NAVY,CREAM,3)
txt(d,(266,380),'4',46,NAVY,CREAM,3)
txt(d,(W-42-pw+68+107,337),'6',80,NAVY,CREAM,4)

c.convert('RGB').save('mockup_hud.png')
print('ok', c.size)
