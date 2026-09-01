// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <unknwn.h>
#include <Windows.h>

// A classic (non-WinRT) COM entry point for LoadComposition, for callers that only
// have a byte buffer and a COM activation story, rather than a Compositor and a
// C++/WinRT toolchain. It is not registered with the system: it is meant to be
// activated registration-free, through an application manifest that names
// LottieRuntime.dll and this CLSID (see the "Isolated applications and side-by-side
// assemblies" COM activation model).
//
// The object it produces is the root Visual of the composition, queried for
// whatever interface the caller asked for through riid - typically IInspectable,
// ABI::Windows::UI::Composition::IVisual, or IUnknown.
MIDL_INTERFACE("3874B71B-05E6-42A9-B838-1AE7F0BA4E1C")
ILottieCompositionLoader : public IUnknown
{
public:
    // Parses buffer (the output of LottieGen -Language flatbuffer, or equivalently
    // CompositionSerializer) and produces the root Visual of the resulting tree.
    //
    // The buffer is treated as untrusted: it is verified before it is read, so a
    // malformed buffer produces a failure HRESULT rather than undefined behaviour.
    STDMETHOD(LoadComposition)(UINT32 length, BYTE const* buffer, REFIID riid, void** result) = 0;
};

// A caller activating this class registration-free never links against
// LottieRuntime.dll's import library (there is no exported symbol to link
// against for an in-proc COM server resolved purely through an activation
// context), so the CLSID is a compile-time constant here rather than an
// extern declaration.
constexpr CLSID CLSID_LottieCompositionLoader = {
    0x9f7b7089, 0xf512, 0x4bbc, { 0x81, 0xfe, 0x4f, 0xf1, 0x2b, 0x4d, 0xa1, 0x59 } };
