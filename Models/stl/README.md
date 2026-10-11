# STL test fixtures

Import test data for the model importer round-trip checks (see
`specs/001-assimpnet-to-sharpassimp`).

## Synthetic (own work, no restrictions)

| File | Format |
| --- | --- |
| `tetra_ascii.stl` | ascii, 4 facets |
| `tetra_binary.stl` | binary little-endian, 4 facets |

Tetrahedron with explicit facet normals.

## Existing set

| File | Format | Notes |
| --- | --- | --- |
| `cube.stl` | ascii | `solid MYSOLID` |
| `bottle.stl` | ascii | |
| `magnolia.stl` | ascii | |
| `sphere.stl` | ascii | non-standard leading line, `solid BOLLEN` |
| `teapot.stl` | ascii | |
| `suzanne.stl` | binary | Blender export |
| `binary/bottle.stl` | binary | VCG (MeshLab) header |
| `colored_binary.stl` | binary | three.js sample with per-facet colors (Materialise color extension) |
| `stanford_bunny.stl` | binary | Stanford bunny via reprap-io (GPL-3.0, test data only), VCG header |

## Sources

- three.js example models (MIT): <https://github.com/mrdoob/three.js/tree/dev/examples/models/stl>
- Stanford bunny via reprap-io (GPL-3.0): <https://github.com/reprap-io/reprapio_stanford_bunny> —
  vendored as test data only; the repository itself stays MIT-licensed.
- Original Stanford source: <https://graphics.stanford.edu/data/3Dscanrep> (please
  acknowledge the Stanford Computer Graphics Laboratory).
