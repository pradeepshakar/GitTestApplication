using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictAndPlay.Models
{
    public class Trade
    {
        public DateTime DateTime { get; set; }
        public decimal Price { get; set; }
        public decimal BidPrice { get; set; }
        public decimal AskPrice { get; set; }
        public decimal TradeMedian { get; set; }
        public decimal BuyMedian { get; set; }
        public decimal SellMedian { get; set; }
        public decimal BuyVolume { get; set; }
        public decimal SellVolume { get; set; }

        public decimal MedianScore
        {
            get
            {
                var buyDeviation = TradeMedian - BuyMedian;
                var sellDeviation = SellMedian - TradeMedian;
                return BuyMedian == 0 || SellMedian == 0 ? 0 : sellDeviation - buyDeviation;
            }
        }

        public decimal Score
        {
            get
            {
                var buyDeviation = Price - AskPrice;
                var sellDeviation = BidPrice - Price;
                return AskPrice == 0 || BidPrice == 0 ? 0 : sellDeviation - buyDeviation;
            }
        }
    }
}
