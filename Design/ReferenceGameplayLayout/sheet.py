from pathlib import Path
from PIL import Image,ImageDraw
p=Path('Design/ReferenceGameplayLayout/SourceSprites'); files=list(p.glob('*.png')); out=Image.new('RGB',(800,((len(files)+3)//4)*240),(42,48,61));d=ImageDraw.Draw(out)
for n,f in enumerate(files):
 im=Image.open(f);im.thumbnail((180,195));x=(n%4)*200+(200-im.width)//2;y=(n//4)*240;out.paste(im,(x,y),im);d.text(((n%4)*200+5,y+198),f.stem[:28],fill='white')
out.save('Design/ReferenceGameplayLayout/source-ui-sheet.jpg')
