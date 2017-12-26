using GDAXClient.Services.Fills.Models;
using GDAXClient.Services.Fills.Models.Responses;
using GDAXClient.Services.Orders;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictAndPlay.Models
{
    public class Player
    {
        decimal sellOnPercentageHigher = decimal.Parse(ConfigurationManager.AppSettings["SellOnPercentageHigher"]);
        decimal buyDeviationPercentageMaxToCurrent = decimal.Parse(ConfigurationManager.AppSettings["BuyDeviationPercentageMaxToCurrent"]);
        decimal limitOrderPercentageLessCurrent = decimal.Parse(ConfigurationManager.AppSettings["LimitOrderPercentageLessCurrent"]);
        decimal overrideWindowConstraintPercentage = decimal.Parse(ConfigurationManager.AppSettings["OverrideWindowConstraintPercentage"]);

        ProductType productType = ProductType.LtcUsd;
        public ProductType ProductType { get => productType; }

        public GDAXClient.GDAXClient Client { get; set; }

        public List<Transaction> Transactions { get; private set; }

        public decimal Quota { get; private set; }

        public decimal EmergencyQuota { get; private set; }

        public decimal Wallet { get; private set; }

        public decimal PerBidValue { get; private set; }

        public Player(GDAXClient.GDAXClient client, decimal quota = 100, decimal perBidValue = 20)
        {
            this.Client = client;
            this.Quota = quota;
            this.Wallet = quota;
            this.PerBidValue = perBidValue;
            this.Transactions = new List<Transaction>();
        }

        public void AddOrder(string side, decimal price, decimal size, string buyOrderId = null)
        {
            if (side == "buy")
            {
                var orderResponse = this.Client.OrdersService.PlaceLimitOrderAsync(OrderSide.Buy, this.ProductType, decimal.Round(size, 2), decimal.Round(price, 2)).Result;
                if (orderResponse != null && (orderResponse.Status == "open" || orderResponse.Status == "pending" || orderResponse.Status == "active"))
                {
                    this.Transactions.Add(
                            new Transaction()
                            {
                                BuyOrder = orderResponse,
                                Factor = 1
                            }
                        );
                    this.Wallet -= orderResponse.Price * orderResponse.Size;
                }
            }
            else if (side == "sell")
            {
                if (string.IsNullOrEmpty(buyOrderId))
                {
                    throw new Exception("Buy order empty or null.");
                }
                var transaction = this.Transactions.Find(t => t.BuyOrder.Id.ToString() == buyOrderId);
                if (transaction == null)
                {
                    throw new Exception($"Buy order #{buyOrderId} not found.");
                }
                var orderResponse = this.Client.OrdersService.PlaceLimitOrderAsync(OrderSide.Sell, this.ProductType, decimal.Round(size, 2), decimal.Round(price, 2)).Result;
                if (orderResponse != null && (orderResponse.Status == "open" || orderResponse.Status == "pending" || orderResponse.Status == "active"))
                {
                    transaction.SellOrder = orderResponse;
                }
            }
        }

        public void CheckOrders(Trade trade)
        {
            //Get all transactions which has unsettled orders (buy or sell)
            var unsettledTransactions = this.GetUnSettledTransaction();
            foreach (var transaction in unsettledTransactions)
            {
                //Sync if status in not 'Settled' and order never synced ot interwal for sync is lapsed
                if (transaction.BuyOrder != null && transaction.BuyOrder.Settled == false)
                {
                    if (transaction.SyncedAt == null || transaction.SyncedAt?.AddSeconds(Transaction.SYNCINTERVAL) <= DateTime.UtcNow)
                    {
                        var response = this.Client.OrdersService.GetOrderByIdAsync(transaction.BuyOrder.Id.ToString()).Result;
                        if (response != null && transaction.BuyOrder.Status != response.Status)
                        {
                            transaction.BuyOrder = response;
                            if (transaction.BuyOrder.Settled == true)
                            {
                                CreateSellOrder(transaction.BuyOrder);
                            }
                        }
                        transaction.SyncedAt = DateTime.UtcNow;
                    }
                }
                else if (transaction.SellOrder != null && transaction.SellOrder.Settled == false)
                {
                    if (transaction.SyncedAt == null || transaction.SyncedAt?.AddSeconds(Transaction.SYNCINTERVAL) <= DateTime.UtcNow)
                    {
                        var response = this.Client.OrdersService.GetOrderByIdAsync(transaction.SellOrder.Id.ToString()).Result;
                        if (response != null && transaction.SellOrder.Status != response.Status)
                        {
                            transaction.SellOrder = response;
                        }
                        transaction.SyncedAt = DateTime.UtcNow;
                    }
                }
            }
        }

        public void CreateSellOrder(OrderResponse buyOrder)
        {
            decimal minDeviationBetweenboughtAndCurrenttoSellInPercentage = sellOnPercentageHigher;
            var bidPrice = ((100 + minDeviationBetweenboughtAndCurrenttoSellInPercentage) * buyOrder.Price) / 100;
            this.AddOrder("sell", bidPrice, buyOrder.Size, buyOrder.Id.ToString());
        }

        public List<Transaction> GetUnSettledTransaction()
        {
            return this.Transactions.FindAll(t => t.BuyOrder.Settled == false || t.SellOrder.Settled == false);
        }

        public OrderResponse GetLastOpenBuyOrder()
        {
            var openBuyTransaction = this.Transactions.FindAll(t => (t.BuyOrder != null && t.BuyOrder.Status == "open" || t.BuyOrder.Status == "pending" || t.BuyOrder.Status == "active")
            || (t.SellOrder == null || (t.SellOrder.Status == "open" || t.SellOrder.Status == "pending" || t.SellOrder.Status == "active")));
            if (openBuyTransaction.Count > 0)
            {
                return openBuyTransaction.OrderBy(t => t.BuyOrder.Created_at)?.Last().BuyOrder;
            }
            return null;
        }

        public void Play(Trend trend, Trade currentTrade)
        {
            this.CheckOrders(currentTrade);

            double samplingWindowSize = trend.WindowSize; //in seconds
            decimal overrideWindowConstraintIfCurrentValueLessThanXPercentageOfMax = overrideWindowConstraintPercentage;
            decimal minDeviationBetweenCurrentAndMaxtoBuyInPercentage = buyDeviationPercentageMaxToCurrent;
            decimal limitLessValueThanCurrentToBuyInPercentage = limitOrderPercentageLessCurrent;

            DateTime trendStartTime = trend.Trades.First().DateTime;
            DateTime? lastBoughtTime = null; //Set to last bought time 

            var currentWindowTrades = trend.Trades.FindAll(trade => trade.DateTime > currentTrade.DateTime.AddSeconds(-samplingWindowSize));

            decimal currentValue = currentTrade.Price;
            DateTime currentTradeTime = currentTrade.DateTime;
            var minValue = currentWindowTrades.Min(trade => trade.Price);
            var maxValue = currentWindowTrades.Max(trade => trade.Price);
            decimal? lastBoughtPrice = null;

            var lastBuyOrder = this.GetLastOpenBuyOrder();
            if (lastBuyOrder != null)
            {
                lastBoughtPrice = lastBuyOrder.Price;
                lastBoughtTime = lastBuyOrder.Created_at;
            }

            //Can Buy
            //0. If Wallet has amount greater than per Bid Value
            //1. Not bought in current window and current value - max value has deviation greater than value defined
            //2. Bought in current window, but current value - last bought/max value deviation is greater thand value defined

            if (this.Wallet >= this.PerBidValue)
            {
                if ((LastBoughtInSameWindow(currentTradeTime, trendStartTime, lastBoughtTime, samplingWindowSize) == false)
                    && CanBuyWithMinMaxDeviation(currentValue, maxValue, minDeviationBetweenCurrentAndMaxtoBuyInPercentage))
                {
                    var bidPrice = ((100 - limitLessValueThanCurrentToBuyInPercentage) * currentValue) / 100;
                    var bidSize = this.PerBidValue / bidPrice;
                    this.AddOrder("buy", bidPrice, bidSize);
                }
                else if ((LastBoughtInSameWindow(currentTradeTime, trendStartTime, lastBoughtTime, samplingWindowSize) == true)
                    && CanIgnoreWindowConstraint(currentValue, lastBoughtPrice ?? maxValue, overrideWindowConstraintIfCurrentValueLessThanXPercentageOfMax))
                {
                    var bidPrice = ((100 - limitLessValueThanCurrentToBuyInPercentage) * currentValue) / 100;
                    var bidSize = this.PerBidValue / bidPrice;
                    this.AddOrder("buy", bidPrice, bidSize);
                }
            }

        }

        public bool LastBoughtInSameWindow(DateTime currentTime, DateTime trendStartTime, DateTime? lastBoughtTime, double samplingWindow)
        {
            bool result = false;
            //if never bought in current trend session and trend is older than samplingWindow
            if (lastBoughtTime == null && trendStartTime.AddSeconds(samplingWindow) > currentTime)
            {
                result = false; //Change this to true
            }
            //If never bought and trend is not older than samplingWindow
            else if (lastBoughtTime == null)
            {
                result = false;
            }
            //If last bought is in same window
            else if (lastBoughtTime?.AddSeconds(samplingWindow) >= currentTime)
            {
                result = true;
            }
            return result;
        }

        public bool CanIgnoreWindowConstraint(decimal currentValue, decimal maxValue, decimal overrideWindowConstraintPercentage)
        {
            decimal difference = maxValue - currentValue;
            decimal differencePercentage = (difference * 100) / currentValue;
            return differencePercentage >= overrideWindowConstraintPercentage;
        }

        public bool CanBuyWithMinMaxDeviation(decimal currentValue, decimal maxValue, decimal deviationPercentageToBuy)
        {
            decimal difference = maxValue - currentValue;
            decimal differencePercentage = (difference * 100) / currentValue;
            return differencePercentage >= deviationPercentageToBuy;
        }

    }
}
