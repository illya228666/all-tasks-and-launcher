"""Reproducible imagegen-source processing, never creative image generation."""
import argparse
import json
import subprocess
import sys
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('name')
p.add_argument('--count', type=int, required=True)
p.add_argument('--paired', action='store_true')
p.add_argument('--processor', type=Path, required=True)
p.add_argument('--standing', action='store_true')
p.add_argument('--duration', type=int, default=100)
p.add_argument('--reference-frame', type=int)
p.add_argument('--same-scale-as', type=Path)
p.add_argument('--cols', type=int, default=4)
a = p.parse_args()
root = Path(__file__).resolve().parent
action = root / a.name
total = a.count * (2 if a.paired else 1)
cols = a.cols
rows = total // cols
prepare = [sys.executable, str(root / 'prepare-source.py'), str(action / 'raw-sheet.png'),
    '--rows', str(rows), '--cols', str(cols), '--processor', str(a.processor), '--output', str(action / 'source-grid.png')]
if a.reference_frame is not None: prepare += ['--reference-frame', str(a.reference_frame)]
if a.same_scale_as: prepare += ['--same-scale-as', str(a.same_scale_as)]
subprocess.run(prepare, check=True)
cmd = [sys.executable, str(a.processor), 'process', '--input', str(action / 'source-grid.png'),
    '--target', 'player', '--mode', a.name, '--rows', str(rows), '--cols', str(cols),
    '--scale-profile', str(root / 'character-scale-profile.json'), '--threshold', '100', '--edge-threshold', '180',
    '--duration', str(a.duration), '--strict-qc', '--output-dir', str(action)]
if a.standing:
    cmd += ['--max-body-scale-cv', '.08', '--max-anchor-y-std', '.05', '--max-profile-scale-drift', '.08']
else:
    # Outline area changes during crouching, falling or a large hand gesture.
    # Uniform profile scaling remains locked; anatomy needs visual inspection.
    cmd += ['--max-profile-scale-drift', '1']
subprocess.run(cmd, check=True)
metadata = json.loads((action / 'pipeline-meta.json').read_text())
(action / 'export-meta.json').write_text(json.dumps({'frameCount': a.count, 'paired': a.paired,
    'strictQcPassed': True, 'sharedScaleProfile': '../character-scale-profile.json',
    'standingOutlineGate': a.standing, 'qc': metadata['qc_summary']}, indent=2), encoding='utf-8')
print(a.name, 'processed:', metadata['qc_summary'])
