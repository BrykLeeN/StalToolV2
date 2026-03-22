using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace SatlTool.Services
{
    public class AuthService : INotifyPropertyChanged
    {
        
        public string Token { get; set; } = string.Empty;
        
        private static AuthService _instance;
        public static AuthService Instance => _instance ??= new AuthService();

        private string _currentUsername = "Local User";
        public string CurrentUsername
        {
            get => _currentUsername;
            set { _currentUsername = value; OnPropertyChanged(); }
        }

        private bool _isAuthenticated = false;
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set { _isAuthenticated = value; OnPropertyChanged(); }
        }

        public string SessionToken { get; set; }

        // --- НОВЫЕ ПОЛЯ ДЛЯ ПОДПИСКИ ---
        
        // 1. Статус подписки (Тир)
        private string _subscriptionTier = "Базовая";
        public string SubscriptionTier 
        { 
            get => _subscriptionTier; 
            set 
            { 
                _subscriptionTier = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(TierColorBrush)); 
                OnPropertyChanged(nameof(SubscriptionStatusText)); // Обновляем текст даты
            } 
        }

        // 2. Цвет статуса (для UI)
        public Brush TierColorBrush
        {
            get
            {
                if (SubscriptionTier != null)
                {
                    if (SubscriptionTier.Contains("Легенда") || SubscriptionTier.Contains("Олигарх") || SubscriptionTier.Contains("VIP") || SubscriptionTier.Contains("Навсегда"))
                        return new SolidColorBrush((Color)Color.Parse("#FFB020")); // Золотой
                    
                    if (SubscriptionTier.Contains("Tester") || SubscriptionTier.Contains("Тестер"))
                        return new SolidColorBrush((Color)Color.Parse("#00E5FF")); // Неоново-голубой
                    
                    if (SubscriptionTier.Contains("Спонсор"))
                        return new SolidColorBrush((Color)Color.Parse("#FF5555")); // Красный
                }
                
                return new SolidColorBrush((Color)Color.Parse("#7B68EE")); // Фиолетовый
            }
        }

        private DateTime _subscriptionEndDate;
        public DateTime SubscriptionEndDate
        {
            get => _subscriptionEndDate;
            set 
            { 
                _subscriptionEndDate = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(IsSubscriptionActive)); 
                OnPropertyChanged(nameof(SubscriptionStatusText));
            }
        }

        // Проверяет активна ли подписка
        public bool IsSubscriptionActive 
        {
            get 
            {
                if (SubscriptionTier != null && (SubscriptionTier.Contains("Легенда") || SubscriptionTier.Contains("Олигарх") || SubscriptionTier.Contains("Навсегда") || SubscriptionTier.Contains("VIP") || SubscriptionTier.Contains("Tester") || SubscriptionTier.Contains("Тестер")))
                    return true;
                    
                return SubscriptionEndDate > DateTime.UtcNow;
            }
        }

        // Красивый текст для профиля
        public string SubscriptionStatusText
        {
            get
            {
                if (!IsAuthenticated) return "Не авторизован";
                
                // Фикс бесконечности для VIP
                if (SubscriptionTier != null && (SubscriptionTier.Contains("Легенда") || SubscriptionTier.Contains("Олигарх") || SubscriptionTier.Contains("Навсегда") || SubscriptionTier.Contains("VIP") || SubscriptionTier.Contains("Tester") || SubscriptionTier.Contains("Тестер")))
                {
                    return "Действует до: ∞";
                }
                
                if (IsSubscriptionActive)
                {
                    var left = SubscriptionEndDate - DateTime.UtcNow;
                    return $"Активна (Осталось: {left.Days} дн.)";
                }
                return "Подписка истекла";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        
        // Скрытый доступ для разработчиков
        public bool IsQaTester => SubscriptionTier != null && 
                                  (SubscriptionTier.Contains("Tester") || SubscriptionTier.Contains("Тестер") || CurrentUsername == "BrykLeeN___");
    }
    
}