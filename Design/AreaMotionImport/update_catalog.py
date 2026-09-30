exec((__import__('pathlib').Path(__file__).parent/'export_unity.py').read_text().split('before=AUDIT')[0])
p=PROJECT/'Assets/JellyGarden/Resources/RoyalAreas/areas.json';catalog=json.loads(p.read_text())
costs=[[1,1,2,2,2,2],[1,2,1,2,2,2,2],[1,2,1,2,2,2,2,1,2]]
names=['Castle Gardens','Throne Room','Royal Dining Room'];subtitles=['Bring the royal gardens to life','Restore the heart of the kingdom','Prepare a banquet for the court']
for area in range(1,4):
 a=catalog['areas'][area-1];d=json.loads((SOURCE/f'assets/Area{area:02}/runtime.json').read_text());orig=PROJECT.parent/f'RoyalThrone-Reference-Assets-Review/Area{area:02}/Original-Assembled';ui=ROOT/f'Area{area:02}/UI';ui.mkdir(parents=True,exist_ok=True)
 image=Image.open(orig/'reference-scene.png').convert('RGBA');bg=Image.open(orig/'TaskLayers'/f'Area{area:02}Background.png').convert('RGBA') if (orig/'TaskLayers'/f'Area{area:02}Background.png').exists() else Image.open(orig/'TaskLayers/Background.png').convert('RGBA')
 # Native area 2 has a square atlas canvas and the portrait window specified in the source placement.
 crop=(346,27,854,1173) if area==2 and image.width==1200 and image.height==1200 else (0,0,image.width,image.height)
 def ui_save(im,name,maxsize):
  im=im.copy();im.thumbnail(maxsize,Image.Resampling.LANCZOS);im.save(ui/(name+'.png'))
 ui_save(image.crop(crop),'after',(900,1600));ui_save(bg.crop(crop),'before',(900,1600));ui_save(image.crop(crop),'thumb-after',(300,500));ui_save(bg.crop(crop),'thumb-before',(300,500))
 tasks=[]
 for i,t in enumerate(d['tasks']):
  group=t['root'].split('/')[-1];layer=Image.open(orig/'TaskLayers'/(group+'.png')).convert('RGBA').crop(crop);box=layer.getbbox() or (0,0,layer.width,layer.height)
  ui_save(layer.crop(box),f'task-{i+1:02}',(1200,1600))
  icon=PROJECT.parent/f'RoyalMatch-Areas-1-5/Area{area:02}/Sprites/area_{area}_task_{i+1}_icon.png';copy(icon,ui/f'icon-{i+1:02}.png')
  prefix=f'NativeMotion/Area{area:02}/UI/'
  tasks.append(dict(name=t['name'],cost=costs[area-1][i],patch=prefix+f'task-{i+1:02}',icon=prefix+f'icon-{i+1:02}',x0=box[0]/layer.width,y0=box[1]/layer.height,x1=box[2]/layer.width,y1=box[3]/layer.height))
 a.update(name=names[area-1],subtitle=subtitles[area-1],outdoor=area==1,placeholder=False,nativeMotion=True,tasks=tasks)
 for key,fn in [('before','before'),('after','after'),('thumbBefore','thumb-before'),('thumbAfter','thumb-after')]:a[key]=f'NativeMotion/Area{area:02}/UI/{fn}'
save(p,catalog)
