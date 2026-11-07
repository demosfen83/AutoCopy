from pathlib import Path
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parent
S = 1024
sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]

def render(state):
    im = Image.new('RGBA', (S, S))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((0, 0, 1023, 1023) if state else (48, 48, 976, 976), radius=220, fill='#243b53')
    # The two outlines follow the supplied Tabler copy icon on a 24-unit grid.
    scale = 36 if state else 31
    ox, oy = (45, 45) if state else (135, 135)
    def box(v): return tuple(round(v[i] * scale + (ox if i % 2 == 0 else oy)) for i in range(len(v)))
    stroke = round(2 * scale)
    white = '#f7fbff'
    d.rounded_rectangle(box((7, 7, 21, 21)), radius=round(2.667*scale), outline=white, width=stroke)
    def cubic(p0, p1, p2, p3):
        return [tuple((1-t)**3*p0[i] + 3*(1-t)**2*t*p1[i] + 3*(1-t)*t*t*p2[i] + t**3*p3[i] for i in (0,1)) for t in (j/24 for j in range(25))]
    path = cubic((4.012,16.737),(3.39,16.38),(3,15.72),(3,15))
    path += [(3,5)]
    path += cubic((3,5),(3,3.9),(3.9,3),(5,3))
    path += [(15,3)]
    path += cubic((15,3),(15.75,3),(16.158,3.385),(16.5,4))
    d.line([box(p) for p in path], fill=white, width=stroke, joint='curve')
    for x,y in [(4.012,16.737), (16.5,4)]:
        cx, cy = box((x,y)); r=stroke//2; d.ellipse((cx-r,cy-r,cx+r,cy+r),fill=white)
    if state:
        d.ellipse((428, 428, 1023, 1023), fill='#243b53')
        d.ellipse((460, 460, 1000, 1000), fill='#31c48d' if state == 'active' else '#f4b942')
        if state == 'active':
            points = [(555, 735), (674, 849), (904, 599)]
            d.line(points, fill='#102a43', width=88, joint='curve')
            for x, y in points:
                d.ellipse((x-44, y-44, x+44, y+44), fill='#102a43')
        else:
            d.rounded_rectangle((587, 578, 675, 878), radius=28, fill='#102a43')
            d.rounded_rectangle((780, 578, 868, 878), radius=28, fill='#102a43')
    return im

for state, name in [(None, 'app'), ('active', 'active'), ('paused', 'paused')]:
    img = render(state)
    img.save(out / f'{name}.ico', format='ICO', sizes=[(n,n) for n in sizes])
    img.resize((256,256), Image.Resampling.LANCZOS).save(out / f'{name}-preview.png')



