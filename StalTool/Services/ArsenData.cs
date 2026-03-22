using System.Collections.Generic;

namespace SatlTool.Models
{
    // Модель предмета Арсена
    public class ArsenItemConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal BuyPrice { get; set; }
        public int RepPerItem { get; set; }
        public int ItemsPerTrade { get; set; }
        public string Category { get; set; }
        public decimal BaseWeight { get; set; }
    }

    // Хранилище всех предметов
    public static class ArsenData
    {
        public static List<ArsenItemConfig> GetItems()
        {
            return new List<ArsenItemConfig>
            {
                // === ОБЫЧНЫЕ ПРЕДМЕТЫ ===
                new ArsenItemConfig { Name = "Очищенное вещество", Description = "Не выпадает при смерти", BuyPrice = 30000, RepPerItem = 9, BaseWeight = 0.05m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Папоротник", Description = "Не выпадает при смерти", BuyPrice = 13000, RepPerItem = 12, BaseWeight = 0.15m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Портативный квантовый генератор", Description = "Не выпадает при смерти", BuyPrice = 60000, RepPerItem = 50, BaseWeight = 0.15m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Пси-маячок", Description = "НЕ выпадает при смерти", BuyPrice = 37000, RepPerItem = 7, BaseWeight = 3.00m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Блок данных «Гамма»", Description = "НЕ выпадает при смерти", BuyPrice = 60000, RepPerItem = 45, BaseWeight = 0.20m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Блок данных «Бета»", Description = "НЕ выпадает при смерти", BuyPrice = 160000, RepPerItem = 45, BaseWeight = 0.20m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Концентрированная лимбоплазма", Description = "НЕ Выпадает при смерти", BuyPrice = 715000, RepPerItem = 800, BaseWeight = 0.20m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Блок данных «Альфа»", Description = "НЕ выпадает при смерти", BuyPrice = 90000, RepPerItem = 45, BaseWeight = 0.20m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Блок данных «Лямбда»", Description = "Не выпадает при смерти", BuyPrice = 115000, RepPerItem = 150, BaseWeight = 0.05m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Росцветщий Горьколистник", Description = "НЕ выпадает при смерти", BuyPrice = 48000, RepPerItem = 85, BaseWeight = 0.20m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Темный лимб", Description = "НЕ выпадает при смерти", BuyPrice = 125000, RepPerItem = 90, BaseWeight = 0.30m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Модифицированная аномальная батарея", Description = "НЕ выпадает при смерти", BuyPrice = 3200000, RepPerItem = 1000, BaseWeight = 0.45m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Набор компонентов брони (мастер)", Description = "НЕ выпадает при смерти", BuyPrice = 2000, RepPerItem = 2, BaseWeight = 0.20m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Набор компонентов оружия (мастер)", Description = "НЕ выпадает при смерти", BuyPrice = 26000, RepPerItem = 6, BaseWeight = 0.20m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Хроносфера", Description = "НЕ выпадает при смерти", BuyPrice = 250000, RepPerItem = 175, BaseWeight = 0.25m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Малый артефактный фрагмент", Description = "Выпадает при смерти", BuyPrice = 500, RepPerItem = 1, BaseWeight = 0.20m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Обычный артефактный фрагмент", Description = "Выпадает при смерти", BuyPrice = 833, RepPerItem = 2, BaseWeight = 0.30m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Крупный артефактный фрагмент", Description = "Выпадает при смерти", BuyPrice = 1000, RepPerItem = 3, BaseWeight = 0.40m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Огромный артефактный фрагмент", Description = "Выпадает при смерти", BuyPrice = 1333, RepPerItem = 2, BaseWeight = 0.50m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Ноутбук", Description = "Выпадает при смерти", BuyPrice = 20000, RepPerItem = 12, BaseWeight = 10.00m, ItemsPerTrade = 1, Category = "Обычные" },
                new ArsenItemConfig { Name = "Фильтр", Description = "Выпадает при смерти", BuyPrice = 2250, RepPerItem = 12, BaseWeight = 0.50m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Дорогие сигареты", Description = "Выпадает при смерти", BuyPrice = 2500, RepPerItem = 5, BaseWeight = 0.30m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Запчасти для ПДА", Description = "Выпадает при смерти", BuyPrice = 4500, RepPerItem = 18, BaseWeight = 0.70m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Медная катушка", Description = "Выпадает при смерти", BuyPrice = 3500, RepPerItem = 10, BaseWeight = 0.30m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Соленоид", Description = "Выпадает при смерти", BuyPrice = 1000, RepPerItem = 2, BaseWeight = 0.40m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Рука сильного шныря", Description = "Выпадает при смерти", BuyPrice = 2000, RepPerItem = 9, BaseWeight = 0.50m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Лоскут светящейся кожи", Description = "Выпадает при смерти", BuyPrice = 4500, RepPerItem = 15, BaseWeight = 0.40m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Кость мутанта", Description = "Выпадает при смерти", BuyPrice = 4800, RepPerItem = 6, BaseWeight = 2.00m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Глаз сильного бурелома", Description = "Выпадает при смерти", BuyPrice = 6200, RepPerItem = 15, BaseWeight = 1.00m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Голова сильной бестии", Description = "Выпадает при смерти", BuyPrice = 20000, RepPerItem = 35, BaseWeight = 6.00m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Черная селезенка", Description = "Выпадает при смерти", BuyPrice = 100000, RepPerItem = 150, BaseWeight = 1.60m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Сердце Лимб", Description = "Выпадает при смерти", BuyPrice = 500000, RepPerItem = 1000, BaseWeight = 3.00m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Остатки приборов «Шепота»", Description = "Выпадает при смерти", BuyPrice = 27000, RepPerItem = 95, BaseWeight = 2.50m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Протоартефакт", Description = "Выпадает при смерти", BuyPrice = 1800, RepPerItem = 4, BaseWeight = 0.10m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Комплект заводских инструментов", Description = "Выпадает при смерти", BuyPrice = 6800, RepPerItem = 11, BaseWeight = 0.40m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Драгоценности", Description = "Выпадает при смерти", BuyPrice = 28000, RepPerItem = 65, BaseWeight = 0.20m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Компоненты редких сплавов", Description = "Выпадает при смерти", BuyPrice = 15500, RepPerItem = 35, BaseWeight = 0.40m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Промышленные запчасти", Description = "Выпадает при смерти", BuyPrice = 19000, RepPerItem = 6, BaseWeight = 0.40m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Прототипы «Шепота»", Description = "Выпадает при смерти", BuyPrice = 49000, RepPerItem = 55, BaseWeight = 0.30m, ItemsPerTrade = 64, Category = "Обычные" },
                new ArsenItemConfig { Name = "Системы наведения", Description = "Выпадает при смерти", BuyPrice = 15000, RepPerItem = 35, BaseWeight = 0.40m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Военный радиопередатчик", Description = "Выпадает при смерти", BuyPrice = 16000, RepPerItem = 10, BaseWeight = 0.40m, ItemsPerTrade = 256, Category = "Обычные" },
                new ArsenItemConfig { Name = "Ящик с запчастями", Description = "Выпадает при смерти", BuyPrice = 1500, RepPerItem = 10, BaseWeight = 0.40m, ItemsPerTrade = 256, Category = "Обычные" },

                // === ПЕРСОНАЛЬНЫЕ ЯЩИКИ ===
                new ArsenItemConfig { Name = "Ящик с ресурсами (П,М; С,Р; И,О)", Description = "Не выпадает", BuyPrice = 1000, RepPerItem = 155, BaseWeight = 3.00m, ItemsPerTrade = 10, Category = "ПерсЯщики" },
                new ArsenItemConfig { Name = "Ящик с ресурсами (З,П; М; Н)", Description = "Не выпадает", BuyPrice = 1000, RepPerItem = 160, BaseWeight = 2.50m, ItemsPerTrade = 10, Category = "ПерсЯщики" },
                new ArsenItemConfig { Name = "Ящик с ресурсами (Р,С; С; П,М)", Description = "Не выпадает", BuyPrice = 1000, RepPerItem = 160, BaseWeight = 2.50m, ItemsPerTrade = 10, Category = "ПерсЯщики" },
                new ArsenItemConfig { Name = "Ящик с ресурсами (С,Р; О,У; З,П)", Description = "Не выпадает", BuyPrice = 1000, RepPerItem = 160, BaseWeight = 2.50m, ItemsPerTrade = 10, Category = "ПерсЯщики" },
                new ArsenItemConfig { Name = "Ящик с ресурсами (Т; П,Э; О,У)", Description = "Не выпадает", BuyPrice = 1000, RepPerItem = 160, BaseWeight = 2.50m, ItemsPerTrade = 10, Category = "ПерсЯщики" },
                new ArsenItemConfig { Name = "Ящик с ресурсами (М; Т; Н)", Description = "Не выпадает", BuyPrice = 1000, RepPerItem = 160, BaseWeight = 2.50m, ItemsPerTrade = 10, Category = "ПерсЯщики" },
                new ArsenItemConfig { Name = "Ящик с ресурсами (П,Э; С; И,О)", Description = "Не выпадает", BuyPrice = 1000, RepPerItem = 160, BaseWeight = 2.50m, ItemsPerTrade = 10, Category = "ПерсЯщики" },

                // === НЕ ПЕРСОНАЛЬНЫЕ ЯЩИКИ ===
                new ArsenItemConfig { Name = "Ящик с ресурсами (Н,Б; М,Г,«Г»; М,Г,«Б»)", Description = "Выпадает при смерти", BuyPrice = 1000, RepPerItem = 1, BaseWeight = 1.00m, ItemsPerTrade = 10, Category = "НеПерсЯщики" },
                new ArsenItemConfig { Name = "Ящик с ресурсами (М,Г,«А»; П,М; Я,П)", Description = "Выпадает при смерти", BuyPrice = 1000, RepPerItem = 1, BaseWeight = 1.00m, ItemsPerTrade = 10, Category = "НеПерсЯщики" },
                new ArsenItemConfig { Name = "Ящик с ресурсами (А,М; М,Ф; З,М)", Description = "Выпадает при смерти", BuyPrice = 1000, RepPerItem = 1, BaseWeight = 1.00m, ItemsPerTrade = 10, Category = "НеПерсЯщики" },
                new ArsenItemConfig { Name = "Ящик с ресурсами (Г; Э; Ч)", Description = "Выпадает при смерти", BuyPrice = 1000, RepPerItem = 1, BaseWeight = 1.00m, ItemsPerTrade = 10, Category = "НеПерсЯщики" },
                new ArsenItemConfig { Name = "Ящик с ресурсами (М,Г,«А»; Н,Б; З,М)", Description = "Выпадает при смерти", BuyPrice = 1000, RepPerItem = 1, BaseWeight = 1.00m, ItemsPerTrade = 10, Category = "НеПерсЯщики" },
                new ArsenItemConfig { Name = "Ящик с ресурсами (М,Г,«Г»; Ч; Я,П)", Description = "Выпадает при смерти", BuyPrice = 1000, RepPerItem = 1, BaseWeight = 1.00m, ItemsPerTrade = 10, Category = "НеПерсЯщики" },
                new ArsenItemConfig { Name = "Ящик с ресурсами (М,Г,«Б»; Г; П,М)", Description = "Выпадает при смерти", BuyPrice = 1000, RepPerItem = 1, BaseWeight = 1.00m, ItemsPerTrade = 10, Category = "НеПерсЯщики" },
            };
        }
    }
}