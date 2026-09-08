#requires -Version 5.1
param(
    [Parameter(Mandatory = $true)][string]$ProjectPath,
    [ValidateSet('Package', 'Folder')][string]$Output = 'Package',
    [string]$SdkRoot = $env:SHARPPROSPERO_ROOT,
    [string]$TitleId = ''
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$ProjectPath = (Resolve-Path $ProjectPath).Path
if (-not $SdkRoot) { throw 'SHARPPROSPERO_ROOT (or -SdkRoot) is required.' }
$SdkRoot = (Resolve-Path $SdkRoot).Path
$env:SHARPPROSPERO_ROOT = $SdkRoot

& (Join-Path $PSScriptRoot 'doctor.ps1') -SdkRoot $SdkRoot

$projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
$logDirectory = Join-Path $repoRoot 'artifacts/logs'
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
$logPath = Join-Path $logDirectory ("build-" + $projectName + '.log')
$buildDriver = Join-Path $SdkRoot 'build/build-app.ps1'
$projectDirectory = Split-Path -Parent $ProjectPath
$outputFolder = Join-Path $projectDirectory 'out'

try {
    Start-Transcript -Path $logPath -Force | Out-Null
    Write-Host "Building $ProjectPath"
    Write-Host "SDK: $SdkRoot"
    if ($TitleId) {
        & $buildDriver -ProjectPath $ProjectPath -Output $Output -OutputFolder $outputFolder -SdkRoot $SdkRoot -TitleId $TitleId
    } else {
        & $buildDriver -ProjectPath $ProjectPath -Output $Output -OutputFolder $outputFolder -SdkRoot $SdkRoot
    }
    if ($LASTEXITCODE -ne 0) { throw "SharpProspero build exited with code $LASTEXITCODE." }
    $module = Join-Path $outputFolder 'module/eboot.bin'
    if (-not (Test-Path $module)) { throw "Build returned without expected module: $module" }
    if ($Output -eq 'Package' -and -not (Get-ChildItem $outputFolder -Filter '*.pkg' -File -Recurse -ErrorAction SilentlyContinue)) {
        throw "Package output was requested but no .pkg exists below $outputFolder"
    }
    Write-Host "BUILD OK: $outputFolder"
}
catch {
    Write-Error $_ -ErrorAction Continue
    Write-Host "Complete build transcript: $logPath"
    throw
}
finally {
    try { Stop-Transcript | Out-Null } catch { }
}
