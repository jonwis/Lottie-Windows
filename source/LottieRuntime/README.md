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

Animations containing loaded image surfaces must be loaded on the XAML UI thread
with the XAML host's compositor (for example, the compositor obtained from
`ElementCompositionPreview::GetElementVisual(element)`). `LoadedImageSurface`
uses XAML's image pipeline; initializing only a dispatcher queue or supplying an
unrelated `Compositor` is not sufficient for these assets.

### Loading metadata and live theme properties

```cpp
namespace Lottie = CommunityToolkit::WinUI2::Lottie;
auto animation = Lottie::LoadCompositionWithMetadata(compositor, bytes);
auto root = animation.Root;                  // Visual
auto duration = animation.Duration;          // Windows::Foundation::TimeSpan
auto size = animation.Size;                  // Windows::Foundation::Numerics::float2
auto framesPerSecond = animation.FramesPerSecond; // float
auto progress = Lottie::ProgressPropertySet(root);
for (auto const& marker : animation.Markers)
{
    // marker.Name is hstring; Progress and DurationProgress are floats.
    RegisterMarker(marker.Name, marker.Progress, marker.DurationProgress);
}
if (animation.ThemeProperties)
{
    // Use the actual serialized property name and Composition storage type.
    animation.ThemeProperties.InsertVector4(L"Foreground", foregroundVector);
}
```

`LoadedComposition` owns its metadata and retains the Composition objects; the
input buffer may be released after loading. `Duration.count()` is the serialized
duration in **100ns ticks**, not milliseconds. `Size` and `FramesPerSecond` are
the serialized source dimensions and frame rate, not inferred from the visual
tree. Serialized zero values are preserved, including buffers produced without
source timing metadata.

`Markers` is a `std::vector<CompositionMarker>` with `Name`, `Progress`, and
`DurationProgress`. Both numeric values are fractions of the entire animation's
duration, not frame numbers or seconds. Names, order, duplicates, and finite
out-of-playback-range markers are preserved, matching the serializer's unfiltered
source markers. A host projecting markers to `IMapView<hstring, double>` must
choose its own duplicate-name and out-of-range policy. The runtime does not
invent separate start/end marker names, clamp markers, or nudge frame positions.

`ThemeProperties` is nullable. When present it is the **same standalone
CompositionPropertySet** used by expressions in the loaded graph, not the root's
progress property set and not a new set populated from metadata defaults.
Updating an existing property therefore targets the loaded animation. Color
bindings may be stored as `Vector4`; use the names/types generated for the asset.
Theme lifetime follows the returned Composition objects.

### Observing and reusing loaded images

`LoadedComposition.ImageSurfaces` is a
`std::vector<CompositionImageSurface>`. Each descriptor contains `Index`
(`uint32_t`, the serialized surface-table index) and `Surface`
(`ICompositionSurface`). These are the **actual cached `LoadedImageSurface`
objects used by the returned graph**, not new image loads. Descriptors are in
serialized index order; visual surfaces and unrealized images are excluded.
The runtime header does not add a XAML dependency for this field.

A WinUI 2 host can include `<winrt/Windows.UI.Xaml.Media.h>`, cast a descriptor's
`Surface` to `winrt::Windows::UI::Xaml::Media::LoadedImageSurface`, and subscribe
to its `LoadCompleted` event. A WinUI 3 host uses the matching
`Microsoft.UI.Xaml.Media` projection. Image completion, asynchronous-loading
policy, notifications, retry/fallback, and event revocation belong to the host.
The runtime starts image loading but does not wait for decoding or substitute
a static visual. A host waiting for images should retain the first
`LoadedComposition` and attach its already-created root once ready.

To create another tree without restarting those image loads:

```cpp
auto another = Lottie::LoadCompositionWithMetadata(
    compositor, bytes, animation.ImageSurfaces);
```

This additive three-argument overload seeds the new interpreter's image cache.
The original two-argument overload and `LoadComposition` remain available.
Only reuse descriptors from the **same serialized buffer and XAML host
compositor**; indices do not identify images across different files or UAP
variants. Null objects, duplicate/out-of-range indices, nonimage surface indices,
and objects that are not the selected projection's `LoadedImageSurface` are
rejected with `winrt::hresult_invalid_argument`. Images without a supplied
descriptor are loaded normally.

`LoadCompositionWithMetadata` requires a metadata table and rejects negative
duration/dimensions/frame rate/marker duration, nonfinite numeric metadata,
invalid string/property-set indices and a theme index that refers to an owned
property set. Missing markers mean an empty vector; the theme sentinel means
`nullptr`. These are explicit format conventions, not exception fallbacks.
`LoadComposition` retains its original Visual-only contract and can still load
buffers without metadata.

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

### Focused native metadata tests

`tests\LottieRuntime.Tests\MetadataTests.cpp` exercises the native API, malformed
metadata, buffer verification, the legacy no-metadata contract, and theme-cache
identity, including a child's expression referencing its not-yet-initialized
parent's owned properties, image/brush cache identity, image reuse across new
trees, and invalid image-cache descriptors. It uses the existing generated schema
and vendored FlatBuffers, without an additional test framework. The harness initializes XAML
on its dispatcher thread and uses its host visual's compositor to exercise image
assets through `LoadedImageSurface`; its manifest declares the Windows version
required for desktop XAML hosting.
From an x64 Visual Studio Developer Command Prompt at the repository root:

```cmd
mkdir tests\LottieRuntime.Tests\bin
cl /nologo /std:c++20 /EHsc /W4 /WX /DWIN32_LEAN_AND_MEAN /DNOMINMAX /DLOTTIE_RUNTIME_WINUI2 /Iexternal\flatbuffers\include tests\LottieRuntime.Tests\MetadataTests.cpp /Fotests\LottieRuntime.Tests\bin\MetadataTests.obj /Fetests\LottieRuntime.Tests\bin\MetadataTests.exe /link /MANIFEST:EMBED /MANIFESTINPUT:tests\LottieRuntime.Tests\MetadataTests.manifest CoreMessaging.lib d2d1.lib dxguid.lib ole32.lib shcore.lib shlwapi.lib windowsapp.lib
tests\LottieRuntime.Tests\bin\MetadataTests.exe
```

Pass optional `.lcomp` paths to also load real generator output and print its
dimensions, duration, frame rate, marker count, and realized image count. For
image-bearing files the runner also creates another tree with the returned image
descriptors and asserts that every image object is reused rather than reloaded.
Prefix the paths with `--legacy` to exercise the Visual-only `LoadComposition`
path instead, separating graph-realization failures from metadata validation.
