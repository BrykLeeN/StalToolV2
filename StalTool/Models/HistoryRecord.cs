using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SatlTool.Models
{
    public class HistoryRecord
    {
        public string Type { get; set; } = "";
        public int Quantity { get; set; }
        public int Reputation { get; set; }

        public int TradesMain { get; set; }
        public int TradesBonus { get; set; }

        public int Buy { get; set; }
        public int Sell { get; set; }

        public DateTime Time { get; set; }
    }
}
