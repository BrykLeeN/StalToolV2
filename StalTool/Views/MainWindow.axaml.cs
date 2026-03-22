using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using StalTool.ViewModels;

namespace StalTool.Views;

public partial class MainWindow : Window
{
    private Popup? _userPopup;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Перетаскивание окна
        var titleBar = this.FindControl<Border>("TitleBar");
        if (titleBar != null)
            titleBar.PointerPressed += (s, ev) =>
            {
                if (ev.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    BeginMoveDrag(ev);
            };

        _userPopup ??= this.FindControl<Popup>("UserPopup");
    }

    private MainWindowViewModel? VM => DataContext as MainWindowViewModel;

    // Навигация по сайдбару
    private void NavBtn_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var tag = (sender as Control)?.Tag?.ToString();
        switch (tag)
        {
            case "Auction":  VM?.NavigateToAuctionCommand.Execute(null);  break;
            case "Arsen":    VM?.NavigateToArsenCommand.Execute(null);    break;
            case "History":  VM?.NavigateToHistoryCommand.Execute(null);  break;
            case "Radar":    VM?.NavigateToRadarCommand.Execute(null);    break;
            case "Settings": VM?.NavigateToSettingsCommand.Execute(null); break;
        }
    }

    // Профиль — открыть/закрыть popup
    private void UserMenu_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (VM == null) return;

        _userPopup ??= this.FindControl<Popup>("UserPopup");
        var popupIsOpen = _userPopup?.IsOpen ?? VM.IsUserMenuOpen;
        VM.IsUserMenuOpen = !popupIsOpen;

        e.Handled = true;
    }

    private void UserPopup_Opened(object? sender, System.EventArgs e)
    {
        if (VM != null)
            VM.IsUserMenuOpen = true;
    }

    private void UserPopup_Closed(object? sender, System.EventArgs e)
    {
        if (VM != null)
            VM.IsUserMenuOpen = false;
    }

    // Вкладки
    private void SubTab_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var tag = (sender as Control)?.Tag?.ToString();
        switch (tag)
        {
            case "1": VM?.NavigateToSubTab1Command.Execute(null); break;
            case "2": VM?.NavigateToSubTab2Command.Execute(null); break;
            case "3": VM?.NavigateToSubTab3Command.Execute(null); break;
        }
    }

    // Кнопки окна
    private void MinimizeBtn_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            VM?.MinimizeCommand.Execute(null);
    }

    private void CloseBtn_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            VM?.CloseCommand.Execute(null);
    }
}
