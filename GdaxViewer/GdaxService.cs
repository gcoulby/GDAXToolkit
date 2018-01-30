using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GdaxViewer.Models;
using GdaxViewer.Properties;
using GDAXClient.Authentication;
using GDAXClient.Services.Orders.Models.Responses;
using GDAXClient.Shared;

namespace GdaxViewer
{
    public class GdaxService : GDAXClient.GDAXClient
    {
        public GdaxService(IAuthenticator authenticator, bool sandBox = false) : base(authenticator, sandBox)
        {
        }

        public static async Task<bool> CanConnect(string apiKey, string apiSecret, string apiPassPhrase)
        {
            var auth = new Authenticator(apiKey, apiSecret, apiPassPhrase);
            var gdaxClient = new GDAXClient.GDAXClient(auth);
            try
            {
                var ticker = await gdaxClient.ProductsService.GetProductTickerAsync(ProductType.BtcEur);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async void CancelOrder(string id)
        {
            await OrdersService.CancelOrderByIdAsync(id);
        }

        public async void CancelAllOrders()
        {
            await OrdersService.CancelAllOrdersAsync();
        }

        public async Task<decimal> GetEurPrice()
        {
            var ep = await ProductsService.GetProductTickerAsync(ProductType.BtcEur);
            return ep.Price;
        }
    }
}
