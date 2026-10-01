"""Arrange authored runtime frames for visual QA; never generate character artwork."""
import json
from pathlib import Path
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parent
bundle=json.loads((root/'bundle.json').read_text())
cell=200
for start in range(0,len(bundle['actions']),8):
    sheet=Image.new('RGB',(cell*6,8*(cell+24)),(22,27,43));draw=ImageDraw.Draw(sheet)
    for row,clip in enumerate(bundle['actions'][start:start+8]):
        count=len(clip['durations'])
        for variant,key in enumerate(('hatFrames','noHatFrames')):
            for column,index in enumerate((0,count//2,count-1)):
                image=Image.open(root/clip[key][index]).convert('RGBA').resize((cell,cell),Image.Resampling.LANCZOS)
                x=(variant*3+column)*cell;y=row*(cell+24)
                background=Image.new('RGB',(cell,cell),(239,234,225) if variant==0 else (22,27,43))
                background.paste(image,mask=image.getchannel('A'));sheet.paste(background,(x,y))
                draw.text((x+8,y+cell+3),f"{clip['name']} {index+1}",fill=(207,223,237))
    folder=root/'runtime-review';folder.mkdir(exist_ok=True)
    sheet.save(folder/f'overview-{start//8+1}.jpg',quality=90)
print('Three overviews: first, middle and last authored pose of both hat variants.')
