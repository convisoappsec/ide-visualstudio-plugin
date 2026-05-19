param(
    [Parameter(Mandatory = $true)]
    [string]$VsixPath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $VsixPath)) {
    throw "VSIX not found: $VsixPath"
}

$workspace = Split-Path -Parent $PSScriptRoot
$extractDir = Join-Path $workspace ".artifacts\vsix-validation"

if (Test-Path -LiteralPath $extractDir) {
    Remove-Item -LiteralPath $extractDir -Recurse -Force
}

Expand-Archive -LiteralPath $VsixPath -DestinationPath $extractDir -Force

$pkgdef = Get-ChildItem -Path $extractDir -Filter *.pkgdef -Recurse | Select-Object -First 1
$dll = Get-ChildItem -Path $extractDir -Filter Conviso.Platform.VisualStudio.dll -Recurse | Select-Object -First 1

if (-not $pkgdef) {
    throw "VSIX does not contain a .pkgdef file."
}

if (-not $dll) {
    throw "VSIX does not contain Conviso.Platform.VisualStudio.dll."
}

$pkgdefContent = Get-Content -LiteralPath $pkgdef.FullName -Raw
if ($pkgdefContent -notmatch '=\s*"\s*,\s*Menus\.ctmenu,\s*1"') {
    throw ".pkgdef does not reference Menus.ctmenu."
}

$assembly = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($dll.FullName)
$manifestNames = $assembly.GetManifestResourceNames()

if ($manifestNames -notcontains "_EmptyResource.resources") {
    $found = if ($manifestNames.Count -gt 0) { $manifestNames -join ", " } else { "<none>" }
    throw "Managed resource container '_EmptyResource.resources' not found. Manifest resources: $found"
}

$resourceStream = $assembly.GetManifestResourceStream("_EmptyResource.resources")
if (-not $resourceStream) {
    throw "Failed to open _EmptyResource.resources from assembly."
}

$reader = New-Object System.Resources.ResourceReader($resourceStream)
$menuResourceFound = $false

try {
    $enumerator = $reader.GetEnumerator()
    while ($enumerator.MoveNext()) {
        if ($enumerator.Key -eq "Menus.ctmenu" -and $enumerator.Value -is [byte[]]) {
            $menuResourceFound = $true
            break
        }
    }
}
finally {
    $reader.Close()
    $resourceStream.Close()
}

if (-not $menuResourceFound) {
    throw "Menus.ctmenu byte[] entry not found inside _EmptyResource.resources."
}

Write-Host "Validated VSIX: $VsixPath"
Write-Host "pkgdef: $($pkgdef.Name)"
Write-Host "dll: $($dll.Name)"
Write-Host "managed resource: _EmptyResource.resources -> Menus.ctmenu"
