using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictAndPlay.Models
{
    public class Trend
    {
        private List<Trade> trades = new List<Trade>();
        private int maxCount = 0;
        private int windowSize = 0;

        public List<Trade> Trades { get => trades;}
        public int WindowSize { get => windowSize; }

        public Trend(int maxCount, int windowSize = 1800)
        {
            this.maxCount = maxCount;
            this.windowSize = windowSize;
        }

        public void Add(Trade trade)
        {
            if(this.Trades.Count >= maxCount)
            {
                this.Trades.RemoveAt(0);
            }
            this.Trades.Add(trade);
        }

        public DateTime? GetTrendStartTime()
        {
            if(this.Trades.Count > 0)
            {
                return this.Trades[0].DateTime;
            }
            return null;
        }

        public List<Trade> GetCurrentWindowTrades()
        {
            return this.Trades.FindAll(trade => trade.DateTime >= DateTime.UtcNow.AddSeconds(-this.WindowSize));
        }

        public decimal GetMaxValue(bool currentWindow = true)
        {
            decimal? maxValue;
            if(currentWindow == true)
            {
                maxValue = this.GetCurrentWindowTrades()?.Max(trade => trade.Price);
            }
            else
            {
                maxValue = this.Trades?.Max(trade => trade.Price);
            }
            return maxValue ?? 0;
        }

        public decimal GetMinValue(bool currentWindow = true)
        {
            decimal? minValue;
            if (currentWindow == true)
            {
                minValue = this.GetCurrentWindowTrades()?.Min(trade => trade.Price);
            }
            else
            {
                minValue = this.Trades?.Min(trade => trade.Price);
            }
            return minValue ?? 0;
        }

        public decimal GetCurrentValue()
        {
            return this.Trades.Last()?.Price ?? 0;
        }

        public List<Trade> GetLastNTrades(int count)
        {
            var currentCount = this.Trades.Count;
            return this.Trades.Skip(currentCount - count).ToList();
        }


    }
}
