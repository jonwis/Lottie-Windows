# Regenerates the FlatBuffers code for lottie_comp.fbs.
#
# The generated C# and C++ sources are checked in so that contributors do not
# need flatc installed in order to build. Run this script after any change to
# source/LottieFlatbuffer/lottie_comp.fbs, and check in the result.
#
# C# output must match the Google.FlatBuffers package available through the
# configured Microsoft feed. C++ output must match external/flatbuffers.
#
# To obtain the matching C++ compiler and headers:
# 1. Download the Windows flatc binary for v25.12.19 from
#    https://github.com/google/flatbuffers/releases/tag/v25.12.19.
# 2. Download and extract the source archive for commit
#    5761d6e67af841d15ee21bc1ce9a78ffa9cf939e.
# 3. Pass the extracted source directory as -CppSource. The script validates
#    its version, copies include/flatbuffers and LICENSE into external/flatbuffers,
#    and regenerates the checked-in C++ binding with the matching flatc.exe.

[CmdletBinding()]
param(
    # C# code must match the Google.FlatBuffers package version.
    [string]$CSharpFlatc = "flatc",

    # C++ code must match the headers in external/flatbuffers.
    [Parameter(Mandatory)]
    [string]$CppFlatc,

    # Optional extracted FlatBuffers source tree. Supply this when updating the
    # vendored headers and license.
    [string]$CppSource
)

$ErrorActionPreference = "Stop"

$expectedCSharpVersion = "25.2.10"
$expectedCppVersion = "25.12.19"
$expectedCppCommit = "5761d6e67af841d15ee21bc1ce9a78ffa9cf939e"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$schema = Join-Path $repoRoot "source/LottieFlatbuffer/lottie_comp.fbs"
$csharpOut = Join-Path $repoRoot "source/CompDataFlatbuffer/Generated"
$cppOut = Join-Path $repoRoot "source/LottieRuntime/Generated"
$vendoredFlatBuffers = Join-Path $repoRoot "external/flatbuffers"

function Get-HeaderVersion([string]$sourceRoot) {
    $baseHeader = Join-Path $sourceRoot "include/flatbuffers/base.h"
    if (!(Test-Path $baseHeader)) {
        throw "FlatBuffers header not found: $baseHeader"
    }

    $contents = Get-Content $baseHeader -Raw
    $major = [regex]::Match($contents, '#define FLATBUFFERS_VERSION_MAJOR\s+(\d+)').Groups[1].Value
    $minor = [regex]::Match($contents, '#define FLATBUFFERS_VERSION_MINOR\s+(\d+)').Groups[1].Value
    $revision = [regex]::Match($contents, '#define FLATBUFFERS_VERSION_REVISION\s+(\d+)').Groups[1].Value

    if (!$major -or !$minor -or !$revision) {
        throw "Could not read the FlatBuffers version from $baseHeader."
    }

    return "$major.$minor.$revision"
}

$actualCSharpVersion = (& $CSharpFlatc --version) -replace '^flatc version ', ''
if ($actualCSharpVersion.Trim() -ne $expectedCSharpVersion) {
    throw "C# flatc version $expectedCSharpVersion is required, but '$CSharpFlatc' is version $($actualCSharpVersion.Trim())."
}

$actualCppVersion = (& $CppFlatc --version) -replace '^flatc version ', ''
if ($actualCppVersion.Trim() -ne $expectedCppVersion) {
    throw "C++ flatc version $expectedCppVersion is required, but '$CppFlatc' is version $($actualCppVersion.Trim())."
}

if ($CppSource) {
    $resolvedCppSource = Resolve-Path $CppSource
    $sourceVersion = Get-HeaderVersion $resolvedCppSource
    if ($sourceVersion -ne $expectedCppVersion) {
        throw "FlatBuffers headers version $expectedCppVersion is required, but '$resolvedCppSource' contains version $sourceVersion."
    }

    $sourceLicense = Join-Path $resolvedCppSource "LICENSE"
    if (!(Test-Path $sourceLicense)) {
        throw "FlatBuffers license not found: $sourceLicense"
    }

    $vendoredHeaders = Join-Path $vendoredFlatBuffers "include/flatbuffers"
    Remove-Item -LiteralPath $vendoredHeaders -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path (Split-Path $vendoredHeaders) | Out-Null
    Copy-Item -LiteralPath (Join-Path $resolvedCppSource "include/flatbuffers") -Destination $vendoredHeaders -Recurse
    Copy-Item -LiteralPath $sourceLicense -Destination (Join-Path $vendoredFlatBuffers "LICENSE") -Force

    Write-Host "Updated vendored FlatBuffers headers from commit $expectedCppCommit"
}

$vendoredVersion = Get-HeaderVersion $vendoredFlatBuffers
if ($vendoredVersion -ne $expectedCppVersion) {
    throw "Vendored FlatBuffers headers must be version $expectedCppVersion, but they are version $vendoredVersion."
}

New-Item -ItemType Directory -Force -Path $csharpOut, $cppOut | Out-Null

# --gen-onefile keeps the checked in C# to a single file rather than one file
# per type spread over a namespace directory tree.
& $CSharpFlatc --csharp --gen-onefile -o $csharpOut $schema
if ($LASTEXITCODE -ne 0) { throw "flatc failed to generate C#." }

& $CppFlatc --cpp --scoped-enums -o $cppOut $schema
if ($LASTEXITCODE -ne 0) { throw "flatc failed to generate C++." }

Write-Host "Generated FlatBuffers code from $schema"
