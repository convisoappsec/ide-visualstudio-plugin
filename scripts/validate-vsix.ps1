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
if ($pkgdefContent -notmatch "Menus\.ctmenu") {
    throw ".pkgdef does not reference Menus.ctmenu."
}

Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class ResourceInspector
{
    private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

    private delegate bool EnumResTypeProc(IntPtr hModule, IntPtr lpszType, IntPtr lParam);
    private delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hLibModule);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EnumResourceTypes(IntPtr hModule, EnumResTypeProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpszType, EnumResNameProc lpEnumFunc, IntPtr lParam);

    public static string[] GetResourceNames(string dllPath)
    {
        var names = new List<string>();
        var module = LoadLibraryEx(dllPath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
        if (module == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "LoadLibraryEx failed.");
        }

        try
        {
            EnumResTypeProc typeProc = (h, typePtr, l) =>
            {
                EnumResNameProc nameProc = (h2, t, namePtr, l2) =>
                {
                    names.Add(PointerToString(namePtr));
                    return true;
                };

                EnumResourceNames(h, typePtr, nameProc, IntPtr.Zero);
                return true;
            };

            EnumResourceTypes(module, typeProc, IntPtr.Zero);
        }
        finally
        {
            FreeLibrary(module);
        }

        return names.ToArray();
    }

    private static string PointerToString(IntPtr ptr)
    {
        ulong value = unchecked((ulong)ptr.ToInt64());
        if ((value >> 16) == 0)
        {
            return "#" + (value & 0xFFFF);
        }

        return Marshal.PtrToStringUni(ptr) ?? string.Empty;
    }
}
"@

$resourceNames = [ResourceInspector]::GetResourceNames($dll.FullName)

if ($resourceNames -notcontains "Menus.ctmenu") {
    $found = if ($resourceNames.Count -gt 0) { $resourceNames -join ", " } else { "<none>" }
    throw "Menus.ctmenu not embedded in DLL. Resources found: $found"
}

Write-Host "Validated VSIX: $VsixPath"
Write-Host "pkgdef: $($pkgdef.Name)"
Write-Host "dll: $($dll.Name)"
Write-Host "native resource: Menus.ctmenu"
