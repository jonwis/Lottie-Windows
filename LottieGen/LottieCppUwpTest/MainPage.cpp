#include "pch.h"
#include "MainPage.h"
#include "MainPage.g.cpp"

using namespace winrt;
using namespace Windows::UI::Xaml;

namespace winrt::LottieCppUwpTest::implementation
{
    int32_t MainPage::MyProperty()
    {
        throw hresult_not_implemented();
    }

    void MainPage::MyProperty(int32_t /* value */)
    {
        throw hresult_not_implemented();
    }

    void MainPage::ClickHandler(IInspectable const&, RoutedEventArgs const&)
    {
        myButton().Content(box_value(L"Clicked"));
    }

    Windows::Foundation::Collections::IVector<Windows::Foundation::IInspectable> MainPage::BeforeList()
    {
        return winrt::multi_threaded_vector( std::vector<Windows::Foundation::IInspectable>{
            Before::LottieLogo1(),
        });
    }

    Windows::Foundation::Collections::IVector<Windows::Foundation::IInspectable> MainPage::AfterList()
    {
        return winrt::multi_threaded_vector(std::vector<Windows::Foundation::IInspectable>{
            After::LottieLogo1(),
        });
    }

}
