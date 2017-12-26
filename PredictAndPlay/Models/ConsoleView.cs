using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictAndPlay.Models
{
    public class ConsoleView
    {
        Point minValueLocation = new Point(3, 1);
        Point currentValueLocation = new Point(30, 1);
        Point maxValueLocation = new Point(70, 1);
        Point walletLocation = new Point(90, 1);
        

        Point trendLocation = new Point(3, 5);
        Point transactionLocation = new Point(60, 5);

        Point exceptionLocation = new Point(3, 28);

        Player player;
        Trend trend;

        public Player Player { get => player; }
        public Trend Trend { get => trend; }

        public ConsoleView(Player player, Trend trend)
        {
            this.player = player;
            this.trend = trend;
            Console.CursorVisible = false;
        }

        public void SetCursorLocation(Point point)
        {
            Console.SetCursorPosition(point.X, point.Y);
        }

        public void SetCursorLocation(int x, int y)
        {
            Console.SetCursorPosition(x, y);
        }

        public void Show()
        {
            this.ShowTrendMinCurrentMaxValues();
            this.ShowTrend();
            this.ShowWalletDetails();
            this.ShowTransactions();
        }

        public void ShowTrendMinCurrentMaxValues()
        {
            var minValue = trend.GetMinValue();
            var currentValue = trend.GetCurrentValue();
            var maxValue = trend.GetMaxValue();
            var deviationMin = ((currentValue - minValue) * 100) / minValue;
            var deviationMax = ((maxValue - currentValue) * 100) / maxValue;
            this.SetCursorLocation(this.minValueLocation);
            Console.Write($"Min: {minValue}");
            this.SetCursorLocation(this.currentValueLocation);
            Console.Write($"Current: {currentValue} ({decimal.Round(deviationMin, 2)}%/{decimal.Round(deviationMax, 2)}%)      ");
            this.SetCursorLocation(this.maxValueLocation);
            Console.Write($"Max: {maxValue}");
        }

        public void ShowTrend()
        {
            var trendList = this.Trend.GetLastNTrades(20);
            int x = trendLocation.X;
            int y = trendLocation.Y;
            this.SetCursorLocation(x, y);
            Console.Write("Price");
            this.SetCursorLocation(x + 10, y);
            Console.Write("Bid");
            this.SetCursorLocation(x + 20, y);
            Console.Write("Ask");
            this.SetCursorLocation(x + 30, y);
            Console.Write("Date");
            y++;
            foreach (var t in trendList)
            {
                x = trendLocation.X;
                this.SetCursorLocation(x, y);
                Console.Write(t.Price);
                this.SetCursorLocation(x + 10, y);
                Console.Write(t.BidPrice);
                this.SetCursorLocation(x + 20, y);
                Console.Write(t.AskPrice);
                this.SetCursorLocation(x + 30, y);
                Console.Write(t.DateTime);
                y++;
            }
        }

        public void ShowTransactions()
        {
            var maxCount = 20;
            var transactions = this.Player.GetUnSettledTransaction();
            if(transactions.Count > maxCount)
            {
                var count = transactions.Count;
                transactions = transactions.Skip(count - maxCount).ToList();
            }
            int x = transactionLocation.X;
            int y = transactionLocation.Y;
            this.SetCursorLocation(x, y);
            Console.Write("Size");
            this.SetCursorLocation(x + 10, y);
            Console.Write("BPrice");
            this.SetCursorLocation(x + 20, y);
            Console.Write("SPrice");
            this.SetCursorLocation(x + 30, y);
            Console.Write("CreatedAt");
            this.SetCursorLocation(x + 40, y);
            Console.Write("Status");
            y++;
            if (transactions.Count == 0)
            {
                this.SetCursorLocation(x, y);
                Console.Write("No active Transactions..                                   ");
                y++;
                for(int i = 1; i< maxCount; i++)
                {
                    this.SetCursorLocation(x, y);
                    Console.Write("                                                           ");
                    y++;
                }
            }
            else
            {
                foreach (var t in transactions)
                {
                    this.SetCursorLocation(x, y);
                    Console.Write("                                                           ");
                    this.SetCursorLocation(x, y);
                    Console.Write(t.BuyOrder?.Size.ToString("F") ?? "------");
                    this.SetCursorLocation(x + 10, y);
                    Console.Write(t.BuyOrder?.Price.ToString("F") ?? "------");
                    this.SetCursorLocation(x + 20, y);
                    Console.Write(t.SellOrder?.Price.ToString("F") ?? "------");
                    this.SetCursorLocation(x + 30, y);
                    Console.Write(t.BuyOrder?.Created_at.ToShortTimeString() ?? "------");
                    this.SetCursorLocation(x + 40, y);
                    string status = t.SellOrder?.Status;
                    if (status != null)
                    {
                        status = "Sell - " + status  + "    ";
                    }
                    else
                    {
                        status = t.BuyOrder?.Status;
                        status = status == null ? "------" : "Buy - " + status + "    ";
                    }
                    Console.Write(status);
                    y++;
                }
                for(int i = transactions.Count; i < maxCount; i++)
                {
                    this.SetCursorLocation(x, y);
                    Console.Write("                                                           ");
                    y++;
                }
            }
        }

        public void ShowWalletDetails()
        {
            int x = walletLocation.X;
            int y = walletLocation.Y;
            this.SetCursorLocation(x, y);
            Console.Write($"Quota: {this.player.Quota}");
            this.SetCursorLocation(x, y + 1);
            Console.Write($"Wallet: {this.player.Wallet}");
        }

        public void ShowError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            this.SetCursorLocation(this.exceptionLocation);
            Console.Write(DateTime.UtcNow + " : " + ex.Message);
            Console.ResetColor();
        }


    }
}
