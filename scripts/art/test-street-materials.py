#!/usr/bin/env python3
"""Fetch the test street's CC0 surface textures and write art/materials/test-street/.

Each surface keeps its albedo, OpenGL normal and roughness at 1024 px, plus its real-world tile size.
A painted surface's albedo becomes greyscale with a fixed linear mean luminance, so a Building's
paint colour divided by that mean is the material's colour factor. Needs Pillow.
"""
import hashlib
import io
import json
import urllib.request
import zipfile
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art/materials/test-street'
AGENT = {'User-Agent': 'borough-test-street-materials'}
PAINT_MEAN = .7
POLY_HAVEN = 'https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/{0}/{0}_{1}_1k.jpg'

SURFACES = {
    'siding': {
        'asset': 'Wood Siding 009', 'publisher': 'ambientCG', 'url': 'https://ambientcg.com/a/WoodSiding009',
        'zip': 'https://ambientcg.com/get?file=WoodSiding009_1K-JPG.zip',
        'maps': {'albedo': 'WoodSiding009_1K-JPG_Color.jpg', 'normal': 'WoodSiding009_1K-JPG_NormalGL.jpg',
                 'roughness': 'WoodSiding009_1K-JPG_Roughness.jpg'},
        'metres': [2.4, 1.2], 'paint': True,
        'measured': '8 boards over the image height; a 150 mm lap exposure makes the 2:1 image 2.4 x 1.2 m',
    },
    'render': {
        'asset': 'Painted Plaster Wall', 'publisher': 'Poly Haven', 'url': 'https://polyhaven.com/a/painted_plaster_wall',
        'id': 'painted_plaster_wall', 'metres': [2.0, 2.0], 'paint': True,
        'measured': 'No module to count; Poly Haven states 2 x 2 m',
    },
    'block': {
        'asset': 'Concrete Block Wall', 'publisher': 'Poly Haven', 'url': 'https://polyhaven.com/a/concrete_block_wall',
        'id': 'concrete_block_wall', 'metres': [2.0, 1.6], 'paint': True,
        'measured': '8 courses over the image height and 5 blocks across; 200 mm courses and 400 mm blocks give 2.0 x 1.6 m. Poly Haven states 2 x 2 m',
    },
    'sheet': {
        'asset': 'Box Profile Metal Sheet', 'publisher': 'Poly Haven', 'url': 'https://polyhaven.com/a/box_profile_metal_sheet',
        'id': 'box_profile_metal_sheet', 'metres': [2.0, 2.0], 'paint': True,
        'measured': '10 trapezoidal ribs over the image width, a 200 mm pitch at the stated 2 x 2 m',
    },
    'membrane': {
        'asset': 'Bitumen', 'publisher': 'Poly Haven', 'url': 'https://polyhaven.com/a/bitumen',
        'id': 'bitumen', 'metres': [20.0, 20.0], 'paint': False,
        'measured': 'About 21 roll laps over the image height, roughly 1 m rolls at the stated 20 x 20 m',
    },
}


def fetch(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=AGENT), timeout=120) as response:
        return response.read()


def linear(value):
    value /= 255
    return value / 12.92 if value <= .04045 else ((value + .055) / 1.055) ** 2.4


def srgb(value):
    value = min(max(value, 0.0), 1.0)
    return round(255 * (value * 12.92 if value <= .0031308 else 1.055 * value ** (1 / 2.4) - .055))


def paintable(image):
    """Greyscale with a linear mean luminance of PAINT_MEAN."""
    grey = image.convert('L')
    to_linear = [linear(v) for v in range(256)]
    histogram = grey.histogram()
    mean = sum(to_linear[v] * n for v, n in enumerate(histogram)) / sum(histogram)
    gain = PAINT_MEAN / mean
    return grey.point([srgb(to_linear[v] * gain) for v in range(256)]).convert('RGB'), mean


def sha256(data):
    return hashlib.sha256(data).hexdigest()


OUT.mkdir(parents=True, exist_ok=True)
manifest = {
    'what_this_is': 'Surface textures for the procedural-buildings test street, embedded by scripts/art/test-street.py.',
    'authority': 'scripts/art/test-street-materials.py',
    'paint_mean_linear_luminance': PAINT_MEAN,
    'surfaces': {},
}
for name, spec in SURFACES.items():
    if 'zip' in spec:
        archive = fetch(spec['zip'])
        with zipfile.ZipFile(io.BytesIO(archive)) as bundle:
            sources = {key: (f"{spec['zip']}#{member}", bundle.read(member)) for key, member in spec['maps'].items()}
    else:
        sources = {key: (url, fetch(url)) for key, url in
                   (('albedo', POLY_HAVEN.format(spec['id'], 'diff')), ('normal', POLY_HAVEN.format(spec['id'], 'nor_gl')),
                    ('roughness', POLY_HAVEN.format(spec['id'], 'rough')))}
    files = {}
    for key, (url, data) in sources.items():
        image = Image.open(io.BytesIO(data)).convert('RGB' if key != 'roughness' else 'L')
        note = None
        if key == 'albedo' and spec['paint']:
            image, mean = paintable(image)
            note = f'greyscale, linear mean luminance {mean:.4f} scaled to {PAINT_MEAN}'
        path = OUT / f'{name}-{key}.jpg'
        image.save(path, quality=90)
        files[key] = {'file': str(path.relative_to(ROOT)), 'from': url, 'source_sha256': sha256(data),
                      'sha256': sha256(path.read_bytes()), **({'transform': note} if note else {})}
    manifest['surfaces'][name] = {
        'asset': spec['asset'], 'publisher': spec['publisher'], 'url': spec['url'], 'license': 'CC0-1.0',
        'tile_metres': spec['metres'], 'measured': spec['measured'], 'paint': spec['paint'], 'files': files,
    }
    print('SURFACE', name, spec['metres'])
(OUT / 'materials.json').write_text(json.dumps(manifest, indent=2) + '\n')
