#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = ["numpy==2.5.3", "Pillow==12.3.0"]
# ///
"""Build the Building texture library in src/Borough.Godot/assets/city/library/ from its textures.toml.

Each texture keeps its 1K colour, OpenGL normal and roughness maps. The colour map is flattened and,
for a painted texture, made greyscale at PAINT_MEAN. Every map is re-encoded at JPEG quality 90,
and roughness as greyscale. Downloads are cached under ~/.cache/borough-textures/sources.

    uv run scripts/art/materials.py [NAME ...]
"""
import hashlib
import io
import json
import sys
import tomllib
import urllib.request
import zipfile
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'src/Borough.Godot/assets/city/library'
CACHE = Path.home() / '.cache/borough-textures/sources'
AGENT = {'User-Agent': 'borough-materials'}
PAINT_MEAN = .7
POLY_HAVEN_MAPS = {'albedo': 'Diffuse', 'normal': 'nor_gl', 'roughness': 'Rough'}
AMBIENT_CG_MAPS = {'albedo': 'Color', 'normal': 'NormalGL', 'roughness': 'Roughness'}


def fetch(url):
    path = CACHE / hashlib.sha256(url.encode()).hexdigest()
    if not path.exists():
        with urllib.request.urlopen(urllib.request.Request(url, headers=AGENT), timeout=120) as response:
            data = response.read()
        CACHE.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
    return path.read_bytes()


def sources(texture):
    """Maps each of albedo, normal and roughness to (url, bytes)."""
    if texture['publisher'] == 'Poly Haven':
        files = json.loads(fetch(f"https://api.polyhaven.com/files/{texture['id']}"))
        urls = {key: files[name]['1k']['jpg']['url'] for key, name in POLY_HAVEN_MAPS.items()}
        return {key: (url, fetch(url)) for key, url in urls.items()}
    url = f"https://ambientcg.com/get?file={texture['id']}_1K-JPG.zip"
    with zipfile.ZipFile(io.BytesIO(fetch(url))) as bundle:
        members = {key: f"{texture['id']}_1K-JPG_{suffix}.jpg" for key, suffix in AMBIENT_CG_MAPS.items()}
        present = set(bundle.namelist())
        return {key: (f'{url}#{member}', bundle.read(member)) for key, member in members.items()
                if key != 'roughness' or member in present}


def to_linear(srgb):
    return np.where(srgb <= .04045, srgb / 12.92, ((srgb + .055) / 1.055) ** 2.4)


def to_srgb(linear):
    linear = np.clip(linear, 0, 1)
    return np.where(linear <= .0031308, linear * 12.92, 1.055 * linear ** (1 / 2.4) - .055)


def flatten(linear, cycles):
    """Divides out luminance variation below `cycles` cycles per tile.

    Works on log luminance with a Gaussian low-pass in the frequency domain, so the tile still wraps
    and the geometric mean is kept.
    """
    log = np.log(linear @ [.2126, .7152, .0722] + 1e-3)
    height, width = log.shape
    radius = np.hypot(np.fft.fftfreq(height)[:, None] * height, np.fft.rfftfreq(width)[None, :] * width)
    low = np.exp(-(radius / cycles) ** 2)
    low[0, 0] = 0
    drift = np.fft.irfft2(np.fft.rfft2(log) * low, s=log.shape)
    return linear * np.exp(-drift)[..., None]


def albedo(data, texture):
    linear = to_linear(np.asarray(Image.open(io.BytesIO(data)).convert('RGB'), np.float64) / 255)
    linear = flatten(linear, texture['detile_cycles'])
    if texture['paint']:
        grey = linear @ [.2126, .7152, .0722]
        linear = np.repeat((grey * PAINT_MEAN / grey.mean())[..., None], 3, axis=2)
    linear = np.clip(linear, 0, 1)
    mean = float((linear @ [.2126, .7152, .0722]).mean())
    return Image.fromarray((to_srgb(linear) * 255 + .5).astype(np.uint8)), mean


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def build(texture):
    files = {}
    mean = None
    for key, (url, data) in sources(texture).items():
        path = OUT / f"{texture['name']}-{key}.jpg"
        entry = {'file': str(path.relative_to(ROOT)), 'from': url, 'source_sha256': sha256(data)}
        if key == 'albedo':
            image, mean = albedo(data, texture)
            image.save(path, quality=90)
            entry['transform'] = (f"flattened below {texture['detile_cycles']} cycles per tile"
                                  + (f', greyscale at linear mean luminance {PAINT_MEAN}' if texture['paint'] else ''))
        else:
            Image.open(io.BytesIO(data)).convert('RGB' if key == 'normal' else 'L').save(path, quality=90)
        entry['sha256'] = sha256(path.read_bytes())
        files[key] = entry
    return {
        'asset': texture['asset'], 'publisher': texture['publisher'], 'id': texture['id'], 'license': 'CC0-1.0',
        'slots': texture['slots'], 'tile_metres': texture['tile_metres'], 'measured': texture['measured'],
        'paint': texture['paint'], 'detile_cycles': texture['detile_cycles'],
        'albedo_linear_mean_luminance': round(mean, 4), 'files': files,
    }


def main(names):
    textures = tomllib.loads((OUT / 'textures.toml').read_text())['texture']
    unknown = set(names) - {t['name'] for t in textures}
    if unknown:
        sys.exit(f"not in textures.toml: {', '.join(sorted(unknown))}")
    manifest_path = OUT / 'materials.json'
    manifest = json.loads(manifest_path.read_text()) if manifest_path.exists() else {}
    built = manifest.get('textures', {})
    listed = {t['name'] for t in textures}
    built = {name: entry for name, entry in built.items() if name in listed}
    for stale in OUT.glob('*.jpg'):
        if stale.stem.rsplit('-', 1)[0] not in listed:
            stale.unlink()
    for texture in textures:
        if names and texture['name'] not in names:
            continue
        built[texture['name']] = build(texture)
        print('TEXTURE', texture['name'], texture['tile_metres'], built[texture['name']]['albedo_linear_mean_luminance'])
    manifest = {
        'what_this_is': 'The CC0 texture library for Building bodies.',
        'authority': 'scripts/art/materials.py',
        'paint_mean_linear_luminance': PAINT_MEAN,
        'textures': {name: built[name] for name in sorted(built)},
    }
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n')


if __name__ == '__main__':
    main(sys.argv[1:])
