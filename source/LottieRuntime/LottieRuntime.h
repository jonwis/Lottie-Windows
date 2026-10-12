// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <unknwn.h>

#include <cstddef>
#include <span>

#if defined(LOTTIE_RUNTIME_WINUI2) == defined(LOTTIE_RUNTIME_WINUI3)
#error Define exactly one of LOTTIE_RUNTIME_WINUI2 or LOTTIE_RUNTIME_WINUI3.
#endif

#if defined(LOTTIE_RUNTIME_WINUI3)
#include <winrt/Microsoft.UI.Composition.h>
namespace CommunityToolkit::WinUI3::Lottie
{
    using namespace winrt::Microsoft::UI::Composition;
#else
#include <winrt/Windows.UI.Composition.h>
namespace CommunityToolkit::WinUI2::Lottie
{
    using namespace winrt::Windows::UI::Composition;
#endif

    // Define LOTTIE_RUNTIME_WINUI2 for Windows.UI.Composition or
    // LOTTIE_RUNTIME_WINUI3 for Microsoft.UI.Composition.
    // Builds the composition described by a serialized Lottie composition.
    //
    // The buffer is the output of LottieGen -Language flatbuffer, or equivalently of
    // CompositionSerializer. It is treated as untrusted: it is verified before it is
    // read, and every index in it is range checked, so a malformed buffer produces an
    // exception rather than undefined behaviour.
    //
    // The returned visual is the root of a tree that is already animated and ready to
    // be attached to a target with SetElementChildVisual or as a child of another
    // visual. The animations are bound to the "Progress" property of the property set
    // returned by ProgressPropertySet, so playback is driven by animating that single
    // property.
    //
    // Visual is sufficient for parenting the root and driving its progress.
    //
    // Throws winrt::hresult_error with E_INVALIDARG if the buffer is not a well formed
    // composition, and E_NOTIMPL if it requires a feature this build does not have.
    Visual LoadComposition(
        Compositor const& compositor,
        std::span<std::byte const> buffer);

    // The property set that drives the animation. Animating its "Progress" scalar from
    // 0 to 1 plays the animation once.
    //
    // Returns the root Visual's property set.
    CompositionPropertySet ProgressPropertySet(Visual const& root);
}
