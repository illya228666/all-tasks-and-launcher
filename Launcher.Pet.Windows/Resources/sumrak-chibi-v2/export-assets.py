"""Deterministic sprite cleanup/export only; does not generate art or alter app code.

Run after generate2dsprite processing. Special actions retain source-space motion
or include a small overscan region so the fixed-grid cutter cannot clip a hat.
Every frame in an action uses one scale; no individual bbox-fit resizing.
"""
import argparse
import importlib.util
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent
ACTIONS = {
    'idle': (12, 140, True), 'walk': (16, 80, True),
    'run': (16, 60, True), 'jump': (16, 80, False),
    'sit-down': (12, 100, False), 'seated-idle': (12, 140, True),
    'sleep': (12, 180, True), 'wake-up': (12, 110, False),
    'wave': (12, 100, False), 'surprise': (12, 100, False),
    'hat-trick': (16, 90, False),
}


def despill(image):
    """Neutralize magenta contamination only within two pixels of transparency.

    Brown skin/hair and blue fabric are untouched because both red and blue
    must exceed green. Alpha and geometry are retained, including fine hair.
    """
    pixels = np.array(image.convert('RGBA'))
    alpha = pixels[:, :, 3]
    eroded = np.array(image.getchannel('A').filter(ImageFilter.MinFilter(5)))
    rgb = pixels[:, :, :3].astype(np.int16)
    mask = (alpha > 0) & (eroded < alpha)
    mask &= (np.minimum(rgb[:, :, 0], rgb[:, :, 2]) - rgb[:, :, 1] > 12)
    for channel in (0, 2):
        pixels[:, :, channel][mask] = np.minimum(
            rgb[:, :, channel][mask], rgb[:, :, 1][mask] + 8
        ).astype(np.uint8)
    return Image.fromarray(pixels), int(mask.sum())


