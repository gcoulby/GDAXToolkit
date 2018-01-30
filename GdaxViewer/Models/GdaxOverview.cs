using System.Collections.Generic;
using GDAXClient.Services.Accounts.Models;
using GDAXClient.Services.Fills.Models.Responses;
using GDAXClient.Services.Orders.Models.Responses;
using GDAXClient.Services.Products.Models;

namespace GdaxViewer.Models
{
    public class GdaxOverview
    {
        public ProductTicker BtcEurTicker { get; set; }
        public IList<IList<OrderResponse>> OpenOrders { get; set; }
        public IList<IList<FillResponse>> Fills { get; set; }
        public IEnumerable<Account> Finances { get; set; }
        public GdaxOverview(ProductTicker btcEurTicker, IList<IList<OrderResponse>> openOrders, IList<IList<FillResponse>> fills, IEnumerable<Account> finances)
        {
            BtcEurTicker = btcEurTicker;
            OpenOrders = openOrders;
            Fills = fills;
            Finances = finances;
        }
    }
}
