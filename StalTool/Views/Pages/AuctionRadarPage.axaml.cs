using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StalTool.Views.Pages;

public partial class AuctionRadarPage : UserControl
{
    public AuctionRadarPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
