#requires -Version 5.1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'examples/models/model-viewer.obj'
$output = Join-Path $root 'samples/ModelViewer/assets/model.mmmesh'
$verification = Join-Path $root 'samples/ModelViewer/assets/model.verify.mmmesh'
& dotnet run --project (Join-Path $root 'tools/MMEngine.Cli/MMEngine.Cli.csproj') -c Release -- mesh import $source $output
if ($LASTEXITCODE -ne 0) { throw 'Model fixture conversion failed.' }
try {
    & dotnet run --project (Join-Path $root 'tools/MMEngine.Cli/MMEngine.Cli.csproj') -c Release -- mesh import $source $verification
    if ($LASTEXITCODE -ne 0) { throw 'Second deterministic conversion failed.' }
    $firstHash = (Get-FileHash $output -Algorithm SHA256).Hash
    $secondHash = (Get-FileHash $verification -Algorithm SHA256).Hash
    if ($firstHash -ne $secondHash) { throw 'Two identical imports produced different bytes.' }
    & dotnet run --project (Join-Path $root 'tools/MMEngine.Cli/MMEngine.Cli.csproj') -c Release -- mesh validate $output
    if ($LASTEXITCODE -ne 0) { throw 'Model fixture validation failed.' }
    Write-Host "Deterministic asset SHA-256: $firstHash"
}
finally {
    Remove-Item $verification -Force -ErrorAction SilentlyContinue
}
