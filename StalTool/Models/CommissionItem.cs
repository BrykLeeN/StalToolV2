using System.Text.Json.Serialization;
using Avalonia.Media;

namespace SatlTool.Models
{
    public class CommissionItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; } // Например: "Артефакты", "Медицина"
        public string Icon { get; set; }     // Можно будет добавить иконку позже, пока просто заглушка
        public string JsonPath { get; set; }
        public decimal Weight { get; set; }
        public string RarityColorHex { get; set; }

        [JsonIgnore] // Не сохраняем кисть в JSON, создаем её на лету
        public IBrush ColorBrush
        {
            get
            {
                if (string.IsNullOrEmpty(RarityColorHex)) return Brushes.White;
                try { return (SolidColorBrush)new BrushConverter().ConvertFrom(RarityColorHex); }
                catch { return Brushes.White; }
            }
        }
    }
}