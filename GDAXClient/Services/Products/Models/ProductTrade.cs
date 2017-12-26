using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDAXClient.Services.Products.Models
{
    public class ProductTrade
    {
        public DateTime Time { get; set; }
        public string Trade_Id { get; set; }
        public decimal Price { get; set; }
        public decimal Size { get; set; }
        public string Side { get; set; }
    }
}
