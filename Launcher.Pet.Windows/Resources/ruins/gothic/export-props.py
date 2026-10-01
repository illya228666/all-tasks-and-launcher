"""Crop and chroma-clean imagegen artwork; no procedural asset authoring."""
import importlib.util
import json
import shutil
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
root = Path(__file__).resolve().parent
sources = root.parent.parent / 'sumrak'
processor_file = Path.home()/'.codex/skills/generate2dsprite/scripts/generate2dsprite.py'
spec = importlib.util.spec_from_file_location('processor', processor_file)
processor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(processor)
assets = root / 'assets'
assets.mkdir(parents=True, exist_ok=True)
catalog = []
checks = []
def source(name):
    folder = root / name
    folder.mkdir(exist_ok=True)
    for filename in ('raw-sheet.png','prompt-used.txt'):
        shutil.copyfile(sources / name / filename, folder / filename)
    raw = Image.open(folder / 'raw-sheet.png').convert('RGBA')
    clean = processor.remove_bg_magenta(raw, 100, 180)
    clean.save(folder / 'transparent-sheet.png')
    return clean
def export(image, identity, modular=False):
    box = image.getbbox()
    if not box: raise ValueError(f'Empty art {identity}')
    image = image.crop(box)
    alpha = np.asarray(image.getchannel('A'))
    dense = np.where((alpha > 150).sum(axis=1) > image.width * .80)[0]
    contact = int(dense[0]) if len(dense) else 0
    file = identity.replace(':','-')+'.png'
    image.save(assets / file)
    catalog.append(dict(id=identity, file='assets/'+file, contactY=contact, modular=modular))
    checks.append(dict(id=identity, size=list(image.size), nonempty=True, contactY=contact, magentaPixels=int(((np.asarray(image)[:,:,0]>240)&(np.asarray(image)[:,:,1]<20)&(np.asarray(image)[:,:,2]>240)&(alpha>100)).sum())))
def components(image, number, order):
    selected = processor.connected_components(image, 1000)[:number]
    if len(selected) != number: raise ValueError('Missing complete objects')
    selected.sort(key=lambda c:c['bbox'][order])
    for c in selected:
        x0,y0,x1,y1 = c['bbox']
        binary = Image.fromarray(np.where(np.asarray(image.getchannel('A'))>0,255,0).astype('uint8'))
        # Find a seed belonging to this component on its highest occupied row.
        found = False
        for x in range(x0,x1):
            if binary.getpixel((x,y0)) != 255: continue
            work = binary.copy()
            ImageDraw.floodfill(work,(x,y0),128,thresh=0)
            mask = np.asarray(work)==128
            if int(mask.sum()) == c['area']:
                clean = image.copy()
                clean.putalpha(Image.fromarray(np.where(mask,np.asarray(image.getchannel('A')),0).astype('uint8')))
                found = True
                yield clean.crop((x0,y0,x1,y1))
                break
        if not found: raise ValueError('Could not isolate complete object')
for group,name in enumerate(('platforms-a','platforms-b','platforms-c','platforms-d')):
    for i,image in enumerate(components(source(name),3,1)): export(image,f'platform:{group*3+i}',True)
for i,image in enumerate(components(source('arches'),4,0)): export(image,f'arch:{i}')
for i,image in enumerate(components(source('roots'),3,0)): export(image,f'root:{i}')
for i,image in enumerate(components(source('bridges'),3,1)): export(image,f'bridge:{i}',True)
for name,identities in [('props',[f'prop:{i}' for i in range(8)]),
    ('ecology',['life:germinating','life:growing','life:mature','life:exhausted','life:soil-poor','life:soil-rich','life:spore','life:remains'])]:
    image = source(name)
    for i,identity in enumerate(identities):
        row,col = divmod(i,4)
        export(image.crop((round(col*image.width/4),round(row*image.height/2),round((col+1)*image.width/4),round((row+1)*image.height/2))),identity)
wallpaper = source('wallpaper') if False else Image.open(sources / 'wallpaper' / 'raw-sheet.png')
wallpaper.save(root.parent / 'wallpaper.png')
shutil.copytree(sources / 'wallpaper',root / 'wallpaper',dirs_exist_ok=True)
(root / 'catalog.json').write_text(json.dumps(catalog,indent=2),encoding='utf-8')
(root / 'export-checks.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
print('Exported',len(catalog),'gothic assets')
