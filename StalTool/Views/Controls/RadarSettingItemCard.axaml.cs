using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StalTool.Views.Controls;

public partial class RadarSettingItemCard : UserControl
{
    public RadarSettingItemCard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
