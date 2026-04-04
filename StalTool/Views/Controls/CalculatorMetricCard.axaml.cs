using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace StalTool.Views.Controls;

public partial class CalculatorMetricCard : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<CalculatorMetricCard, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<CalculatorMetricCard, string>(nameof(Value), "—");

    public static readonly StyledProperty<IBrush> ValueBrushProperty =
        AvaloniaProperty.Register<CalculatorMetricCard, IBrush>(nameof(ValueBrush), Brushes.White);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public IBrush ValueBrush
    {
        get => GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    public CalculatorMetricCard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
