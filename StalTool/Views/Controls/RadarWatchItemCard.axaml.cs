using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StalTool.Views.Controls;

public partial class RadarWatchItemCard : UserControl
{
    public RadarWatchItemCard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
