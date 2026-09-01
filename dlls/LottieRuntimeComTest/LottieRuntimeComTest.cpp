// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Exercises the classic COM entry point declared in LottieRuntimeCom.h.
//
// LottieRuntime.dll is never registered with the system: its CLSID is only ever
// known through LottieRuntime.manifest, its own private-assembly manifest, copied
// next to this exe at build time. This test builds an activation context directly
// out of that manifest - the same shape a real isolated application uses when it
// carries a private assembly instead of registering a DLL - and activates it for
// the duration of the CoCreateInstance call, which is the reason CoCreateInstance
// resolves the CLSID without anything ever touching HKEY_CLASSES_ROOT.

#include <unknwn.h>

#include <Windows.h>
#include <combaseapi.h>
#include <shlwapi.h>

#include <cstdio>
#include <span>
#include <stdexcept>
#include <string>
#include <vector>

#include <winrt/base.h>

#include "LottieRuntimeCom.h"

namespace
{
    std::vector<std::byte> ReadCompositionFile(wchar_t const* path)
    {
        HANDLE file = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (file == INVALID_HANDLE_VALUE)
        {
            throw std::runtime_error("Could not open the composition file.");
        }

        LARGE_INTEGER size{};
        GetFileSizeEx(file, &size);

        std::vector<std::byte> buffer(static_cast<size_t>(size.QuadPart));
        DWORD read = 0;
        BOOL const ok = ::ReadFile(file, buffer.data(), static_cast<DWORD>(buffer.size()), &read, nullptr);
        CloseHandle(file);

        if (!ok || read != buffer.size())
        {
            throw std::runtime_error("Could not read the composition file.");
        }

        return buffer;
    }
}

int wmain(int argc, wchar_t* argv[])
{
    if (argc != 2)
    {
        fwprintf(stderr, L"Usage: LottieRuntimeComTest <path-to-flatbuffer-composition>\n");
        return 1;
    }

    try
    {
        winrt::check_hresult(CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED));

        // The manifest is a private assembly for this exe: it declares the CLSID
        // directly rather than through a level of indirection, so it is loaded
        // straight into the activation context as the primary manifest, with no
        // separate application manifest and no HKEY_CLASSES_ROOT registration.
        wchar_t moduleDir[MAX_PATH]{};
        GetModuleFileNameW(nullptr, moduleDir, MAX_PATH);
        PathRemoveFileSpecW(moduleDir);

        std::wstring const manifestPath = std::wstring(moduleDir) + L"\\LottieRuntime.manifest";

        ACTCTXW actCtx{ sizeof(ACTCTXW) };
        actCtx.lpSource = manifestPath.c_str();
        actCtx.lpAssemblyDirectory = moduleDir;
        actCtx.dwFlags = ACTCTX_FLAG_ASSEMBLY_DIRECTORY_VALID;

        HANDLE const hActCtx = CreateActCtxW(&actCtx);
        if (hActCtx == INVALID_HANDLE_VALUE)
        {
            fwprintf(stderr, L"CreateActCtx failed: %lu (manifest: %s, dir: %s)\n", GetLastError(), manifestPath.c_str(), moduleDir);
            return 1;
        }

        ULONG_PTR cookie = 0;
        ActivateActCtx(hActCtx, &cookie);

        winrt::com_ptr<ILottieCompositionLoader> loader;
        HRESULT const hr = CoCreateInstance(
            CLSID_LottieCompositionLoader,
            nullptr,
            CLSCTX_INPROC_SERVER,
            __uuidof(ILottieCompositionLoader),
            loader.put_void());

        DeactivateActCtx(0, cookie);
        ReleaseActCtx(hActCtx);

        if (FAILED(hr))
        {
            fwprintf(stderr, L"CoCreateInstance failed (registration-free activation did not resolve the CLSID): 0x%08X (last error %lu)\n", hr, GetLastError());
            return 1;
        }

        auto const buffer = ReadCompositionFile(argv[1]);

        winrt::com_ptr<::IUnknown> root;
        HRESULT const loadHr = loader->LoadComposition(
            static_cast<UINT32>(buffer.size()),
            reinterpret_cast<BYTE const*>(buffer.data()),
            __uuidof(::IUnknown),
            root.put_void());

        if (FAILED(loadHr))
        {
            fwprintf(stderr, L"LoadComposition failed: 0x%08X\n", loadHr);
            return 1;
        }

        wprintf(L"Registration-free activation succeeded; loaded a composition with a root object at %p.\n", root.get());
        return 0;
    }
    catch (winrt::hresult_error const& e)
    {
        fwprintf(stderr, L"%s\n", e.message().c_str());
        return 1;
    }
    catch (std::exception const& e)
    {
        fprintf(stderr, "%s\n", e.what());
        return 1;
    }
}
