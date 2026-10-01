"""Assemble accepted imagegen actions into the runtime bundle and review gallery.

Frames are translated to the authored sole line. No frame is resized separately.
Hand landmarks describe the drawn gesture, not ecological state changes.
"""
import importlib.util
import json
from pathlib import Path
import numpy as np
from PIL import Image
root = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('processor',Path.home()/'.codex/skills/generate2dsprite/scripts/generate2dsprite.py')
processor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(processor)
contracts = [
 ('idle',0,12,'idle',None,140,True),('walk-right',1,16,'walk-right-correction','walk-right-nohat',80,True),
 ('walk-left',2,16,'walk-left','walk-left-nohat-fixed',80,True),('wave',3,12,'wave',None,100,False),
 ('jump',4,16,'jump','jump-nohat',80,False),('failed',5,16,'failed','failed-nohat',80,False),
 ('look-upper',9,8,'look-upper',None,140,False),('look-lower',10,8,'look-lower',None,140,False),
 ('climb',11,12,'climb-correct','climb-nohat-correct',100,True),('drag',12,12,'drag-correct','drag-correct-nohat',120,True),
 ('grab',13,4,'grab',None,50,False),('pull-up',14,8,'pull-up-correct','pull-up-correct-nohat',40,False),
 ('fall',15,8,'fall-correct',None,100,False),('recovery',16,16,'recovery','recovery-nohat',80,False),
 ('hat-pickup',17,12,'hat-pickup',None,80,False),('idle-curious',18,12,'idle-curious',None,140,True),
 ('observe',19,12,'observe',None,160,False),('pickup',20,12,'pickup-correct','pickup-correct-nohat',160,False),
 ('carry-right',21,16,'carry-right','carry-right-nohat',80,True),('carry-left',22,16,'carry-left-glove','carry-left-nohat-glove',80,True),
 ('carry-climb',23,12,'carry-climb-correct','carry-climb-correct-nohat',100,True),('place',24,12,'place-correct','place-correct-nohat',160,False),
 ('plant',25,16,'plant','plant-nohat',125,False),('recycle',26,16,'recycle','recycle-nohat',125,False)]
hands = {
 'pickup':[(.7,.57),(.78,.7),(.83,.78),(.83,.91),(.81,.94),(.85,.94),(.85,.94),(.88,.68),(.80,.69),(.80,.6),(.82,.6),(.82,.59)],
 'place':[(.83,.55),(.86,.65),(.87,.79),(.90,.94),(.88,.93),(.88,.93),(.85,.88),(.82,.76),(.82,.67),(.82,.57),(.82,.57),(.82,.57)],
 'plant':[(.87,.58),(.80,.73),(.80,.9),(.73,.94),(.85,.9),(.81,.95),(.78,.96),(.69,.96),(.66,.96),(.66,.96),(.72,.94),(.78,.85),(.77,.7),(.72,.62),(.76,.72),(.79,.76)],
 'recycle':[(.86,.54),(.87,.85),(.83,.92),(.85,.93),(.72,.82),(.73,.82),(.72,.82),(.70,.82),(.69,.81),(.70,.82),(.70,.82),(.80,.74),(.70,.65),(.78,.80),(.76,.76),(.79,.76)]}
