using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StalTool.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // ─── Данные пользователя ──────────────────────────────────────────────────
    [ObservableProperty] private string _username = "BrykLeeN";
    [ObservableProperty] private string _userInitial = "B";
    [ObservableProperty] private string _userTier = "Олигарх";

    // ─── Навигация ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _currentPageTitle = "Аукцион";
    [ObservableProperty] private string _currentPageSubtitle = "— аналитика торгов";

    // Сюда будет подставляться активная страница (ViewModel страницы)
    // Пока null — позже заменим на реальные страницы
    [ObservableProperty] private ViewModelBase? _currentPage;

    // ─── Команды навигации ────────────────────────────────────────────────────
    [RelayCommand]
    private void NavigateToAuction()
    {
        CurrentPageTitle = "Аукцион";
        CurrentPageSubtitle = "— аналитика торгов";
        // CurrentPage = new AuctionViewModel(); // раскомментируешь когда создашь
    }

    [RelayCommand]
    private void NavigateToArsen()
    {
        CurrentPageTitle = "Арсен";
        CurrentPageSubtitle = "— калькулятор крафта";
        // CurrentPage = new ArsenViewModel();
    }

    [RelayCommand]
    private void NavigateToHistory()
    {
        CurrentPageTitle = "История";
        CurrentPageSubtitle = "— торговые записи";
        // CurrentPage = new HistoryViewModel();
    }

    [RelayCommand]
    private void NavigateToRadar()
    {
        CurrentPageTitle = "Радар";
        CurrentPageSubtitle = "— мониторинг цен";
        // CurrentPage = new RadarViewModel();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentPageTitle = "Настройки";
        CurrentPageSubtitle = "";
        // CurrentPage = new SettingsViewModel();
    }

    // ─── Команды управления окном ─────────────────────────────────────────────
    [RelayCommand]
    private void Minimize()
    {
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.MainWindow!.WindowState = WindowState.Minimized;
        }
    }

    [RelayCommand]
    private void Close()
    {
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.MainWindow!.Close();
        }
    }
}
