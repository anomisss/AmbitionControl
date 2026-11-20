<#
  Builds a release ZIP from an already-built (and ideally already-tested) AmbitionControl.dll.
  Does not rebuild, so the ZIP contains exactly the binary you point it at.

  Usage (from the repo root):
    powershell -ExecutionPolicy Bypass -File packaging\package.ps1 -Version 0.6.0-alpha.2 [-ExpectedSha256 <hash>]
  Output: dist\AmbitionControl-v<Version>-ea3233-melonloader.zip (dist\ is git-ignored)
#>
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Dll = "MelonLoader\AmbitionControl.Melon\bin\Release\AmbitionControl.dll",
    [string]$ExpectedSha256 = ""
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not (Test-Path $Dll)) { throw "DLL not found: $Dll (build it first)" }
$hash = (Get-FileHash $Dll -Algorithm SHA256).Hash.ToLowerInvariant()
if ($ExpectedSha256 -and $hash -ne $ExpectedSha256.ToLowerInvariant()) {
    throw "DLL hash $hash does not match expected $ExpectedSha256 - refusing to package an untested binary."
}

$name = "AmbitionControl-v$Version-ea3233-melonloader"
$stage = Join-Path $root "dist\$name"
$zip = Join-Path $root "dist\$name.zip"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }
New-Item -ItemType Directory -Force $stage | Out-Null

Copy-Item $Dll (Join-Path $stage "AmbitionControl.dll")
Copy-Item "CHANGELOG.md" (Join-Path $stage "CHANGELOG.md")
$readme = (Get-Content "packaging\README-release.txt" -Raw).Replace("{VERSION}", "v$Version")
Set-Content -Path (Join-Path $stage "README.txt") -Value $readme -Encoding UTF8
Set-Content -Path (Join-Path $stage "SHA256SUMS.txt") -Value "$hash  AmbitionControl.dll" -Encoding ASCII

Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip
$zipHash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
"Packaged $zip"
"DLL SHA-256: $hash"
"ZIP SHA-256: $zipHash"
