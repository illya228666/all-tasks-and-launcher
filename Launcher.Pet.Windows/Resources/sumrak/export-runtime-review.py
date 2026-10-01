"""Package actual GDI renderer captures; the frames come from PetWorld updates."""
import json
import shutil
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parent
repository=root.parents[2]
source=repository/'artifacts'/'sumrak-graphics'
target=root/'runtime-review';target.mkdir(exist_ok=True)
for name in ('observe','carry','plant','recycle'):
    frames=[Image.open(path).convert('RGB') for path in sorted((source/f'motion-{name}').glob('frame-*.png'))]
    if len(frames)!=27:raise ValueError('Missing runtime sequence '+name)
    frames[0].save(target/f'{name}.gif',save_all=True,append_images=frames[1:],duration=100,loop=0,disposal=2)
    shutil.copyfile(source/f'motion-{name}'/'history.json',target/f'{name}-history.json')
for name in ('life-000.png','life-120.png','life-300.png','life-420.png','activities.png'):
    shutil.copyfile(source/name,target/name)
(target/'review.json').write_text(json.dumps(dict(source='PetWorld + production RuinRenderer/PetImages/LifeDrawing',stepMs=20,captureMs=100,sequences=4,framesPerSequence=27),indent=2))
print('Packaged four actual motion sequences and ecological renderer captures.')
