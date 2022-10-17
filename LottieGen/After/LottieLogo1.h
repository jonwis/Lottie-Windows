﻿#pragma once
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//       LottieGen version:
//           7.1.2-build.38+gcdb118a09a
//       
//       Command:
//           LottieGen -Language Cppwinrt -MinimumUapVersion 12 -Namespace After -RootNamespace After -WinUIVersion 2.4 -InputFile LottieLogo1.json
//       
//       Input file:
//           LottieLogo1.json (190271 bytes created 17:20-07:00 Oct 10 2022)
//       
//       LottieGen source:
//           http://aka.ms/Lottie
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#include "LottieLogo1.g.h"

namespace winrt::After
{
    // Frame rate:  30 fps
    // Frame count: 179
    // Duration:    5966.7 mS
    namespace implementation
    {
        class LottieLogo1
            : public LottieLogo1T<LottieLogo1>
        {
        public:
            // Animation duration: 5.967 seconds.
            static constexpr int64_t c_durationTicks{ 59666666L };

            winrt::Microsoft::UI::Xaml::Controls::IAnimatedVisual TryCreateAnimatedVisual(
                winrt::Windows::UI::Composition::Compositor const& compositor);

            winrt::Microsoft::UI::Xaml::Controls::IAnimatedVisual TryCreateAnimatedVisual(
                winrt::Windows::UI::Composition::Compositor const& compositor,
                winrt::Windows::Foundation::IInspectable& diagnostics);

            // Gets the number of frames in the animation.
            double FrameCount();

            // Gets the framerate of the animation.
            double Framerate();

            // Gets the duration of the animation.
            winrt::Windows::Foundation::TimeSpan Duration();

            // Converts a zero-based frame number to the corresponding progress value denoting the
            // start of the frame.
            double FrameToProgress(double frameNumber);

            // Returns a map from marker names to corresponding progress values.
            winrt::Windows::Foundation::Collections::IMapView<hstring, double> Markers();

            // Sets the color property with the given name, or does nothing if no such property
            // exists.
            void SetColorProperty(hstring const& propertyName, winrt::Windows::UI::Color value);

            // Sets the scalar property with the given name, or does nothing if no such property
            // exists.
            void SetScalarProperty(hstring const& propertyName, double value);
        };
    }

    namespace factory_implementation
    {
        struct LottieLogo1 : LottieLogo1T<LottieLogo1, implementation::LottieLogo1>
        {
        };
    }
}
