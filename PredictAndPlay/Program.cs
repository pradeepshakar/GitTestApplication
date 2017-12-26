using GDAXClient.Authentication;
using GDAXClient.Services.Orders;
using GDAXClient.Services.Products.Models;
using PredictAndPlay.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PredictAndPlay
{
    class Program
    {
        static void Main(string[] args)
        {

            var apiKey = ConfigurationManager.AppSettings["ApiKey"];
            var apiSecret = ConfigurationManager.AppSettings["SecretKey"];
            var passPhrase = ConfigurationManager.AppSettings["PassPhrase"];

            var trendSize = int.Parse(ConfigurationManager.AppSettings["TrendItemCount"]);
            var windowSize = int.Parse(ConfigurationManager.AppSettings["TrendWindow"]);
            var pauseInterval = int.Parse(ConfigurationManager.AppSettings["PauseInterval"]);

            var playerQuota = decimal.Parse(ConfigurationManager.AppSettings["PlayerQuota"]);
            var perBidValue = decimal.Parse(ConfigurationManager.AppSettings["PerBidValue"]);

            Authenticator auth = new Authenticator(apiKey, apiSecret, passPhrase);
            GDAXClient.GDAXClient client = new GDAXClient.GDAXClient(auth, false);
            //var result = client.OrdersService.PlaceLimitOrderAsync(OrderSide.Buy, ProductType.LtcUsd, 0.5M, 20).Result;


            var player = new Player(client, playerQuota, perBidValue);
            var trend = new Trend(trendSize, windowSize);
            var view = new ConsoleView(player, trend);
            while (true)
            {
                try
                {
                    var trades = client.ProductsService.GetProductTradesAsync(player.ProductType).Result.ToList();
                    var currentTicker = client.ProductsService.GetProductTickerAsync(player.ProductType).Result;

                    var buyTrades = trades.FindAll(t => t.Side == "buy");
                    var sellTrades = trades.FindAll(t => t.Side == "sell");

                    var tradeMedian = GetMedian(trades);
                    var buyMedian = GetMedian(buyTrades);
                    var sellMedian = GetMedian(sellTrades);
                    var buyVolume = buyTrades.Sum(t => t.Size);
                    var sellVolume = sellTrades.Sum(t => t.Size);

                    var trade = new Trade()
                    {
                        DateTime = currentTicker.Time,
                        Price = Decimal.Round(currentTicker.Price, 2),
                        BidPrice = Decimal.Round(currentTicker.Bid, 2),
                        AskPrice = Decimal.Round(currentTicker.Ask, 2),
                        TradeMedian = Decimal.Round(tradeMedian, 2),
                        BuyMedian = Decimal.Round(buyMedian, 2),
                        BuyVolume = Decimal.Round(buyVolume, 2),
                        SellMedian = Decimal.Round(sellMedian, 2),
                        SellVolume = Decimal.Round(sellVolume, 2)
                    };
                    trend.Add(trade);
                    view.Show();
                    //Console.WriteLine($"Time: {trade.DateTime}, Price: {trade.Price}, Bid: {trade.BidPrice}, Ask: {trade.AskPrice}, TradeM: {trade.TradeMedian}, BuyM: {trade.BuyMedian}, SellM: {trade.SellMedian}, BV: {trade.BuyVolume}, SV: {trade.SellVolume}, MedianScore: {trade.MedianScore}");
                    
                    player.Play(trend, trade);
                }
                catch(Exception ex)
                {
                    view.ShowError(ex);
                    Helper.WriteError(ex);
                }
                Thread.Sleep(pauseInterval);
            }



        }

        public static decimal GetMedian(List<ProductTrade> trades)
        {
            var tradeCount = trades.Sum(t => t.Size);
            var tradeCost = trades.Sum(t => t.Size * t.Price);
            return tradeCount == 0 ? 0 : tradeCost/tradeCount;
        }
    }
}
