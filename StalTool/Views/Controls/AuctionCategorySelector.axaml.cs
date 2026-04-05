using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using StalTool.Models;

namespace StalTool.Views.Controls;

public partial class AuctionCategorySelector : UserControl
{
    public static readonly StyledProperty<ObservableCollection<AuctionCategoryGroup>?> CategoriesProperty =
        AvaloniaProperty.Register<AuctionCategorySelector, ObservableCollection<AuctionCategoryGroup>?>(nameof(Categories));

    public static readonly StyledProperty<string> SearchTextProperty =
        AvaloniaProperty.Register<AuctionCategorySelector, string>(nameof(SearchText), string.Empty);

    public static readonly StyledProperty<System.Windows.Input.ICommand?> ClearSearchCommandProperty =
        AvaloniaProperty.Register<AuctionCategorySelector, System.Windows.Input.ICommand?>(nameof(ClearSearchCommand));

    public static readonly StyledProperty<System.Windows.Input.ICommand?> ToggleOrSelectItemCommandProperty =
        AvaloniaProperty.Register<AuctionCategorySelector, System.Windows.Input.ICommand?>(nameof(ToggleOrSelectItemCommand));

    public static readonly StyledProperty<System.Windows.Input.ICommand?> SelectItemCommandProperty =
        AvaloniaProperty.Register<AuctionCategorySelector, System.Windows.Input.ICommand?>(nameof(SelectItemCommand));

    public static readonly StyledProperty<System.Windows.Input.ICommand?> CollapseCategoryByItemCommandProperty =
        AvaloniaProperty.Register<AuctionCategorySelector, System.Windows.Input.ICommand?>(nameof(CollapseCategoryByItemCommand));

    public AuctionCategorySelector()
    {
        InitializeComponent();
    }

    public ObservableCollection<AuctionCategoryGroup>? Categories
    {
        get => GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public string SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public System.Windows.Input.ICommand? ClearSearchCommand
    {
        get => GetValue(ClearSearchCommandProperty);
        set => SetValue(ClearSearchCommandProperty, value);
    }

    public System.Windows.Input.ICommand? ToggleOrSelectItemCommand
    {
        get => GetValue(ToggleOrSelectItemCommandProperty);
        set => SetValue(ToggleOrSelectItemCommandProperty, value);
    }

    public System.Windows.Input.ICommand? SelectItemCommand
    {
        get => GetValue(SelectItemCommandProperty);
        set => SetValue(SelectItemCommandProperty, value);
    }

    public System.Windows.Input.ICommand? CollapseCategoryByItemCommand
    {
        get => GetValue(CollapseCategoryByItemCommandProperty);
        set => SetValue(CollapseCategoryByItemCommandProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCategoryItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsMiddleButtonPressed)
            return;

        if (sender is not Control { DataContext: AuctionCatalogItem item })
            return;

        if (CollapseCategoryByItemCommand is null || !CollapseCategoryByItemCommand.CanExecute(item))
            return;

        CollapseCategoryByItemCommand.Execute(item);
        e.Handled = true;
    }
}
