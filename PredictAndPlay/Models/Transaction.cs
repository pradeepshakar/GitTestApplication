using GDAXClient.Services.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictAndPlay.Models
{
    public class Transaction
    {
        public const int SYNCINTERVAL = 5;

        public OrderResponse BuyOrder { get; set; }
        public OrderResponse SellOrder { get; set; }
        public bool ErrorSellOrder { get; set; }
        public decimal Factor { get; set; }
        public DateTime? SyncedAt { get; set; }

    }
}
