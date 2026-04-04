using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using StalTool.Models;
using StalTool.ViewModels.Auction.Sections;

namespace StalTool.Views.Pages;

public partial class AuctionPriceChartPage : UserControl
{
    public AuctionPriceChartPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Visual source || IsInteractiveElement(source))
            return;

        if (sender is InputElement inputElement)
            inputElement.Focus();
    }

    private void OnCategoryItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsMiddleButtonPressed)
            return;

        if (DataContext is not AuctionPriceChartViewModel viewModel)
            return;

        if (sender is not Control { DataContext: AuctionCatalogItem item })
            return;

        if (viewModel.CollapseCategoryByItemCommand.CanExecute(item))
            viewModel.CollapseCategoryByItemCommand.Execute(item);

        e.Handled = true;
    }

    private static bool IsInteractiveElement(Visual source)
    {
        return source.GetSelfAndVisualAncestors().Any(v =>
            v is TextBox or Button or ToggleButton or ScrollBar or Slider or Thumb or DatePicker);
    }
}
