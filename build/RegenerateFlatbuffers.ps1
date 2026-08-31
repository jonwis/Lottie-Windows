# Regenerates the FlatBuffers code for lottie_comp.fbs.
#
# The generated C# and C++ sources are checked in so that contributors do not
# need flatc installed in order to build. Run this script after any change to
# source/LottieFlatbuffer/lottie_comp.fbs, and check in the result.
#
# The version of flatc MUST match the version of the Google.FlatBuffers NuGet
# package referenced by dlls/CompDataFlatbuffer/CompDataFlatbuffer.dll.csproj,
# because the generated C# calls a version-stamped validation method
# (FlatBufferConstants.FLATBUFFERS_<version>) that only exists in the matching
# package.
#
# Download flatc from https://github.com/google/flatbuffers/releases/tag/v25.2.10

[CmdletBinding()]
param(
    # Path to the flatc executable. Defaults to whatever is on the PATH.
    [string]$Flatc = "flatc"
)

$ErrorActionPreference = "Stop"

$expectedVersion = "25.2.10"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$schema = Join-Path $repoRoot "source/LottieFlatbuffer/lottie_comp.fbs"
$csharpOut = Join-Path $repoRoot "source/CompDataFlatbuffer/Generated"
$cppOut = Join-Path $repoRoot "dlls/LottieRuntime/Generated"

$actualVersion = (& $Flatc --version) -replace '^flatc version ', ''
if ($actualVersion.Trim() -ne $expectedVersion) {
    throw "flatc version $expectedVersion is required, but '$Flatc' is version $($actualVersion.Trim())."
}

New-Item -ItemType Directory -Force -Path $csharpOut, $cppOut | Out-Null

# --gen-onefile keeps the checked in C# to a single file rather than one file
# per type spread over a namespace directory tree.
& $Flatc --csharp --gen-onefile -o $csharpOut $schema
if ($LASTEXITCODE -ne 0) { throw "flatc failed to generate C#." }

& $Flatc --cpp --scoped-enums -o $cppOut $schema
if ($LASTEXITCODE -ne 0) { throw "flatc failed to generate C++." }

Write-Host "Generated FlatBuffers code from $schema"
