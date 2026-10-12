# LottieRuntime

Native C++/WinRT source builders for serialized Lottie animations.

The reader builds a Composition visual tree directly with the caller's
`Compositor`. There is no COM activation layer and no runtime DLL. The consumer
compiles `LottieRuntime.cpp` with exactly one projection definition:

| Composition API | Preprocessor definition |
| --- | --- |
| `Windows.UI.Composition` (WinUI 2/UWP) | `LOTTIE_RUNTIME_WINUI2` |
| `Microsoft.UI.Composition` (WinUI 3/Windows App SDK) | `LOTTIE_RUNTIME_WINUI3` |

The source and header reject builds where neither or both definitions are set.
This keeps the ABI-incompatible projections separate without duplicating the
interpreter.

## Producing buffers

```powershell
LottieGen -InputFile animation.json -Language flatbuffer -OutputFolder .
```

If one translation covers the requested UAP range, this writes
`Animation.lcomp`. If fallbacks are required, LottieGen writes one file per
supported range, for example:

```text
Animation.uap14-plus.lcomp
Animation.uap10-13.lcomp
Animation.uap7-9.lcomp
```

The consumer selects the file matching its Composition API contract. The data is
not tied to either projection: the same variant can be read by the WinUI 2 or
WinUI 3 builder when that projection provides the APIs required by the variant.

## Incorporating the reader

Add `LottieRuntime.cpp` to the native project, define the matching projection
flag, and add `LottieRuntime/` and `LottieRuntime/flatbuffers/include/` to its
include paths. Build as C++20 and link:

```text
d2d1.lib
dxguid.lib
shcore.lib
shlwapi.lib
windowsapp.lib
```

Include `LottieRuntime.h`, create the compositor in the host, and pass it to the
reader:

```cpp
#include <LottieRuntime.h>

auto bytes = ReadFileBytes(L"Animation.lcomp");
auto root = CommunityToolkit::WinUI2::Lottie::LoadComposition(compositor, bytes);
```

The same projection definition must be visible to every file that includes
`LottieRuntime.h`. WinUI 3 builds use the corresponding
`CommunityToolkit::WinUI3::Lottie` namespace. The distinct namespaces allow both
projection-specific objects to be linked into the same binary.

`LoadComposition` returns the root `Visual`. `ProgressPropertySet(root)` returns
the Composition property set containing the `Progress` scalar. XAML integration
and `IAnimatedVisual` wrapping remain the caller's responsibility.

## Validation and safety

The reader verifies the FlatBuffer identifier and structure, range-checks every
node index, rejects unsupported schema or API-contract versions, detects geometry
cycles, and limits recursive visual, shape, and geometry traversal to 128 levels.
Malformed input throws `winrt::hresult_invalid_argument`; unsupported content
throws `winrt::hresult_not_implemented`.

## Validation tool

`LottieRuntimeTool.vcxproj` builds `LottieRuntime.exe`, a WinUI 2 validation and
inspection harness that compiles the reader directly.

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\Tools\Launch-VsDevShell.ps1" -SkipAutomaticLocation
msbuild .\source\LottieRuntime\LottieRuntimeTool.vcxproj /p:Platform=x64 /p:Configuration=Release
```

The executable is written to:

```text
source\LottieRuntime\x64\Release\LottieRuntime.exe
```

It validates a buffer by default, prints the interpreted Composition hierarchy
with `--dump`, or displays and animates it with `--show`:

```powershell
.\source\LottieRuntime\x64\Release\LottieRuntime.exe Animation.lcomp
.\source\LottieRuntime\x64\Release\LottieRuntime.exe Animation.lcomp --dump
.\source\LottieRuntime\x64\Release\LottieRuntime.exe Animation.lcomp --show
```
