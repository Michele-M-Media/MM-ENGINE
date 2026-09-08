#requires -Version 5.1
param([string]$SdkRoot = $env:SHARPPROSPERO_ROOT)
$ErrorActionPreference = 'Stop'
$failed = $false

function Check([string]$Name, [bool]$Condition, [string]$Fix) {
    if ($Condition) { Write-Host "[ ok ] $Name"; return }
    Write-Host "[fail] $Name"
    Write-Host "       $Fix"
    $script:failed = $true
}

Write-Host 'MM ENGINE v0.1-alpha environment check'
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
$version = if ($dotnet) { (& dotnet --version | Out-String).Trim() } else { '' }
Check '.NET 10 SDK' ($dotnet -and $version.StartsWith('10.')) 'Install .NET SDK 10 and make dotnet available on PATH.'
Check 'SHARPPROSPERO_ROOT is set' (-not [string]::IsNullOrWhiteSpace($SdkRoot)) 'Set SHARPPROSPERO_ROOT to the supplied SharpProspero 0.8 checkout.'
if ($SdkRoot) {
    $SdkRoot = [System.IO.Path]::GetFullPath($SdkRoot)
    Check 'SharpProspero build driver' (Test-Path (Join-Path $SdkRoot 'build/build-app.ps1')) 'Point SHARPPROSPERO_ROOT at the SDK root, not its src folder.'
    Check 'SharpProspero application targets' (Test-Path (Join-Path $SdkRoot 'build/Prospero.App.targets')) 'Use the complete SharpProspero 0.8 checkout supplied with the bootstrap.'
    Check 'SharpProspero library project' (Test-Path (Join-Path $SdkRoot 'src/SharpProspero/SharpProspero.csproj')) 'The SDK checkout is incomplete.'
    $baselinePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'eng/sdk-baseline.json'
    if (Test-Path $baselinePath) {
        $baseline = Get-Content -Raw $baselinePath | ConvertFrom-Json
        foreach ($entry in $baseline.sourceChecks.PSObject.Properties) {
            $sourcePath = Join-Path $SdkRoot $entry.Name
            $matches = (Test-Path $sourcePath) -and ((Get-FileHash $sourcePath -Algorithm SHA256).Hash -eq $entry.Value)
            Check ("SDK baseline: " + $entry.Name) $matches 'Use one of the two supplied stock SDK archives; do not point MM ENGINE at the patched reference-app copy.'
        }
    }
}

$onWindows = [System.Environment]::OSVersion.Platform -eq 'Win32NT'
if ($onWindows) {
    $wsl = Get-Command wsl.exe -ErrorAction SilentlyContinue
    $wslVersion = if ($wsl) { (& wsl.exe -e bash -lc 'dotnet --version 2>/dev/null' | Out-String).Trim() } else { '' }
    Check 'WSL with .NET 10' ($wsl -and $wslVersion.StartsWith('10.')) 'Install WSL and .NET 10 inside WSL; NativeAOT compilation runs there.'
}

if ($failed) { throw 'MM ENGINE environment check failed.' }
Write-Host 'Environment is ready for the SharpProspero application pipeline.'
