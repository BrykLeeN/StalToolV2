using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StalTool.Models;
using StalTool.ViewModels.Auction;
using StalTool.ViewModels.Base;

namespace StalTool.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly NavigationService _navigationService;

    [ObservableProperty] private string _username = "BrykLeeN";
    [ObservableProperty] private string _userInitial = "B";
    [ObservableProperty] private string _userTier = "Олигарх";
    [ObservableProperty] private string _userTierFull = "Олигарх Зоны • до 15.09.2026";

    [ObservableProperty] private Color _tierAccentColor = Color.Parse("#FFB020");
    [ObservableProperty] private Color _tierAccentColorEnd = Color.Parse("#FF8C00");
    [ObservableProperty] private Color _tierBgColor = Color.Parse("#1A1200");

    [ObservableProperty] private bool _isUserMenuOpen = false;
    [ObservableProperty] private string _userMenuArrow = "▾";

    [ObservableProperty] private bool _isNotifOpen = false;

    public ObservableCollection<RadarNotification> RadarNotifications { get; } = new();

    public bool HasNotifications => RadarNotifications.Count > 0;
    public bool HasUnreadNotifications => RadarNotifications.Any(n => n.IsUnread);
    public int UnreadCount => RadarNotifications.Count(n => n.IsUnread);

    [ObservableProperty] private string _currentPageTitle = "Аукцион";
    [ObservableProperty] private string _currentPageSubtitle = "— аналитика торгов";
    [ObservableProperty] private ViewModelBase? _currentPage;

    [ObservableProperty] private bool _isAuctionActive = true;
    [ObservableProperty] private bool _isArsenActive = false;
    [ObservableProperty] private bool _isHistoryActive = false;
    [ObservableProperty] private bool _isRadarActive = false;
    [ObservableProperty] private bool _isSettingsActive = false;

    [ObservableProperty] private bool _hasSubTabs = true;
    [ObservableProperty] private bool _hasSubTab3 = true;
    [ObservableProperty] private string _subTab1Title = "График цен";
    [ObservableProperty] private string _subTab2Title = "История лотов";
    [ObservableProperty] private string _subTab3Title = "Калькулятор";
    [ObservableProperty] private bool _isSubTab1Active = true;
    [ObservableProperty] private bool _isSubTab2Active = false;
    [ObservableProperty] private bool _isSubTab3Active = false;

    public MainWindowViewModel()
    {
        _navigationService = new NavigationService();
        _navigationService.CurrentPageChanged += page => CurrentPage = page;

        ApplyTierColors(UserTier);
        AddTestNotifications();
        NavigateToAuction();
    }

    partial void OnIsUserMenuOpenChanged(bool value)
    {
        UserMenuArrow = value ? "▴" : "▾";
    }

    partial void OnIsNotifOpenChanged(bool value)
    {
        if (value)
        {
            OnPropertyChanged(nameof(UnreadCount));
            OnPropertyChanged(nameof(HasUnreadNotifications));
        }
    }

    [RelayCommand]
    private void ToggleUserMenu()
    {
        IsUserMenuOpen = !IsUserMenuOpen;
        if (IsUserMenuOpen) IsNotifOpen = false;
    }

    [RelayCommand]
    private void Logout()
    {
        IsUserMenuOpen = false;
    }

    [RelayCommand]
    private void ToggleNotif()
    {
        IsNotifOpen = !IsNotifOpen;
        if (IsNotifOpen) IsUserMenuOpen = false;
    }

    [RelayCommand]
    private void MarkAllRead()
    {
        foreach (var n in RadarNotifications)
            n.IsUnread = false;

        OnPropertyChanged(nameof(HasUnreadNotifications));
        OnPropertyChanged(nameof(UnreadCount));
    }

    [RelayCommand]
    private void OpenRadar()
    {
        IsNotifOpen = false;
        NavigateToRadar();
    }

    public void AddNotification(RadarNotification notification)
    {
        RadarNotifications.Insert(0, notification);
        OnPropertyChanged(nameof(HasNotifications));
        OnPropertyChanged(nameof(HasUnreadNotifications));
        OnPropertyChanged(nameof(UnreadCount));
    }

    public void AddTestNotifications()
    {
        AddNotification(new RadarNotification
        {
            ItemName = "АК-74М (Коллекционный)",
            Description = "Цена упала ниже 5 000 ₽",
            TimeAgo = "2 мин назад",
            PriceText = "4 820 ₽",
            PriceColor = Color.Parse("#44FF88"),
            IsUnread = true,
        });

        AddNotification(new RadarNotification
        {
            ItemName = "Ящик «Страйкер»",
            Description = "Цена достигла цели 900 ₽",
            TimeAgo = "15 мин назад",
            PriceText = "890 ₽",
            PriceColor = Color.Parse("#44FF88"),
            IsUnread = true,
        });

        AddNotification(new RadarNotification
        {
            ItemName = "СВДС (Состаренный)",
            Description = "Цена выросла выше 14 000 ₽",
            TimeAgo = "1 час назад",
            PriceText = "14 100 ₽",
            PriceColor = Color.Parse("#FF5566"),
            IsUnread = false,
        });
    }

    private void ResetSidebarActive()
    {
        IsAuctionActive = false;
        IsArsenActive = false;
        IsHistoryActive = false;
        IsRadarActive = false;
        IsSettingsActive = false;
        IsUserMenuOpen = false;
        IsNotifOpen = false;
    }

    private void ResetSubTabs()
    {
        IsSubTab1Active = false;
        IsSubTab2Active = false;
        IsSubTab3Active = false;
    }

    [RelayCommand]
    private void NavigateToAuction()
    {
        ResetSidebarActive();
        IsAuctionActive = true;
        CurrentPageTitle = "Аукцион";
        CurrentPageSubtitle = "— аналитика торгов";
        HasSubTabs = true;
        HasSubTab3 = true;
        SubTab1Title = "График цен";
        SubTab2Title = "История лотов";
        SubTab3Title = "Калькулятор";
        ResetSubTabs();
        IsSubTab1Active = true;

        _navigationService.Navigate(new AuctionViewModel());
    }

    [RelayCommand]
    private void NavigateToArsen()
    {
        ResetSidebarActive();
        IsArsenActive = true;
        CurrentPageTitle = "Арсен";
        CurrentPageSubtitle = "— калькулятор крафта";
        HasSubTabs = true;
        HasSubTab3 = false;
        SubTab1Title = "Калькулятор";
        SubTab2Title = "История крафта";
        ResetSubTabs();
        IsSubTab1Active = true;

        CurrentPage = null;
    }

    [RelayCommand]
    private void NavigateToHistory()
    {
        ResetSidebarActive();
        IsHistoryActive = true;
        CurrentPageTitle = "История";
        CurrentPageSubtitle = "— торговые записи";
        HasSubTabs = false;
        ResetSubTabs();

        CurrentPage = null;
    }

    [RelayCommand]
    private void NavigateToRadar()
    {
        ResetSidebarActive();
        IsRadarActive = true;
        CurrentPageTitle = "Радар";
        CurrentPageSubtitle = "— мониторинг цен";
        HasSubTabs = true;
        HasSubTab3 = false;
        SubTab1Title = "Активные цели";
        SubTab2Title = "Настройки радара";
        ResetSubTabs();
        IsSubTab1Active = true;

        CurrentPage = null;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        ResetSidebarActive();
        IsSettingsActive = true;
        CurrentPageTitle = "Настройки";
        CurrentPageSubtitle = "";
        HasSubTabs = false;
        ResetSubTabs();

        CurrentPage = null;
    }

    [RelayCommand]
    private void NavigateToSubTab1()
    {
        ResetSubTabs();
        IsSubTab1Active = true;

        if (IsAuctionActive)
            _navigationService.Navigate(new AuctionViewModel());
    }

    [RelayCommand]
    private void NavigateToSubTab2()
    {
        ResetSubTabs();
        IsSubTab2Active = true;
    }

    [RelayCommand]
    private void NavigateToSubTab3()
    {
        ResetSubTabs();
        IsSubTab3Active = true;
    }

    private void ApplyTierColors(string tier)
    {
        if (tier.Contains("Олигарх") || tier.Contains("VIP") || tier.Contains("Навсегда"))
        {
            TierAccentColor = Color.Parse("#FFB020");
            TierAccentColorEnd = Color.Parse("#FF8C00");
            TierBgColor = Color.Parse("#1A1200");
        }
        else if (tier.Contains("Спонсор"))
        {
            TierAccentColor = Color.Parse("#FF5566");
            TierAccentColorEnd = Color.Parse("#CC3344");
            TierBgColor = Color.Parse("#1A0008");
        }
        else if (tier.Contains("Supporter") || tier.Contains("Саппортер"))
        {
            TierAccentColor = Color.Parse("#44FF88");
            TierAccentColorEnd = Color.Parse("#22CC66");
            TierBgColor = Color.Parse("#001A0A");
        }
        else if (tier.Contains("Tester") || tier.Contains("Тестер"))
        {
            TierAccentColor = Color.Parse("#00E5FF");
            TierAccentColorEnd = Color.Parse("#00AACC");
            TierBgColor = Color.Parse("#001A1A");
        }
        else
        {
            TierAccentColor = Color.Parse("#9B59FF");
            TierAccentColorEnd = Color.Parse("#7B39DF");
            TierBgColor = Color.Parse("#0D001A");
        }
    }

    [RelayCommand]
    private void Minimize()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.MainWindow!.WindowState = WindowState.Minimized;
    }

    [RelayCommand]
    private void Close()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.MainWindow!.Close();
    }
}