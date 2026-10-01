import subprocess
import sys
from pathlib import Path
root = Path(__file__).resolve().parent
processor = Path('C:/Users/illya/.codex/skills/generate2dsprite/scripts/generate2dsprite.py')
jobs = [('walk-right-nohat',16,False,None),('walk-left',16,False,0),('jump',16,False,0),
    ('pickup',12,True,0),('place',12,True,0),('plant',16,False,0),('grab',4,True,0),
    ('pull-up',8,True,7),('carry-right',16,False,0),('carry-left',16,False,0),
    ('recycle',16,False,0),('drag',12,True,None),('failed',16,False,0),
    ('recovery',16,False,15),('hat-pickup',12,True,None),('carry-climb',12,True,None),
    ('plant-nohat',16,False,None),('jump-nohat',16,False,None),('walk-left-nohat',16,False,None)]
for name,count,paired,reference in jobs:
    if (root / name / 'export-meta.json').exists(): continue
    cmd = [sys.executable,str(root / 'process-action.py'),name,'--count',str(count),'--processor',str(processor)]
    if paired: cmd += ['--paired']
    if reference is not None: cmd += ['--reference-frame',str(reference)]
    if name.endswith('-nohat'):
        parent = 'walk-right-correction' if name == 'walk-right-nohat' else name[:-6]
        cmd += ['--same-scale-as',str(root / parent / 'source-grid.json')]
    result = subprocess.run(cmd)
    print(name,'EXIT',result.returncode,flush=True)
