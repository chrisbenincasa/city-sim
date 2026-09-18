#!/usr/bin/env python3
"""Deterministic permission-storage sizing model; not Core code or a timing benchmark.

Run from any directory. Prints JSON; no repository files are written.
Rectangles are half-open (x, y, width, height, packed permission).
The dense grid is an independent semantic oracle and a bounded normalisation scratch page.
"""
import json
import random

SIDE = 32
TILES = SIDE * SIDE
# Proposed global bounds (4 x int32), packed permission (uint64), Rows metadata
# (uint64 id + uint32 generation + int32 free link), two derived int32 indices.
RECORD_BYTES = 16 + 8 + 16 + 8
PERMISSION_BYTES = 8
LIMIT = 1 << 20


def encode(grid):
    """Maximal horizontal equal-value runs; extend identical runs vertically."""
    result, active = [], {}
    for y in range(SIDE):
        runs = []
        x = 0
        while x < SIDE:
            value = grid[y * SIDE + x]
            end = x + 1
            while end < SIDE and grid[y * SIDE + end] == value:
                end += 1
            if value != 0:
                runs.append((x, end - x, value))
            x = end
        following = {}
        for x, width, value in runs:
            key = (x, width, value)
            if key in active:
                index = active[key]
                result[index][3] += 1
            else:
                index = len(result)
                result.append([x, y, width, 1, value])
            following[key] = index
        active = following
    return result


def decode(rectangles):
    grid = [0] * TILES
    for x, y, width, height, value in rectangles:
        assert value and width > 0 and height > 0
        for row in range(y, y + height):
            for col in range(x, x + width):
                index = row * SIDE + col
                assert grid[index] == 0, 'overlapping permission rectangles'
                grid[index] = value
    return grid


def split_paint(rectangles, x, y, width, height, value):
    """Alternative: subtract paint bounds from old rectangles, no coalescing."""
    result = []
    right, top = x + width, y + height
    for a, b, w, h, old in rectangles:
        left, bottom = max(a, x), max(b, y)
        r, t = min(a + w, right), min(b + h, top)
        if left >= r or bottom >= t:
            result.append([a, b, w, h, old])
            continue
        for piece in ([a, b, w, bottom - b, old],
                      [a, t, w, b + h - t, old],
                      [a, bottom, left - a, t - bottom, old],
                      [r, bottom, a + w - r, t - bottom, old]):
            if piece[2] > 0 and piece[3] > 0:
                result.append(piece)
    if value:
        result.append([x, y, width, height, value])
    return result


def run(name, operations):
    grid, split = [0] * TILES, []
    peak, split_peak, capacity, noops = 0, 0, 8, 0
    previous = []
    for x, y, width, height, value in operations:
        before = grid[:]
        for row in range(y, y + height):
            grid[row * SIDE + x:row * SIDE + x + width] = [value] * width
        rectangles = encode(grid)
        assert decode(rectangles) == grid
        assert encode(decode(rectangles)) == rectangles
        if grid == before:
            noops += 1
            assert rectangles == previous
        previous = rectangles
        split = split_paint(split, x, y, width, height, value)
        assert decode(split) == grid
        peak = max(peak, len(rectangles))
        split_peak = max(split_peak, len(split))
        # Model row reuse: retire replaced rows before assigning the final rows;
        # candidate scratch is separate, not extra persistent table allocations.
        while capacity < len(rectangles):
            capacity *= 2
    return dict(name=name, operations=len(operations), noops=noops,
                canonical_final=len(previous), canonical_peak=peak,
                split_only_final=len(split), split_only_peak=split_peak,
                allocated_rows=capacity, allocated_record_bytes=capacity * RECORD_BYTES)


