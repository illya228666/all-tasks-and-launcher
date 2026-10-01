"""Record accepted artistic inputs alongside each runtime action. Preserve rejected inputs."""
import json
import shutil
import ast
from pathlib import Path
root=Path(__file__).resolve().parent
bundle=json.loads((root/'bundle.json').read_text())
tree=ast.parse((root/'export-bundle.py').read_text())
contracts=next(ast.literal_eval(node.value) for node in tree.body if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='contracts' for t in node.targets))
for name,row,count,parent,bare,duration,loop in contracts:
    folder=root/name
    for variant,source in [('hat',parent),('no-hat',bare or parent)]:
        target=folder/'source'/variant;target.mkdir(parents=True,exist_ok=True)
        for filename in ('raw-sheet.png','prompt-used.txt','source-grid.json','pipeline-meta.json','export-meta.json'):
            path=root/source/filename
            if path.exists():shutil.copyfile(path,target/filename)
    (folder/'source'/'selection.json').write_text(json.dumps(dict(hatSource=parent,noHatSource=bare or parent,paired=bare is None),indent=2),encoding='utf-8')
gothic=root.parent/'ruins'/'gothic'
(gothic/'catalog-preview.js').write_text('window.RUIN_ART='+(gothic/'catalog.json').read_text()+';',encoding='utf-8')
print('Recorded accepted sources for',len(bundle['actions']),'actions and offline gothic preview.')
