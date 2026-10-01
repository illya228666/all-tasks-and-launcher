import importlib.util
import json
import shutil
from pathlib import Path
from PIL import Image
root = Path(__file__).resolve().parent
sources = root.parent.parent / 'sumrak'
spec=importlib.util.spec_from_file_location('processor',Path.home()/'.codex/skills/generate2dsprite/scripts/generate2dsprite.py')
processor=importlib.util.module_from_spec(spec); spec.loader.exec_module(processor)
catalog=json.loads((root/'catalog.json').read_text())
catalog=[d for d in catalog if not d['id'].startswith('fx:')]
for name in ('release','plant','decompose'):
    source=sources/('fx-plant-fixed' if name == 'plant' else f'fx-{name}')
    target=root/f'fx-{name}'; shutil.copytree(source,target,dirs_exist_ok=True)
    raw=processor.remove_bg_magenta(Image.open(source/'raw-sheet.png').convert('RGBA'),100,180)
    raw.save(target/'transparent-sheet.png')
    frames=[]
    scale=512/max(raw.width/4,raw.height/2)
    for index in range(8):
        row,col=divmod(index,4)
        cell=raw.crop((round(col*raw.width/4),round(row*raw.height/2),round((col+1)*raw.width/4),round((row+1)*raw.height/2)))
        box=cell.getbbox()
        if not box: raise ValueError('Empty effect')
        image=cell.crop(box).resize((round((box[2]-box[0])*scale),round((box[3]-box[1])*scale)),Image.Resampling.LANCZOS)
        frame=Image.new('RGBA',(512,512));frame.alpha_composite(image,((512-image.width)//2,480-image.height))
        filename=f'fx-{name}-{index}.png';frame.save(root/'assets'/filename)
        catalog.append(dict(id=f'fx:{name}:{index}',file='assets/'+filename,contactY=480,modular=False))
        frames.append(frame)
    processor.compose_sheet(frames,2,4,512).save(target/'sheet-transparent.png')
    processor.save_transparent_gif(frames,target/'animation.gif',160)
    (target/'export-meta.json').write_text(json.dumps(dict(frameCount=8,sharedScale=scale,source='imagegen',baseline=480),indent=2))
(root/'catalog.json').write_text(json.dumps(catalog,indent=2),encoding='utf-8')
hat_source=sources/'hat';hat_raw=processor.remove_bg_magenta(Image.open(hat_source/'raw-sheet.png').convert('RGBA'),100,180)
hat_folder=hat_source/'hat';hat_folder.mkdir(exist_ok=True)
for index in range(8):
    row,col=divmod(index,4)
    cell=hat_raw.crop((round(col*hat_raw.width/4),round(row*hat_raw.height/2),round((col+1)*hat_raw.width/4),round((row+1)*hat_raw.height/2)))
    cell.crop(cell.getbbox()).save(hat_folder/f'hat-{index+1}.png')
hat_raw.save(hat_source/'sheet-transparent.png')
print('Exported 24 effect frames and 8 hat poses')
