# MM ENGINE architecture

MM ENGINE is a standalone, layered runtime. Portable code (`Core`, `Scene`, `Assets`, `Input`, and `Animation`) has no SharpProspero dependency; native 2D/3D and console lifecycle code sit behind dedicated projects. Applications compose those layers without needing AGC knowledge, while `MultiMeshRenderer3D.DrawSubMesh` remains the advanced native escape hatch.

The complete dependency, frame-ownership, lifecycle, diagnostics, rendering-boundary, and resource-ownership specification is in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