def source_motion_frames(action, processor, metadata):
    """Reuse skill chroma/component/anchor primitives, with fixed raw-space scale.

    Wake-up uses 24px vertical overscan for complete hats drawn above nominal
    cell boundaries. Jump and hat-trick keep raw root motion/prop placement.
    """
    raw = Image.open(ROOT / action / 'raw-sheet.png').convert('RGBA')
    cleaned = processor.remove_bg_magenta(raw, 100, 180)
    rows, cols = metadata['rows'], metadata['cols']
    width, height = raw.width // cols, raw.height // rows
    cell = metadata['cell_size']
    scale = min(cell / (width - 8), cell / (height - 8)) * 0.85
    parts = []
    for row in range(rows):
        for col in range(cols):
            x0, y0 = col * width, row * height
            top = max(0, y0 - 24) if action == 'wake-up' else y0
            bottom = min(raw.height, y0 + height + 24) if action == 'wake-up' else y0 + height
            source = cleaned.crop((x0, top, x0 + width, bottom))
            components = processor.connected_components(source, min_area=24)
            if action != 'hat-trick':
                components = sorted(components, key=lambda item: item['area'], reverse=True)[:1]
            selected = Image.new('RGBA', source.size)
            for component in components:
                region = component['bbox']
                selected.paste(source.crop(region), (region[0], region[1]))
            bbox = selected.getbbox()
            if not bbox:
                raise ValueError(f'{action}: empty source frame {row},{col}')
            if bbox[0] == 0 or bbox[1] == 0 or bbox[2] == source.width or bbox[3] == source.height:
                raise ValueError(f'{action}: actual source contour touches edge {row},{col}')
            anchor = processor.estimate_anchor(selected, bbox, 'feet')
            parts.append((selected, bbox, anchor, top - y0))
    first_anchor = parts[0][2]
    frames, records = [], []
    for index, (source, bbox, anchor, top_offset) in enumerate(parts):
        reference_anchor = anchor if action == 'wake-up' else first_anchor
        target = (cell / 2, 335)
        # Overscan changes source coordinate origin, not anatomical scale.
        paste_x = round(target[0] - (reference_anchor[0] - bbox[0]) * scale)
        anchor_y = reference_anchor[1] if action == 'wake-up' else reference_anchor[1] - top_offset
        paste_y = round(target[1] - (anchor_y - bbox[1]) * scale)
        subject = source.crop(bbox)
        subject = subject.resize((round(subject.width * scale), round(subject.height * scale)), Image.Resampling.LANCZOS)
        if paste_x < 1 or paste_y < 1 or paste_x + subject.width >= cell or paste_y + subject.height >= cell:
            raise ValueError(f'{action}: output would clip frame {index + 1}')
        canvas = Image.new('RGBA', (cell, cell))
        canvas.alpha_composite(subject, (paste_x, paste_y))
        frames.append(canvas)
        records.append({'frame': index + 1, 'source_bbox': list(bbox),
                        'source_top_overscan_offset': top_offset,
                        'source_to_output_scale': scale,
                        'paste_position': [paste_x, paste_y]})
    return frames, records


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--processor', required=True, help='Path to generate2dsprite.py from the installed skill')
    parser.add_argument('--action', choices=ACTIONS, help='Re-export only one newly processed action')
    arguments = parser.parse_args()
    spec = importlib.util.spec_from_file_location('sprite_processor', arguments.processor)
    processor = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(processor)
    manifest = {'version': 1, 'character': 'sumrak-chibi-v2', 'actions': []}
    for action, (count, duration, looping) in ACTIONS.items():
        folder = ROOT / action
        metadata = json.loads((folder / 'pipeline-meta.json').read_text(encoding='utf-8'))
        assert len(metadata['frame_labels']) == count
        if arguments.action and action != arguments.action:
            export = json.loads((folder / 'export-meta.json').read_text(encoding='utf-8'))
            manifest['actions'].append({'name': action, **export,
                                       'frames': [f'{action}/{label}.png' for label in metadata['frame_labels']]})
            continue
        records = []
        if action in {'jump', 'hat-trick', 'wake-up'}:
            frames, records = source_motion_frames(action, processor, metadata)
        else:
            frames = [Image.open(folder / f'{label}.png').convert('RGBA') for label in metadata['frame_labels']]
        cleaned_frames, changed_pixels = [], 0
        for frame, label in zip(frames, metadata['frame_labels']):
            frame, changed = despill(frame)
            bbox = frame.getbbox()
            assert bbox and 0 < bbox[0] < bbox[2] < frame.width and 0 < bbox[1] < bbox[3] < frame.height
            changed_pixels += changed
            frame.save(folder / f'{label}.png')
            cleaned_frames.append(frame)
        processor.compose_sheet(cleaned_frames, metadata['rows'], metadata['cols'], metadata['cell_size']).save(folder / 'sheet-transparent.png')
        # GIFs intentionally repeat for review, even for one-shot actions.
        processor.save_transparent_gif(cleaned_frames, folder / 'animation.gif', duration)
        export = {'frame_count': count, 'frame_duration_ms': duration,
                  'loop': looping, 'gif_repeats_for_review': True,
                  'cell_size': metadata['cell_size'], 'origin': [181, 335],
                  'empty_frames': [], 'output_edge_touch_frames': [],
                  'paste_clamped_frames': [], 'despilled_edge_pixels': changed_pixels,
                  'alignment': 'fixed-source-root' if action in {'jump', 'hat-trick'} else 'feet',
                  'source_motion_export': records}
        (folder / 'export-meta.json').write_text(json.dumps(export, indent=2), encoding='utf-8')
        manifest['actions'].append({'name': action, **export,
                                   'frames': [f'{action}/{label}.png' for label in metadata['frame_labels']]})
        print(f'{action}: {count} RGBA frames, no clipping, despilled {changed_pixels} edge pixels')
    # Also clean the single master, without touching its original generation.
    if not arguments.action:
        master, _ = despill(Image.open(ROOT / 'master/single-1.png').convert('RGBA'))
        master.save(ROOT / 'master/single-1.png')
    (ROOT / 'bundle.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
