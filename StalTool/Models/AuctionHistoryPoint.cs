using System;

namespace SatlTool.Models
{
    public class AuctionHistoryPoint
    {
        public long Price { get; set; }       // Цена за 1 штуку (по ней строится график)
        public long TotalPrice { get; set; }  // Общая сумма всей сделки (для описания)
        public DateTime Time { get; set; }
        public int Amount { get; set; }
    }
}