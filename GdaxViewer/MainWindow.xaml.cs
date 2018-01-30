using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GdaxViewer.Models;
using GdaxViewer.Properties;
using GDAXClient.Authentication;
using GDAXClient.Services.Orders;
using GDAXClient.Services.Orders.Models;
using GDAXClient.Services.Orders.Models.Responses;
using GDAXClient.Shared;

namespace GdaxViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static GdaxService _gdaxService;
        private static IList<OrderResponse> _openOrders;
        private decimal _eurBalance;
        public MainWindow()
        {
            InitializeComponent();
            var settings = Settings.Default;
            if (settings.ApiKey == "" || settings.ApiSecret == "" || settings.ApiPassPhrase == "")
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.ShowDialog();
            }
            var marketFetcher = new GdaxFetcher();
            var auth = new Authenticator(Settings.Default.ApiKey, Settings.Default.ApiSecret, Settings.Default.ApiPassPhrase);
            _gdaxService = new GdaxService(auth);

            marketFetcher.ScheduleTask(this, _gdaxService);
            marketFetcher.Execute(null);
        }


        public void UpdateWindow(GdaxOverview gdaxOverview)
        {
            BtcEurPrice.Content = BtcEurPrice.Content.ToString() == "" ? 0 : BtcEurPrice.Content;
            StaleBtc.Content = StaleBtc.Content.ToString() == "" ? 0 : StaleBtc.Content;
            var color = Convert.ToDecimal(BtcEurPrice.Content.ToString().TrimStart('€')) < Math.Round(gdaxOverview.BtcEurTicker.Price, 2) ? Colors.Red : Colors.LawnGreen;
            BtcEurPrice.Foreground = new SolidColorBrush(color);
            BtcEurPrice.Content = "€" + Math.Round(gdaxOverview.BtcEurTicker.Price, 2);
            BtcVolume.Content = "฿" + Math.Round(gdaxOverview.BtcEurTicker.Volume, 2);
            _eurBalance = gdaxOverview.Finances.First(x => x.Currency == "EUR").Balance;
            TotalEur.Content = "€" + Math.Round(_eurBalance, 2);
            TotalBtc.Content = "฿" + Math.Round(gdaxOverview.Finances.First(x => x.Currency == "BTC").Balance, 6);
            _openOrders = gdaxOverview.OpenOrders.First().Where(x => x.Side == "sell").ToList();
            var returns = _openOrders.Sum(fill => fill.Size * fill.Price) + _eurBalance;
            AnticipatedReturns.Content = "€" + Math.Round(returns, 2);
            StaleBtc.Content = "฿" + Math.Round(gdaxOverview.Finances.First(x => x.Currency == "BTC").Available, 6);
            StaleBtc.Foreground = new SolidColorBrush((Convert.ToDecimal(StaleBtc.Content.ToString().TrimStart('฿')) > 0) ? Colors.Red : Colors.LawnGreen);
            FilledOrders.Items.Clear();

            foreach (var fill in gdaxOverview.Fills.First())
            {
                var item = new ListBoxItem
                {
                    Content = fill.Side + "\t" + fill.Size + "\t" + fill.Price,
                    Tag = fill.Order_id
                };
                FilledOrders.Items.Add(item);
            }
            OpenOrders.Items.Clear();
            foreach (var openOrder in gdaxOverview.OpenOrders.First())
            {
                var item = new ListBoxItem
                {
                    Content = openOrder.Side + "\t" + openOrder.Size + "\t" + openOrder.Price,
                    Tag = openOrder.Price
                };
                OpenOrders.Items.Add(item);
            }
        }

        private void PercClick(object sender, RoutedEventArgs e)
        {
            var btn = (Button) sender;
            var amount = Convert.ToDouble(Calc.Text);
            var perc = Convert.ToDouble(btn.Tag);
            var total = Math.Round(amount + (amount * (perc / 100)), 2);
            CalcTotal.Content = "€" + total + $" @ {perc}%";
            CalcTotal.Tag = total;
        }

        private void FilledOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = (ListBox) sender;
            if (((ListBoxItem) lb.SelectedItem) == null) return;
            var value = Math.Round(Convert.ToDecimal(((ListBoxItem) lb.SelectedItem).Tag), 2);
            Calc.Text = value.ToString(CultureInfo.InvariantCulture);
            PercClick(BtnOnePerc, e);
        }

        private void OpenOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MessageBox.Show("Are you sure you want to cancel this order?", "Cancel Order", MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            
            var lb = (ListBox)sender;
            if ((ListBoxItem)lb.SelectedItem == null) return;
            var value = ((ListBoxItem)lb.SelectedItem).Tag.ToString();
            
            _gdaxService.CancelOrder(value);
            Calc.Text = value.ToString(CultureInfo.InvariantCulture);
            PercClick(BtnOnePerc, e);
        }

        private void CalcTotal_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(CalcTotal.Tag.ToString());
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Environment.Exit(0);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void CancelAllOrders(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to cancel all orders?", "Cancel Order", MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if(result == MessageBoxResult.Yes)
                _gdaxService.CancelAllOrders();
        }

        private void CalculateBailout(object sender, RoutedEventArgs e)
        {
            var total = _openOrders.Sum(fill => fill.Size * Convert.ToDecimal(BailoutRate.Text)) + _eurBalance;

            var dayStart = Convert.ToDecimal(DayStartBalance.Text);
            var perc = ((total - dayStart) / dayStart) * 100;
            BailoutTotal.Content = $"€{Math.Round(total,2)}\t{Math.Round(perc,2)}%";
        }

        private void PlaceOrder(object sender, RoutedEventArgs e)
        {
            var btn = (Button) sender;
            if (btn == null) return;
            OrderSize.Text = "";
            OrderPrice.Text = "";
            Enum.TryParse(btn.Tag.ToString(), out OrderSide side);
            var order = _gdaxService.OrdersService.PlaceLimitOrderAsync(side, ProductType.BtcEur,
                Convert.ToDecimal(OrderSize.Text), Convert.ToDecimal(OrderPrice.Text));

        }

        private void QuickBtc(object sender, RoutedEventArgs e)
        {
            var btn = (Button) sender;
            if (btn == null) return;
            var btnTag = btn.Tag.ToString();
            string value;
            if(btnTag.Contains("%"))
            {
                var s = btnTag.TrimEnd('%');
                value = Math.Round(_eurBalance * Convert.ToDecimal(s)/100,6).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                value = btnTag;
            }
            OrderSize.Text = value;
        }
    }
}
