// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Sample tool: loads a serialized Lottie composition (a .lcomp FlatBuffer produced
// by LottieGen -Language flatbuffer or CompositionSerializer) through LottieRuntime,
// then prints the composition object hierarchy of the resulting visual tree.
//
// Usage: LottieRuntimeDump.exe <path-to.lcomp>

#include <unknwn.h>

#include <LottieRuntime.h>

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.UI.Composition.h>

#include <fstream>
#include <iostream>
#include <string>
#include <vector>

using namespace winrt;
using namespace winrt::Windows::UI::Composition;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Foundation::Collections;

namespace
{
    std::vector<std::byte> ReadFileBytes(wchar_t const* path)
    {
        std::ifstream file(path, std::ios::binary | std::ios::ate);
        if (!file)
        {
            throw std::runtime_error("Could not open input file.");
        }

        auto const size = static_cast<size_t>(file.tellg());
        std::vector<std::byte> result(size);
        file.seekg(0);
        file.read(reinterpret_cast<char*>(result.data()), static_cast<std::streamsize>(size));
        return result;
    }

    void PrintLine(int depth, std::wstring_view text)
    {
        std::wstring indent(static_cast<size_t>(depth) * 2, L' ');
        std::wcout << indent << text << L"\n";
    }

    hstring RuntimeClassName(IInspectable const& node)
    {
        if (node == nullptr)
        {
            return L"(null)";
        }

        return winrt::get_class_name(node);
    }

    hstring CommentOf(CompositionObject const& node)
    {
        auto comment = node.Comment();
        return comment.empty() ? L"" : (L" \"" + comment + L"\"");
    }

    void DumpBrush(CompositionBrush const& brush, int depth);
    void DumpVisual(Visual const& visual, int depth);
    void DumpShape(CompositionShape const& shape, int depth);

    void DumpShape(CompositionShape const& shape, int depth)
    {
        if (shape == nullptr)
        {
            return;
        }

        PrintLine(depth, RuntimeClassName(shape) + CommentOf(shape));

        if (auto containerShape = shape.try_as<CompositionContainerShape>())
        {
            for (auto const& child : containerShape.Shapes())
            {
                DumpShape(child, depth + 1);
            }
        }

        if (auto spriteShape = shape.try_as<CompositionSpriteShape>())
        {
            if (auto fill = spriteShape.FillBrush())
            {
                PrintLine(depth + 1, L"FillBrush:");
                DumpBrush(fill, depth + 2);
            }

            if (auto stroke = spriteShape.StrokeBrush())
            {
                PrintLine(depth + 1, L"StrokeBrush:");
                DumpBrush(stroke, depth + 2);
            }
        }
    }

    void DumpBrush(CompositionBrush const& brush, int depth)
    {
        if (brush == nullptr)
        {
            return;
        }

        PrintLine(depth, RuntimeClassName(brush) + CommentOf(brush));

        if (auto linearGradient = brush.try_as<CompositionLinearGradientBrush>())
        {
            for (auto const& stop : linearGradient.ColorStops())
            {
                PrintLine(depth + 1, L"ColorStop " + RuntimeClassName(stop));
            }
        }
    }

    void DumpVisual(Visual const& visual, int depth)
    {
        if (visual == nullptr)
        {
            return;
        }

        PrintLine(depth, RuntimeClassName(visual) + CommentOf(visual));

        if (auto spriteVisual = visual.try_as<SpriteVisual>())
        {
            if (auto brush = spriteVisual.Brush())
            {
                PrintLine(depth + 1, L"Brush:");
                DumpBrush(brush, depth + 2);
            }

            if (auto shapeVisual = visual.try_as<ShapeVisual>())
            {
                for (auto const& shape : shapeVisual.Shapes())
                {
                    DumpShape(shape, depth + 1);
                }
            }
        }
        else if (auto shapeVisual = visual.try_as<ShapeVisual>())
        {
            for (auto const& shape : shapeVisual.Shapes())
            {
                DumpShape(shape, depth + 1);
            }
        }

        if (auto containerVisual = visual.try_as<ContainerVisual>())
        {
            for (auto const& child : containerVisual.Children())
            {
                DumpVisual(child, depth + 1);
            }
        }
    }
}

int wmain(int argc, wchar_t* argv[])
{
    winrt::init_apartment();

    if (argc != 2)
    {
        std::wcerr << L"Usage: LottieRuntimeDump.exe <path-to.lcomp>\n";
        return 1;
    }

    try
    {
        auto bytes = ReadFileBytes(argv[1]);

        Compositor compositor;
        auto root = CommunityToolkit::WinUI::Lottie::LoadComposition(
            compositor,
            std::span<std::byte const>(bytes.data(), bytes.size()));

        std::wcout << L"Composition object hierarchy:\n";
        DumpVisual(root, 0);
    }
    catch (winrt::hresult_error const& e)
    {
        std::wcerr << L"Failed to load composition: " << e.message().c_str() << L"\n";
        return 1;
    }
    catch (std::exception const& e)
    {
        std::cerr << "Failed: " << e.what() << "\n";
        return 1;
    }

    return 0;
}
