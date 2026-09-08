#requires -Version 5.1
param(
    [ValidateSet('Package', 'Folder')][string]$Output = 'Package',
    [string]$SdkRoot = $env:SHARPPROSPERO_ROOT
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$projects = @(
    'samples/Hello2D/Hello2D.csproj',
    'samples/Hello3D/Hello3D.csproj',
    'samples/InputDemo/InputDemo.csproj',
    'samples/ModelViewer/ModelViewer.csproj'
)
foreach ($relative in $projects) {
    & (Join-Path $PSScriptRoot 'build-ps5.ps1') -ProjectPath (Join-Path $root $relative) -Output $Output -SdkRoot $SdkRoot
}
Write-Host 'All MM ENGINE samples built successfully.'
