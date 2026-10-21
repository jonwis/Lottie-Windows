[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,

    [string]$FlatBuffersIncludeDirectory = (Join-Path $PSScriptRoot "..\..\vcpkg_installed\x64-windows\include")
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $PackagePath))
try {
    $required = @(
        "LottieRuntime/lottie_comp.fbs",
        "LottieRuntime/LottieRuntime.cpp",
        "LottieRuntime/LottieVisualSource.cpp",
        "LottieRuntime/flatbuffers.LICENSE.txt",
        "inc/lottiecomp/LottieRuntime.h",
        "inc/lottiecomp/LottieVisualSource.h",
        "inc/lottiecomp/Generated/lottie_comp_generated.h",
        "inc/lottiecomp/flatbuffers/flatbuffers.h",
        "inc/lottiecomp/flatbuffers/base.h"
    )
    foreach ($path in $required) {
        $entry = $archive.GetEntry($path)
        if (!$entry -or $entry.Length -eq 0) {
            throw "Missing or empty package entry: $path"
        }
    }

    $headers = @($archive.Entries | Where-Object FullName -like "*.h")
    foreach ($header in $headers) {
        if (!$header.FullName.StartsWith("inc/lottiecomp/") -or $header.Length -eq 0) {
            throw "Invalid package header: $($header.FullName)"
        }
    }

    $flatBuffersRoot = (Resolve-Path (Join-Path $FlatBuffersIncludeDirectory "flatbuffers")).Path
    $flatBuffersHeaders = Get-ChildItem -LiteralPath $flatBuffersRoot -Recurse -File -Filter "*.h"
    foreach ($header in $flatBuffersHeaders) {
        $relativePath = $header.FullName.Substring($flatBuffersRoot.Length + 1).Replace("\", "/")
        $path = "inc/lottiecomp/flatbuffers/$relativePath"
        $entry = $archive.GetEntry($path)
        if (!$entry -or $entry.Length -ne $header.Length) {
            throw "Missing or truncated FlatBuffers header: $path"
        }
    }

    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
    $sources = @{
        "LottieRuntime/lottie_comp.fbs" = "source\LottieFlatbuffer\lottie_comp.fbs"
        "inc/lottiecomp/Generated/lottie_comp_generated.h" = "source\LottieRuntime\Generated\lottie_comp_generated.h"
    }
    foreach ($path in $sources.Keys) {
        $reader = [IO.StreamReader]::new($archive.GetEntry($path).Open())
        try {
            $packaged = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
        if ($packaged -cne [IO.File]::ReadAllText((Join-Path $repoRoot $sources[$path]))) {
            throw "Package entry differs from source: $path"
        }
    }

    Write-Host "Package contains the schema, sources, license, and $($headers.Count) headers under inc/lottiecomp."
}
finally {
    $archive.Dispose()
}
