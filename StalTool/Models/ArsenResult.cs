using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SatlTool.Models
{
    public class ArsenResult
    {
        // Ввод
        public int Quantity { get; set; }

        // Репутация
        public int Reputation { get; set; }

        // Трейды
        public int FullTrades { get; set; }        // полные трейды
        public int RemainderItems { get; set; }    // остаток предметов
        public int ItemsPerTrade { get; set; }     // 256 / 64 / ...

        // Деньги
        public decimal Buy { get; set; }
        public decimal Sell { get; set; }

        // Деньги за трейды
        public decimal FullTradeSum { get; set; }  // за 1 полный трейд
        public decimal LastTradeSum { get; set; }  // за последний трейд (если есть)

        // Для отображения "3 (+66 шт)"
        public string TradesDisplay =>
            RemainderItems > 0
                ? $"{FullTrades} (+{RemainderItems} шт)"
                : FullTrades.ToString();
        public decimal SellValue => (Sell);
        public decimal BuyValue => (Buy);
        public string BuyHuman => FormatMoney(Buy);
        public string SellHuman => FormatMoney(Sell);
        // Вес
        public decimal TotalWeight { get; set; }
        public string TotalWeightDisplay => TotalWeight > 0 ? $"{TotalWeight:0.##} кг" : "0 кг";
        public decimal PricePerRep => Reputation > 0 ? Buy / Reputation : 0;
        
        private static string FormatMoney(decimal value)
        {
            if (value >= 1_000_000)
                return $"{value:N0} ({value / 1_000_000m:0.#}кк)";

            if (value >= 1_000)
                return $"{value:N0}";

            return value.ToString("0");
        }
        



    }
}
