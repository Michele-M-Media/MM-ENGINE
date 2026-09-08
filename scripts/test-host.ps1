#requires -Version 5.1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
& dotnet run --project (Join-Path $root 'tests/MMEngine.Tests/MMEngine.Tests.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Portable test suite failed.' }