commits={'observe':11,'pickup':6,'place':5,'plant':10,'recycle':10}
def point(x,y): return dict(x=round(x),y=round(y))
def geometry(image,name,index):
    box=image.getbbox(); x0,y0,x1,y1=box; w=x1-x0; h=y1-y0
    rgb=np.asarray(image)[:,:,:3].astype('int16'); alpha=np.asarray(image)[:,:,3]
    white=(rgb[:,:,0]>165)&(rgb[:,:,1]>150)&(rgb[:,:,2]>110)&(rgb[:,:,0]>=rgb[:,:,2]*.98)&((rgb.max(axis=2)-rgb.min(axis=2))<95)&(alpha>150)
    face_image=Image.new('RGBA',image.size)
    face_image.putalpha(Image.fromarray(np.where(white,255,0).astype('uint8')))
    components=processor.connected_components(face_image.crop(box),100)
    face=tuple(v+(x0 if i%2==0 else y0) for i,v in enumerate(components[0]['bbox'])) if components else (round(x0+w*.4),round(y0+h*.25),round(x0+w*.75),round(y0+h*.47))
    fx0,fy0,fx1,fy1=face; fw=fx1-fx0; fh=fy1-fy0; cx=(fx0+fx1)/2
    head=dict(x=max(0,round(cx-fw*.95)),y=max(0,round(fy0-fh*.9)),width=round(fw*1.9),height=round(fh*2))
    u,v=hands.get(name,[(.84,.61)]*32)[index]
    if name=='carry-left': u,v=.13,.64
    elif name=='carry-climb': u,v=.75,.40
    elif name in ('climb','grab','pull-up'): u,v=.78,.2
    return dict(bodyAnchorX=384,groundAnchorY=711,headAnchor=point(cx,(fy0+fy1)/2),headBounds=head,
        itemAnchor=point(x0+w*u,y0+h*v),hatAnchor=point(cx,fy0-fh*.4))
actions=[]; checks=[]
for name,row,count,parent,bare,duration,loop in contracts:
    folder=root/name; folder.mkdir(exist_ok=True)
    for source in [parent]+([bare] if bare else []):
        if not (root/source/'pipeline-meta.json').exists(): raise ValueError('Unprocessed action '+source)
    clip=dict(name=name,row=row,loop=loop,durations=[duration]*count,hatFrames=[],noHatFrames=[],geometry=[],noHatGeometry=[],phases={'begin':0,'commit':commits.get(name,count-1),'finish':count-1})
    if name=='hat-pickup': clip['phases'].update(grasp=3,attached=7)
    for variant,source,offset,key,geometry_key in [('hat',parent,0,'hatFrames','geometry'),('no-hat',bare or parent,0 if bare else count,'noHatFrames','noHatGeometry')]:
        output=folder/variant; output.mkdir(exist_ok=True)
        frames=[]
        for index in range(count):
            original=Image.open(root/source/f'{source}-{index+1+offset}.png').convert('RGBA')
            box=original.getbbox()
            if not box: raise ValueError('Empty frame')
            dy=711-box[3]+1
            image=Image.new('RGBA',(768,768)); image.alpha_composite(original,(0,dy))
            finalbox=image.getbbox()
            if finalbox[0]<8 or finalbox[1]<8 or finalbox[2]>760 or finalbox[3]>760: raise ValueError('Clipped output')
            filename=f'{name}-{index+1}.png'; image.save(output/filename)
            clip[key].append(f'{name}/{variant}/{filename}')
            clip[geometry_key].append(geometry(image,name,index))
            frames.append(image)
            checks.append(dict(action=name,variant=variant,frame=index,alphaBox=list(finalbox),soleShift=dy))
        processor.compose_sheet(frames,(count+3)//4,4,768).save(output/'sheet-transparent.png')
        processor.save_transparent_gif(frames,output/'animation.gif',duration)
    (folder/'runtime-meta.json').write_text(json.dumps(clip,indent=2),encoding='utf-8')
    actions.append(clip)
    print(name,'exported',count*2,'frames',flush=True)
standing=Image.open(root/actions[0]['hatFrames'][0]).getbbox()
bundle=dict(version=1,id='sumrak',cellSize=dict(width=768,height=768),standingHeight=standing[3]-standing[1],actions=actions)
(root/'bundle.json').write_text(json.dumps(bundle,indent=2),encoding='utf-8')
(root/'bundle-preview.js').write_text('window.SUMRAK='+json.dumps(bundle)+';',encoding='utf-8')
(root/'export-checks.json').write_text(json.dumps(dict(frameCount=len(checks),frames=checks,source='imagegen',perFrameResize=False),indent=2),encoding='utf-8')
print('Exported',len(actions),'clips;',len(checks),'frames; standing height',bundle['standingHeight'])
