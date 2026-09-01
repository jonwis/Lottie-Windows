// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The classic COM entry point declared in LottieRuntimeCom.h. This is the only
// translation unit that needs to know both LoadComposition and classic COM, so it
// stays out of LottieRuntime.cpp.

#include <unknwn.h>

#include "LottieRuntimeCom.h"
#include "LottieRuntime.h"

#include <DispatcherQueue.h>

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.System.h>
#include <winrt/Windows.UI.Composition.h>

using namespace winrt::Windows::UI::Composition;


namespace
{
    // The loader creates its Compositor on the first load. Desktop threads need a
    // DispatcherQueue before Windows.UI.Composition can activate a Compositor.
    struct LottieCompositionLoader : winrt::implements<LottieCompositionLoader, ILottieCompositionLoader>
    {
        STDMETHODIMP LoadComposition(UINT32 length, BYTE const* buffer, REFIID riid, void** result) noexcept override
        {
            if (result == nullptr)
            {
                return E_POINTER;
            }

            *result = nullptr;

            if (buffer == nullptr && length != 0)
            {
                return E_INVALIDARG;
            }

            try
            {
                if (!m_compositor)
                {
                    if (!winrt::Windows::System::DispatcherQueue::GetForCurrentThread())
                    {
                        DispatcherQueueOptions const options{
                            sizeof(DispatcherQueueOptions),
                            DQTYPE_THREAD_CURRENT,
                            DQTAT_COM_NONE,
                        };

                        winrt::check_hresult(CreateDispatcherQueueController(
                            options,
                            reinterpret_cast<PDISPATCHERQUEUECONTROLLER*>(winrt::put_abi(m_dispatcherQueueController))));
                    }

                    m_compositor = Compositor();
                }

                auto const span = std::span(reinterpret_cast<std::byte const*>(buffer), length);
                auto const visual = CommunityToolkit::WinUI::Lottie::LoadComposition(m_compositor, span);
                return visual.as<::IUnknown>()->QueryInterface(riid, result);
            }
            catch (...)
            {
                return winrt::to_hresult();
            }
        }

    private:
        winrt::Windows::System::DispatcherQueueController m_dispatcherQueueController{ nullptr };
        Compositor m_compositor{ nullptr };
    };

    // A minimal class factory: this DLL only ever creates LottieCompositionLoader,
    // so there is no per-CLSID dispatch to do.
    struct ClassFactory : winrt::implements<ClassFactory, IClassFactory>
    {
        STDMETHODIMP CreateInstance(::IUnknown* outer, REFIID riid, void** result) noexcept override
        {
            if (result == nullptr)
            {
                return E_POINTER;
            }

            *result = nullptr;

            if (outer != nullptr)
            {
                return CLASS_E_NOAGGREGATION;
            }

            try
            {
                return winrt::make<LottieCompositionLoader>()->QueryInterface(riid, result);
            }
            catch (...)
            {
                return winrt::to_hresult();
            }
        }

        STDMETHODIMP LockServer(BOOL) noexcept override
        {
            return S_OK;
        }
    };
}

extern "C" HRESULT __stdcall DllGetClassObject(REFCLSID rclsid, REFIID riid, void** result)
{
    if (result == nullptr)
    {
        return E_POINTER;
    }

    *result = nullptr;

    if (rclsid != CLSID_LottieCompositionLoader)
    {
        return CLASS_E_CLASSNOTAVAILABLE;
    }

    try
    {
        return winrt::make<ClassFactory>()->QueryInterface(riid, result);
    }
    catch (...)
    {
        return winrt::to_hresult();
    }
}

extern "C" HRESULT __stdcall DllCanUnloadNow()
{
    // The loader has no process-wide state, so it is always safe to unload; the
    // reference-counted objects it hands out keep the DLL alive on their own.
    return S_OK;
}
