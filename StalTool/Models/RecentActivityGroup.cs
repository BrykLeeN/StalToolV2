using System;

namespace SatlTool.Models
{
    public class RecentActivityGroup
    {
        public string GroupKey { get; set; }
        public RecentActivity Primary { get; set; }
        public RecentActivity Secondary { get; set; }
        public DateTime LastTimestamp { get; set; }
    }
}
