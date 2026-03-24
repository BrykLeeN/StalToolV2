using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using StalTool.ViewModels;

namespace StalTool.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        var titleBar = this.FindControl<Border>("TitleBar");
        if (titleBar != null)
            titleBar.PointerPressed += (s, ev) =>
            {
                if (ev.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    BeginMoveDrag(ev);
            };

    }

    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    // ─── Навигация по сайдбару ───────────────────────────────────────────────
    private void NavBtn_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var tag = (sender as Control)?.Tag?.ToString();
        switch (tag)
        {
            case "Auction":  Vm?.NavigateToAuctionCommand.Execute(null);  break;
            case "Arsen":    Vm?.NavigateToArsenCommand.Execute(null);    break;
            case "History":  Vm?.NavigateToHistoryCommand.Execute(null);  break;
            case "Radar":    Vm?.NavigateToRadarCommand.Execute(null);    break;
            case "Settings": Vm?.NavigateToSettingsCommand.Execute(null); break;
        }
    }

    // ─── Профиль ─────────────────────────────────────────────────────────────
    private void UserMenu_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (Vm == null) return;
        if (Vm.IsNotifOpen) Vm.IsNotifOpen = false;
        Vm.ToggleUserMenuCommand.Execute(null);
        e.Handled = true;
    }
    
    // ─── Уведомления ─────────────────────────────────────────────────────────
    private void NotifBtn_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (Vm == null) return;
        if (Vm.IsUserMenuOpen) Vm.IsUserMenuOpen = false;
        Vm.ToggleNotifCommand.Execute(null);
        e.Handled = true;
    }

    // ─── Вкладки ─────────────────────────────────────────────────────────────
    private void SubTab_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var tag = (sender as Control)?.Tag?.ToString();
        switch (tag)
        {
            case "1": Vm?.NavigateToSubTab1Command.Execute(null); break;
            case "2": Vm?.NavigateToSubTab2Command.Execute(null); break;
            case "3": Vm?.NavigateToSubTab3Command.Execute(null); break;
        }
    }

    // ─── Кнопки окна ─────────────────────────────────────────────────────────
    private void MinimizeBtn_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            Vm?.MinimizeCommand.Execute(null);
    }

    private void CloseBtn_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            Vm?.CloseCommand.Execute(null);
    }
}
