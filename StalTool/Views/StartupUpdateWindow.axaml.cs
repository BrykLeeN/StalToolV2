using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StalTool.Views;

public partial class StartupUpdateWindow : Window
{
    public StartupUpdateWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