whole = (0, 0, SIDE, SIDE, 1)
clear = (0, 0, SIDE, SIDE, 0)
parcels = [(x, 0, 8, SIDE, 1 + x // 8 % 2) for x in range(0, SIDE, 8)]
checker = [(x, y, 1, 1, 1 + (x + y) % 2)
           for y in range(SIDE) for x in range(SIDE)]
rng = random.Random(620018)  # Evidence generator only; forbidden in simulation.
random_paints = []
for _ in range(2000):
    x, y = rng.randrange(SIDE), rng.randrange(SIDE)
    random_paints.append((x, y, rng.randrange(1, SIDE - x + 1),
                          rng.randrange(1, SIDE - y + 1), rng.randrange(4)))

workloads = [
    ('uniform_1000_repeats', [whole] * 1000),
    ('four_parcels', parcels),
    ('one_tile_stripes', [(x, 0, 1, SIDE, 1 + x % 2) for x in range(SIDE)]),
    ('checkerboard', checker),
    ('random_rectangles_seed_620018', random_paints),
    ('same_colour_cuts', [whole] + [(x, y, 1, 1, 1) for y in range(SIDE) for x in range(SIDE)]),
    ('parcel_clear_cycles_10', (parcels + [clear]) * 10),
    ('parcel_clear_cycles_1000', (parcels + [clear]) * 1000),
    ('fragment_restore_cycles_10', (checker + [whole]) * 10),
]
results = [run(name, operations) for name, operations in workloads]
by_name = {item['name']: item for item in results}
assert by_name['parcel_clear_cycles_10']['allocated_rows'] == by_name['parcel_clear_cycles_1000']['allocated_rows']
assert by_name['fragment_restore_cycles_10']['allocated_rows'] == 1024
assert by_name['same_colour_cuts']['canonical_final'] == 1

# Model all-or-nothing preflight, deliberately with a tiny limit for boundary checks.
def transaction(pages, updates, cap):
    proposed = dict(pages)
    proposed.update(updates)
    required = sum(len(encode(grid)) for grid in proposed.values())
    if required > cap:
        return pages, False
    return proposed, True

uniform_grid = [1] * TILES
striped_grid = [1 + x % 2 for y in range(SIDE) for x in range(SIDE)]
pages = {0: uniform_grid}
unchanged, accepted = transaction(pages, {0: striped_grid, 1: uniform_grid}, 32)
assert not accepted and unchanged is pages and pages == {0: uniform_grid}
changed, accepted = transaction(pages, {0: striped_grid}, 32)
assert accepted and len(encode(changed[0])) == 32
compacted, accepted = transaction(changed, {0: uniform_grid}, 32)
assert accepted and len(encode(compacted[0])) == 1

print(json.dumps(dict(
    conditions=dict(page_tiles=SIDE, world_tiles=16384, packed_permission_bytes=PERMISSION_BYTES,
                    proposed_record_bytes=RECORD_BYTES, proposed_record_limit=LIMIT,
                    timing_claim=False, core_implementation=False),
    workloads=results,
    estimates=dict(
        full_world_dense_payload_bytes=16384 ** 2 * PERMISSION_BYTES,
        full_world_rectangle_record_bytes=16384 ** 2 * RECORD_BYTES,
        sparse_page_directory_bytes=512 ** 2 * 4,
        one_dense_page_payload_bytes=TILES * PERMISSION_BYTES,
        proposed_record_capacity_bytes=LIMIT * RECORD_BYTES,
        one_page_decode_scratch_bytes=TILES * PERMISSION_BYTES,
        one_page_max_rect_scratch_bytes=TILES * 24,
        projected_cells_10000_four_parcels_records=10000 * by_name['four_parcels']['canonical_final'],
        projected_cells_10000_four_parcels_allocated_bytes=65536 * RECORD_BYTES),
    checks=['non-overlap and exact paint roundtrip after every operation',
            'canonical encoding is idempotent', 'no-op paint keeps canonical records',
            'repeated edit cycles reuse peak row capacity',
            'multi-page over-limit refusal returns original state',
            'at-limit acceptance and subsequent compaction'],
), indent=2))
