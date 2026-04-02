using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StalTool.Views.Pages;

public partial class AuctionCalculatorPage : UserControl
{
    public AuctionCalculatorPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
