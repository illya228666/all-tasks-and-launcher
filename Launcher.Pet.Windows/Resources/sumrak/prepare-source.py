"""Deterministic cut/alignment only; all artwork comes from imagegen.

Generated rows may have unequal empty margins. Find those margins before cutting;
never fit or resize individual bodies. Every frame uses the same source-column scale.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

p = argparse.ArgumentParser()
p.add_argument('input', type=Path)
p.add_argument('--rows', type=int, required=True)
p.add_argument('--cols', type=int, default=4)
p.add_argument('--processor', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
p.add_argument('--reference-frame', type=int)
p.add_argument('--same-scale-as', type=Path)
a = p.parse_args()
spec = importlib.util.spec_from_file_location('sprite_processor', a.processor)
processor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(processor)
raw = Image.open(a.input).convert('RGBA')
clean = processor.remove_bg_magenta(raw, 100, 180)
alpha = np.asarray(clean.getchannel('A'))
occupied = (alpha > 100).sum(axis=1) > max(3, raw.width // 200)
runs = []
for gap in (8, 4, 2):
    runs = []
    for y in range(len(occupied)):
        if occupied[y] and (not runs or y > runs[-1][1] + gap):
            runs.append([y, y])
        elif occupied[y]:
            runs[-1][1] = y
    if len(runs) == a.rows:
        break
column_runs = []
isolated = {}
def isolate(image, component):
    box = component['bbox']
    binary = Image.fromarray(np.where(np.asarray(image.getchannel('A'))>0,255,0).astype('uint8'))
    for x in range(box[0],box[2]):
        if binary.getpixel((x,box[1])) != 255: continue
        work = binary.copy()
        ImageDraw.floodfill(work,(x,box[1]),128,thresh=0)
        mask = np.asarray(work)==128
        if int(mask.sum()) == component['area']:
            result = image.copy()
            result.putalpha(Image.fromarray(np.where(mask,np.asarray(image.getchannel('A')),0).astype('uint8')))
            return result.crop(box)
    raise ValueError('Unable to isolate body')
for col in range(a.cols):
    left, right = round(col * raw.width / a.cols), round((col + 1) * raw.width / a.cols)
    if len(runs) == a.rows:
        column_runs.append(runs)
        continue
    occupied_column = (alpha[:,left:right] > 100).sum(axis=1) > 2
    separated = []
    for gap in (8,4,2,1):
        separated = []
        for y in range(len(occupied_column)):
            if occupied_column[y] and (not separated or y > separated[-1][1] + gap): separated.append([y,y])
            elif occupied_column[y]: separated[-1][1] = y
        if len(separated) == a.rows: break
    if len(separated) != a.rows:
        column = clean.crop((left,0,right,raw.height))
        bodies = processor.connected_components(column,1000)[:a.rows]
        if len(bodies) != a.rows: raise ValueError(f'Missing bodies in column {col}')
        bodies.sort(key=lambda b:b['bbox'][1])
        separated = []
        for row,body in enumerate(bodies):
            if body['touches_edge']: raise ValueError(f'Clipped body {row},{col}; regenerate')
            isolated[(row,col)] = isolate(column,body)
            separated.append([body['bbox'][1],body['bbox'][3]-1])
    column_runs.append(separated)
sheet = Image.new('RGBA', (a.cols * 512, a.rows * 512), (255, 0, 255, 255))
common_scale = 384 / min(raw.width / a.cols, raw.height / a.rows)
if a.same_scale_as:
    contract = json.loads(a.same_scale_as.read_text())
    source_width = contract.get('sourceSize', Image.open(contract['raw']).size)[0]
    common_scale = contract['cuts'][0]['commonScale'] * source_width / raw.width
elif a.reference_frame is not None:
    ref_row, ref_col = divmod(a.reference_frame, a.cols)
    ref_top, ref_bottom = column_runs[ref_col][ref_row]
    ref_left, ref_right = round(ref_col * raw.width / a.cols), round((ref_col + 1) * raw.width / a.cols)
    reference = isolated.get((ref_row,ref_col),clean.crop((ref_left,ref_top,ref_right,ref_bottom+1)))
    reference_box = reference.getbbox()
    # One source-resolution conversion for the entire sheet, never a pose-specific fit.
    # The explicit reference is a standing pose; compression in all other poses remains real.
    common_scale = 345 / (reference_box[3] - reference_box[1])
cuts = []
for row in range(a.rows):
    for col in range(a.cols):
        top, bottom = column_runs[col][row]
        left, right = round(col * raw.width / a.cols), round((col + 1) * raw.width / a.cols)
        frame = clean.crop((left, top, right, bottom + 1))
        if (row,col) in isolated: frame = isolated[(row,col)]
        else:
            body = processor.connected_components(frame,1000)[0]
            frame = isolate(frame,body)
        box = frame.getbbox()
        if not box:
            raise ValueError(f'Empty frame {row},{col}')
        frame = frame.crop(box)
        size = tuple(round(v * common_scale) for v in frame.size)
        if max(size) > 420:
            raise ValueError(f'Clipped or oversized frame {row},{col}: {size}; regenerate.')
        frame = frame.resize(size, Image.Resampling.LANCZOS)
        destination = (col * 512 + (512 - size[0]) // 2, row * 512 + 450 - size[1])
        sheet.alpha_composite(frame, destination)
        cuts.append({'source': [left + box[0], top + box[1], left + box[2], top + box[3]],
                     'commonScale': common_scale, 'destination': list(destination)})
sheet.convert('RGB').save(a.output)
a.output.with_suffix('.json').write_text(json.dumps({'raw': str(a.input), 'rows': a.rows,
    'cols': a.cols, 'sourceSize': list(raw.size), 'cell': 512, 'referenceFrame': a.reference_frame,
    'sameScaleAs': str(a.same_scale_as) if a.same_scale_as else None, 'cuts': cuts}, indent=2), encoding='utf-8')
