# Capability status matrix

Status vocabulary:

- **Host-verified**: exercised on a workstation during maintainer validation.
- **Hardware-verified (staged)**: the exact behavior crossed a real PS5 staged probe.
- **Implemented / source-verified**: code is present and maps to a verified SDK path, but the exact complete behavior still needs hardware confirmation.
- **Experimental**: implemented, but end-to-end correctness is not claimed.
- **Blocked**: the verified SDK/toolchain path is incomplete; MM ENGINE does not guess missing state.

| Capability | Status | Evidence / notes |
|---|---|---|
| Core loop/time | Host-verified; hardware path crossed indirectly | `EngineRuntime`, `EngineClock`; portable tests and passing normal app probe path |
| Scene/GameObject/component lifecycle | Host-verified; hardware path crossed indirectly | reflection-free lifecycle and scene loader |
| Transform hierarchy | Host-verified | parent cycle checks, row-vector world composition |
| `.mmmesh` codec | Host-verified | canonical encoder/decoder/validator |
| OBJ import | Host-verified | geometry, UV, normals, groups, MTL/material warnings |
| GLB import | Implemented / source-verified | glTF 2.0 scene/accessor path; validate representative assets |
| Native 2D primitives | Hardware-verified (staged) | display/canvas/platform-2D probes reached real frame/present |
| 2D image module load | Hardware-verified (staged) for PngDec load | PNG decode itself remains a later exact gate |
| 2D alpha and scaling | Implemented / source-verified | exact complete Hello2D visual check still pending |
| Nine-slice composition | Implemented / source-verified | exact hardware visual gate pending |
| TrueType AA text | **Current hardware failure boundary** | module loads and asset read passed; `TrueTypeFont.Load` remains focal point |
| UI widgets | Implemented | full Hello2D UI hardware gate pending |
| DualSense buttons/input path | Hardware-verified (staged) | pad probe passed; not every output feature individually cleared |
| Sticks/triggers | Implemented / source-verified | InputDemo complete-range validation pending |
| Motion/touch snapshot | Implemented / source-verified | targeted hardware telemetry test pending |
| Vibration/light bar output | Implemented / source-verified | targeted output validation pending |
| 3D vertex/index upload | Implemented / source-verified | complete Hello3D hardware gate pending |
| Camera/model transforms | Implemented | complete Hello3D hardware gate pending |
| Multi-mesh one-frame submission | Experimental / source-verified | AGC Begin/Draw/End adaptation; hardware gate pending |
| 3D vertex/material color | Experimental / source-verified | built-in mesh shader path; visual gate pending |
| 3D UV/material abstraction | Host-verified / implemented | `.mmmesh` retains UVs, submeshes, material metadata |
| GNF texture preprocessing | Implemented / source-verified | host converter path; does not prove 3D sampling |
| 3D texture sampling | **Blocked** | no verified complete pixel-stage texture binding path for this renderer |
| Correct depth testing | **Blocked** | no verified complete depth defaults/clear/bind sequence |
| 3D alpha blending | **Blocked** | no verified complete blend-state sequence for this renderer |
| Mixed CPU UI + tiled GPU frame | **Blocked** | no verified composition path with unambiguous frame ownership |
| NativeAOT compile/link/sign | Exercised during maintainer validation | normal application path reached signed module output |
| Package creation | Environment-dependent / not universally claimed | keep separate from signed Folder output |
| Hardware execution | Partial staged validation | exact passed probes documented in `HARDWARE_TEST_LOG.md` |

The public alpha uses the exact-state rule: a passing neighboring stage does not promote an untested capability to hardware-verified.
