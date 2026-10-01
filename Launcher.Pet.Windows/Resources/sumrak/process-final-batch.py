import subprocess
import sys
from pathlib import Path
root = Path(__file__).resolve().parent
processor = Path('C:/Users/illya/.codex/skills/generate2dsprite/scripts/generate2dsprite.py')
jobs = [('carry-right-nohat',16,'carry-right'),('carry-left-nohat',16,'carry-left'),
    ('recycle-nohat',16,'recycle'),('failed-nohat',16,'failed'),('recovery-nohat',16,'recovery'),
    ('climb-correct',12,None),('climb-nohat-correct',12,'climb-correct'),('fall-correct',8,None),
    ('drag-correct',12,None),('carry-climb-correct',12,None),('pickup-correct',12,None),('place-correct',12,None),
    ('pull-up-correct',8,None),('drag-correct-nohat',12,'drag-correct'),('carry-climb-correct-nohat',12,'carry-climb-correct'),
    ('pickup-correct-nohat',12,'pickup-correct'),('place-correct-nohat',12,'place-correct')]
for name,count,parent in jobs:
    if (root / name / 'export-meta.json').exists(): continue
    cmd = [sys.executable,str(root / 'process-action.py'),name,'--count',str(count),'--processor',str(processor)]
    if name == 'fall-correct': cmd += ['--paired']
    if parent: cmd += ['--same-scale-as',str(root / parent / 'source-grid.json')]
    else: cmd += ['--reference-frame','7' if name=='pull-up-correct' else '0']
    result = subprocess.run(cmd)
    print(name,'EXIT',result.returncode,flush=True)
