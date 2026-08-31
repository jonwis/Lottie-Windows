#pragma once

#include "MainPage.g.h"

namespace winrt::LottieCppUwpTest::implementation
{
    struct MainPage : MainPageT<MainPage>
    {
        MainPage()
        {
            // Xaml objects should not call InitializeComponent during construction.
            // See https://github.com/microsoft/cppwinrt/tree/master/nuget#initializecomponent
        }

        int32_t MyProperty();
        void MyProperty(int32_t value);

        void ClickHandler(Windows::Foundation::IInspectable const& sender, Windows::UI::Xaml::RoutedEventArgs const& args);

        Windows::Foundation::Collections::IVector<Windows::Foundation::IInspectable> BeforeList();
        Windows::Foundation::Collections::IVector<Windows::Foundation::IInspectable> AfterList();
    };
}

namespace winrt::LottieCppUwpTest::factory_implementation
{
    struct MainPage : MainPageT<MainPage, implementation::MainPage>
    {
    };
}
