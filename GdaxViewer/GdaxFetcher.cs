using System;
using System.Linq;
using System.Threading;
using GdaxViewer.Models;
using GdaxViewer.Properties;
using GDAXClient.Authentication;
using GDAXClient.Shared;
using Quartz;
using Quartz.Impl;

namespace GdaxViewer
{
    class GdaxFetcher : IJob
    {

        private static GdaxService _gdaxService;

        private static MainWindow _mainWindow;
        public void ScheduleTask(MainWindow mainWindow, GdaxService gdaxService)
        {
            _mainWindow = mainWindow;
            _gdaxService = gdaxService;
            var scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.Start();
            var job = JobBuilder.Create<GdaxFetcher>().Build();
            var dt = DateTime.UtcNow.AddSeconds(1);

            var trigger = TriggerBuilder.Create()
                .WithDailyTimeIntervalSchedule
                (s =>
                    s.WithIntervalInSeconds(5)
                        .OnEveryDay()
                        .StartingDailyAt(TimeOfDay.HourMinuteAndSecondOfDay(dt.Hour, dt.Minute, dt.Second))
                )
                .Build();

            scheduler.ScheduleJob(job, trigger);
        }

        public async void Execute(IJobExecutionContext context)
        {
            try
            {
                var btcEurTicker = await _gdaxService.ProductsService.GetProductTickerAsync(ProductType.BtcEur);
                Thread.Sleep(300);
                var openOrders = await _gdaxService.OrdersService.GetAllOrdersAsync(20);
                Thread.Sleep(300);
                var fills = await _gdaxService.FillsService.GetFillsByProductIdAsync(ProductType.BtcEur);
                Thread.Sleep(300);
                var accounts = await _gdaxService.AccountsService.GetAllAccountsAsync();
                Thread.Sleep(300);
                var finances = accounts.Where(x => x.Currency == "EUR" || x.Currency == "BTC");

                App.Current.Dispatcher.Invoke(delegate
                {
                    _mainWindow.UpdateWindow(new GdaxOverview(btcEurTicker, openOrders, fills, finances));
                });
            }
            catch (Exception )
            {
                //
            }
        }
    }
}
