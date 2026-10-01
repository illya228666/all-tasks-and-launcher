import json
import shutil
from pathlib import Path
root = Path(__file__).resolve().parent
for registry in sorted(root.glob('production-jobs*.json')):
    for job in json.loads(registry.read_text(encoding='utf-8-sig')):
        folder = root / job['name']
        folder.mkdir(exist_ok=True)
        shutil.copyfile(job.get('path') or job.get('output'), folder / 'raw-sheet.png')
        (folder / 'prompt-used.txt').write_text(job['prompt'] or '', encoding='utf-8')
