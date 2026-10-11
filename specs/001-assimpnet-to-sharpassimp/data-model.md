# Data Model: AssimpNet to SharpAssimp Migration

No persistent data changes. Test-only entities:

## Round-trip baseline

| Field | Meaning |
| --- | --- |
| `File` | sample model path under `Models/` |
| `MeshCount` | `HelixToolkitScene` mesh/node count after import |
| `VertexCount` | total vertices across meshes |
| `HasMaterials` | at least one material with textures resolved |
| `HasAnimation` | animations present (FBX bone sample only) |

- Recorded pre-migration; pinned as constants in the round-trip test.
- Strict equality by default; justified exceptions commented inline (Q3).

## Large-model opt-in set

| File | Size class |
| --- | --- |
| `Models/ply/stanford_bunny_ascii.ply` | medium (3 MB) |
| `Models/ply/stanford_armadillo_binary_big_endian.ply` | large (7 MB) |
| `Models/ply/stanford_dragon_full_ascii.ply` | xlarge (34 MB) |

Runs under `Category=LargeModels`, excluded from the default gate.
