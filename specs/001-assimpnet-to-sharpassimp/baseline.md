# Import baseline (AssimpNet 5.0.0-beta1, pre-migration)

Recorded 2026-10-12 via `Importer.Load(path)` with
`BuildOctree = false`, default `ImporterConfiguration`.

| File | Meshes | Vertices | Materials |
| --- | --- | --- | --- |
| `Models/obj/bunny/bunny.obj` | 1 | 2503 | DiffuseMaterialCore |
| `Models/glTF/robot nier/scene.gltf` | 8 | 31047 | PbrMaterialCore |
| `Models/stl/cube.stl` | 1 | 18 | PhongMaterialCore |
| `Models/ply/tetra_ascii.ply` | 1 | 4 | DiffuseMaterialCore |
| `Models/FBX/Solus The Knight/Solus_The_Knight.fbx` | 1 | 1816 | PhongMaterialCore |

Round-trip tests MUST assert these numbers after the migration (strict
equality; justified exceptions commented inline).
