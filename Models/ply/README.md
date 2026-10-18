# PLY test fixtures

Import test data for the model importer round-trip checks (see
`specs/001-assimpnet-to-sharpassimp`).

## Synthetic (own work, no restrictions)

| File | Format |
| --- | --- |
| `tetra_ascii.ply` | ascii, float coords, `list uchar int` faces |
| `tetra_binary_little_endian.ply` | binary_little_endian, double coords, `list uchar uint` faces |
| `tetra_binary_big_endian.ply` | binary_big_endian, float coords, `list uchar int` faces |

Tetrahedron: 4 vertices, 4 faces.

## Florida State University Burkardt collection (LGPL)

Source: <https://people.sc.fsu.edu/~jburkardt/data/ply/ply.html>

| File | Notes |
| --- | --- |
| `cube_ascii.ply` | 8 vertices |
| `cow_ascii.ply` | 2903 vertices, `float32` property names |
| `cow_binary.ply` | binary_big_endian real-world sample |

Distributed under the GNU LGPL license (see the source page).

## three.js example models (MIT)

Source: <https://github.com/mrdoob/three.js/tree/dev/examples/models/ply>

| File | Notes |
| --- | --- |
| `dolphins_ascii.ply` | ascii |
| `dolphins_binary_little_endian.ply` | binary_little_endian |
| `dolphins_binary_big_endian.ply` | binary_big_endian, 32 KB |

Same dolphin mesh in all three encodings — ideal for format-parity checks.

## Stanford 3D Scanning Repository

Source: <https://graphics.stanford.edu/data/3Dscanrep>
Please acknowledge the Stanford Computer Graphics Laboratory when using
these models.

| File | Notes |
| --- | --- |
| `stanford_bunny_ascii.ply` | `bun_zipper.ply`, 35947 vertices, 69451 faces, with confidence/intensity properties |
| `stanford_armadillo_binary_big_endian.ply` | `Armadillo.ply`, 172974 vertices, 345944 faces, binary_big_endian |
| `stanford_dragon_ascii.ply` | `dragon_vrip_res4.ply` (decimated), 5205 vertices, 11102 faces |
| `stanford_dragon_full_ascii.ply` | `dragon_vrip.ply` (full reconstruction), 437645 vertices, 871414 faces, ~34 MB |
