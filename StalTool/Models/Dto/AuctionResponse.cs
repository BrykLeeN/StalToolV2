using System.Collections.Generic;

namespace StalTool.Models.Dto;

public class AuctionResponse
{
    public class AuctionResponselable
    {
        public long total { get; set; }
        public List<AuctionLot> lots { get; set; }
    }
}