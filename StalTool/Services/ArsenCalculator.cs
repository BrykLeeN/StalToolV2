using System;
using SatlTool.Models;

namespace SatlTool.Services
{
    public static class ArsenCalculator
    {
        private const decimal RemainderPercent = 0.04m; // 4%

        // Базовые универсальные методы стали PUBLIC, чтобы генератор мог их использовать
        public static ArsenResult BaseCalculate(int quantity, decimal buyPrice, int repPerItem, int itemsPerTrade, decimal weightPerItem, decimal customSellPrice = 0)
        {
            if (quantity <= 0) return new ArsenResult();

            int fullTrades = quantity / itemsPerTrade;
            int remainder = quantity % itemsPerTrade;
            
            // Если остаток меньше 4% от пачки, мы его не считаем в финальный профит
            if (remainder > 0 && remainder <= itemsPerTrade * RemainderPercent)
            {
                remainder = 0;
            }

            decimal defaultSellPrice = buyPrice * 1.5m;
            decimal sellPricePerItem = customSellPrice > 0 ? customSellPrice : defaultSellPrice;

            var result = new ArsenResult
            {
                Quantity = quantity,
                Reputation = quantity * repPerItem,
                Buy = quantity * buyPrice,
                Sell = quantity * sellPricePerItem,
                FullTrades = fullTrades,
                RemainderItems = remainder,
                ItemsPerTrade = itemsPerTrade,
                TotalWeight = quantity * weightPerItem // РАСЧЕТ ВЕСА
            };

            CalculateTradeSum(result, itemsPerTrade, sellPricePerItem);
            return result;
        }

        public static ArsenResult BaseCalculateByMoney(decimal money, decimal buyPrice, int repPerItem, int itemsPerTrade, decimal weightPerItem, decimal customSellPrice = 0)
        {
            if (money <= 0 || buyPrice <= 0) return new ArsenResult();
            int quantity = (int)(money / buyPrice);
            return BaseCalculate(quantity, buyPrice, repPerItem, itemsPerTrade, weightPerItem, customSellPrice);
        }

        public static ArsenResult BaseCalculateByRep(int rep, decimal buyPrice, int repPerItem, int itemsPerTrade, decimal weightPerItem, decimal customSellPrice = 0)
        {
            if (rep <= 0) return new ArsenResult();
            int quantity = (int)Math.Ceiling((double)rep / repPerItem);
            return BaseCalculate(quantity, buyPrice, repPerItem, itemsPerTrade, weightPerItem, customSellPrice);
        }

        private static void CalculateTradeSum(ArsenResult result, int itemsPerTrade, decimal sellPricePerItem)
        {
            if (result.Quantity <= 0) return;

            int fullTrades = result.FullTrades;
            int remainder = result.RemainderItems;

            if (fullTrades <= 0)
            {
                result.FullTrades = 0;
                result.FullTradeSum = 0;
                result.LastTradeSum = remainder * sellPricePerItem;
                return;
            }

            decimal totalMoney = result.Quantity * sellPricePerItem;
            decimal fullTradeMoney = fullTrades * itemsPerTrade * sellPricePerItem;

            result.FullTradeSum = fullTradeMoney / fullTrades;
            result.LastTradeSum = remainder * sellPricePerItem;
        }
    }
}