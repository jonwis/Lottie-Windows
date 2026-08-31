// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <cstddef>
#include <span>

#include <winrt/Windows.UI.Composition.h>

// The component is usable either as a DLL or compiled straight into an application.
// Define LOTTIERUNTIME_STATIC to build it in, and LOTTIERUNTIME_EXPORTS when building
// the DLL itself.
#if defined(LOTTIERUNTIME_STATIC)
#define LOTTIERUNTIME_API
#elif defined(LOTTIERUNTIME_EXPORTS)
#define LOTTIERUNTIME_API __declspec(dllexport)
#else
#define LOTTIERUNTIME_API __declspec(dllimport)
#endif

namespace CommunityToolkit::WinUI::Lottie
{
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
    // The result is the least derived type that describes the root, because a caller
    // that only parents the tree and drives its progress never needs anything more.
    //
    // Throws winrt::hresult_error with E_INVALIDARG if the buffer is not a well formed
    // composition, and E_NOTIMPL if it requires a feature this build does not have.
    LOTTIERUNTIME_API winrt::Windows::UI::Composition::Visual LoadComposition(
        winrt::Windows::UI::Composition::Compositor const& compositor,
        std::span<std::byte const> buffer);

    // The property set that drives the animation. Animating its "Progress" scalar from
    // 0 to 1 plays the animation once.
    //
    // This is the property set of the visual returned by LoadComposition, so it is only
    // a convenience; it exists so that callers do not have to know that detail.
    LOTTIERUNTIME_API winrt::Windows::UI::Composition::CompositionPropertySet ProgressPropertySet(
        winrt::Windows::UI::Composition::Visual const& root);
}
