#!/usr/bin/env pwsh
# Clean build script (Windows/PowerShell) — per the brief's "clean-build scripts" requirement.
# Removes all build output, restores, builds Release, and runs the full test suite.
param(
    [switch]$Publish
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "== Cleaning bin/obj/publish ==" -ForegroundColor Cyan
Get-ChildItem -Path $root -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $root "publish") -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "== Restoring ==" -ForegroundColor Cyan
dotnet restore (Join-Path $root "Lanternmere.sln")

Write-Host "== Building (Release) ==" -ForegroundColor Cyan
dotnet build (Join-Path $root "Lanternmere.sln") --configuration Release --no-restore

Write-Host "== Running tests ==" -ForegroundColor Cyan
dotnet test (Join-Path $root "test/Lanternmere.Tests/Lanternmere.Tests.csproj") --configuration Release --no-build

if ($Publish) {
    Write-Host "== Publishing win-x64 ==" -ForegroundColor Cyan
    dotnet publish (Join-Path $root "src/Lanternmere/Lanternmere.csproj") -c Release -r win-x64 --self-contained false -o (Join-Path $root "publish/win-x64")
}

Write-Host "Done." -ForegroundColor Green
