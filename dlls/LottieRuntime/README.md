# LottieRuntime

A native runtime interpreter for Lottie animations.

`LottieRuntime` builds a Windows.UI.Composition visual tree from a serialized
composition, at runtime, without a JSON parser and without generated code. It is a
header and a source file, so it can be built as a DLL that several applications
share, or compiled straight into one.

## How it fits in

Lottie-Windows already turns a Lottie file into a `WinCompData` graph, which is a
description of a composition tree. There were two things that could consume that
graph:

| Consumer | When it runs | Cost |
| --- | --- | --- |
| `Instantiator` | Runtime, in the app | Ships the whole JSON reader and translator |
| `LottieGen` code generators | Build time | No runtime cost, but one class per animation |

This adds a third. `CompositionSerializer` writes the `WinCompData` graph into a
FlatBuffer at build time, and `LottieRuntime` reads that buffer at runtime. The
animation stays data, so it can be swapped, downloaded or themed without a rebuild,
and the code that reads it is the same size no matter how many animations an
application has.

```
Lottie JSON --> LottieCompositionReader --> LottieData
                                              |
                                              v
                                       LottieToWinComp
                                              |
                                              v
                                        WinCompData graph
                                        /      |        \
                                       /       |         \
                              Instantiator  UIDataCodeGen  CompositionSerializer
                              (runtime C#)   (C#/C++/CX)          |
                                                                  v
                                                            .lcomp buffer
                                                                  |
                                                                  v
                                                            LottieRuntime
                                                            (runtime C++)
```

Because the FlatBuffer is written from the same graph the other two consume, all
three produce the same visual tree. The round trip tests in
`tests/CompDataFlatbuffer.Tests` assert exactly that.

## Producing a buffer

```
LottieGen -InputFile animation.json -Language flatbuffer -OutputFolder .
```

This writes `Animation.lcomp`. The format is described by
[`source/LottieFlatbuffer/lottie_comp.fbs`](../../source/LottieFlatbuffer/lottie_comp.fbs).

## Using it

```cpp
#include <LottieRuntime.h>

auto bytes = ReadFileBytes(L"Animation.lcomp");
auto root = CommunityToolkit::WinUI::Lottie::LoadComposition(compositor, bytes);

// Drive the animation by animating one scalar from 0 to 1.
auto progress = CommunityToolkit::WinUI::Lottie::ProgressPropertySet(root);
progress.StartAnimation(L"Progress", playAnimation);

ElementCompositionPreview::SetElementChildVisual(element, root);
```

`LoadComposition` returns a `Visual`, which is the least derived type that describes
the root. A caller that parents the tree and drives its progress never needs more
than that.

## Design

**Least derived interfaces.** A node's concrete type is known only inside the
function that creates it. The realization caches hold `Visual`,
`CompositionShape`, `CompositionBrush`, `CompositionGeometry` and so on, and every
call the interpreter makes afterwards is declared on those types. There is no point
at which the interpreter asks what a node actually is, because the buffer already
said, and the answer was consumed at the moment of creation. The one exception is
`ObjectCategory::Surface` in an object reference, where the buffer permits an image
surface that is not a `CompositionObject`; that is a validation check, not a
downcast in the normal path.

**Size first.** There is one function per node *category*, not per node type, and
the state shared by every node is applied by a single function that takes
`CompositionObject`. Everything is in one translation unit, so `/OPT:REF` discards
the code for whatever an application never reaches.

**Runtime cost second.** Each node is realized at most once. Lookups are a bounds
check and an array index; there is no hash table anywhere. The buffer is read in
place, so no intermediate object model is built.

**Transient memory third.** The only allocations are the caches, which are exactly
as long as the buffer's node vectors, and the D2D geometries, which are released as
soon as they have been given to a `CompositionPath`.

**Untrusted input.** A buffer may have come from a file or a download. It is checked
for the `LCMP` identifier, run through the FlatBuffers verifier, and every index
read out of it is range checked. A malformed buffer throws
`winrt::hresult_invalid_argument`; it never reads outside the buffer.

## Dependencies

Direct2D, for path geometry, and the composition effect interop interface, for the
two effects the translator emits. There is no dependency on Win2D: the interpreter
implements `IGeometrySource2DInterop` and `IGraphicsEffectD2D1Interop` itself, which
is a few dozen lines and avoids pulling a large component into every application.

Requires the FlatBuffers C++ runtime headers (headers only, no library) from
[flatbuffers 25.2.10](https://github.com/google/flatbuffers/releases/tag/v25.2.10),
which must match the `flatc` used to generate `Generated/lottie_comp_generated.h`.

## Limitations

`LoadedImageSurface` is a Windows.UI.Xaml.Media type, so an interpreter that is
usable outside XAML cannot create one. An animation that embeds a raster image
throws `winrt::hresult_not_implemented`. Custom animation controllers are rejected
for the same reason the managed `Instantiator` rejects them: the composition engine
has no way to express one.

## Status

**This code has not been compiled.** It was written on a Linux host, where neither
MSVC nor the Windows SDK is available, so it has never been through a compiler and
must be treated as unverified. Its behaviour is specified by `Instantiator.cs` and
by `CompositionDeserializer.cs`, both of which are exercised by the tests in
`tests/CompDataFlatbuffer.Tests`, so the shape of what it does is well tested even
though this particular expression of it is not.
