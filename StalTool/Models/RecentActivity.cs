using System;
using System.Text.Json.Serialization;

namespace SatlTool.Models
{
    public class RecentActivity
    {
        public string PageTag { get; set; }
        public string PageTitle { get; set; }
        public string SubTag { get; set; }
        public string DetailTitle { get; set; }
        public string DetailSubtitle { get; set; }
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string Category { get; set; }
        public DateTime Timestamp { get; set; }

        [JsonIgnore]
        public string TimeDisplay => Timestamp.ToString("dd.MM HH:mm");
    }
}
