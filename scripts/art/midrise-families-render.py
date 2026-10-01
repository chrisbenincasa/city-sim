"""Renders near/far mid-rise pairs and UV-grid overlays for review.

Run with Blender in the background:
  blender --background art/midrise-families/lineup.blend --python scripts/art/midrise-families-render.py -- OUT_DIR

The selected references cover a solid mansion, a mansion ring, the attic and a panel slab. The
shared renderer gives far facades a checker per bay and storey and overlays those grid lines on near.
"""
import importlib.util
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
_spec = importlib.util.spec_from_file_location('far_review', ROOT / 'scripts/art/tall-families-render.py')
review = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(review)


def main():
    out = Path(sys.argv[sys.argv.index('--') + 1])
    renderer = review.ReviewRenderer(out)
    renderer.pair('shops-wide-bay', 'mansion-solid-near-far.png')
    renderer.pair('perimeter-ring', 'mansion-ring-near-far.png')
    renderer.pair('back-to-back-deep', 'mansion-attic-near-far.png')
    renderer.pair('courtyard-face', 'panel-slab-near-far.png')
    renderer.overlay('shops-wide-bay', 'mansion-overlay.png')
    renderer.overlay('courtyard-face', 'panel-slab-overlay.png')


if __name__ == '__main__':
    main()
