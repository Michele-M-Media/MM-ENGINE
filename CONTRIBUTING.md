# Contributing

Keep the portable projects independent of SharpProspero. Platform APIs belong in `MMEngine.Platform.PS5`; renderer-specific native types belong in the corresponding graphics project. New scene-loadable components must have an explicit factory path compatible with NativeAOT.

Every behavior change should include a dependency-free executable test where possible. Renderer changes must cite the public SDK source/API they use and add a focused hardware check. Do not commit guessed AGC register offsets, opaque shader binaries without reproducible provenance, copyrighted reference models, package output, or console secrets.

Run before submitting:

```powershell
dotnet build MMEngine.Portable.slnx -c Release
dotnet run --project tests/MMEngine.Tests/MMEngine.Tests.csproj -c Release
```

For PS5-facing changes, also build the smallest affected sample and attach the full transcript plus the relevant section of `docs/HARDWARE_TEST_PLAN.md`.
