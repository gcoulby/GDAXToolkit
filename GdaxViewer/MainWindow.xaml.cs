using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CefSharp;
using CefSharp.Wpf;
using GdaxViewer.Models;
using GdaxViewer.Properties;
using GDAXClient.Authentication;
using GDAXClient.Services.Fills.Models.Responses;
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
        private decimal _eurAvailableBalance;
        private decimal _eurPrice;
        private decimal _staleBtc;
        private static ChromiumWebBrowser _webBrowser;
        
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

            Cef.Initialize();
            _webBrowser = new ChromiumWebBrowser()
            {
                Address = "https://uk.tradingview.com/chart/KZJtkOWA/",
                Height = 300,
                Width = 520,
                Margin = new Thickness(0, 0, 10, 10),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                
            };
            MainGrid.Children.Add(_webBrowser);
            _webBrowser.Visibility = Visibility.Hidden;
            
            marketFetcher.ScheduleTask(this, _gdaxService);
            marketFetcher.Execute(null);
        }


        public void UpdateWindow(GdaxOverview gdaxOverview)
        {
            BtcEurPrice.Content = BtcEurPrice.Content.ToString() == "" ? 0 : BtcEurPrice.Content;
            StaleBtc.Content = StaleBtc.Content.ToString() == "" ? 0 : StaleBtc.Content;
            var btcEurPrice = ConvertToDecimal(BtcEurPrice.Content.ToString().TrimStart('€'));
            if (btcEurPrice == -1) return;
            var color = btcEurPrice < Math.Round(gdaxOverview.BtcEurTicker.Price, 2) ? Colors.Red : Colors.LawnGreen;
            BtcEurPrice.Foreground = new SolidColorBrush(color);
            _eurPrice = Math.Round(gdaxOverview.BtcEurTicker.Price, 2);
            BtcEurPrice.Content = "€" + _eurPrice;
            BtcVolume.Content = "฿" + Math.Round(gdaxOverview.BtcEurTicker.Volume, 2);
            _eurBalance = gdaxOverview.Finances.First(x => x.Currency == "EUR").Balance;
            _eurAvailableBalance = gdaxOverview.Finances.First(x => x.Currency == "EUR").Available;
            TotalEur.Content = "€" + Math.Round(_eurBalance, 2);
            AvailableEur.Content = "€" + Math.Round(_eurAvailableBalance, 2);
            TotalBtc.Content = "฿" + Math.Round(gdaxOverview.Finances.First(x => x.Currency == "BTC").Balance, 10);
            _openOrders = gdaxOverview.OpenOrders.First().Where(x => x.Side == "sell").ToList();
            var returns = _openOrders.Sum(fill => fill.Size * fill.Price) + _eurBalance;
            AnticipatedReturns.Content = "€" + Math.Round(returns, 2);
            _staleBtc = Math.Round(gdaxOverview.Finances.First(x => x.Currency == "BTC").Available, 10);
            StaleBtc.Content = "฿" + _staleBtc;
            StaleBtc.Foreground = new SolidColorBrush((Convert.ToDecimal(StaleBtc.Content.ToString().TrimStart('฿')) > 0) ? Colors.Red : Colors.LawnGreen);
            FilledOrders.Items.Clear();

            foreach (var fill in gdaxOverview.Fills.First())
            {
                var item = new ListBoxItem
                {
                    Content = fill.Side + "\t" + fill.Size + "\t" + fill.Price,
                    Tag = fill
                };
                FilledOrders.Items.Add(item);
            }
            OpenOrders.Items.Clear();
            foreach (var openOrder in gdaxOverview.OpenOrders.First())
            {
                var item = new ListBoxItem
                {
                    Content = openOrder.Side + "\t" + openOrder.Size + "\t" + openOrder.Price,
                    Tag = openOrder
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

            if (e?.Source.GetType() == typeof(FillResponse))
            {
                CalcTotal.Tag = e.Source;  
            }
            
        }

        private void FilledOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = (ListBox) sender;
            if (((ListBoxItem) lb.SelectedItem) == null) return;
            var fillResponse = (FillResponse)((ListBoxItem) lb.SelectedItem).Tag;
            var value = Math.Round(fillResponse.Price, 2);
            Calc.Text = value.ToString(CultureInfo.InvariantCulture);
            e.Source = fillResponse;
            PercClick(BtnOnePerc, e);
        }

        private void OpenOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = (ListBox)sender;
            if ((ListBoxItem)lb.SelectedItem == null) return;
            var orderResponse = (OrderResponse)((ListBoxItem)lb.SelectedItem).Tag;
            lb.SelectedItem = null;

            var result = MessageBox.Show("Are you sure you want to cancel this order?", "Cancel Order", MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No) return;
            
            _gdaxService.CancelOrder(orderResponse.Id.ToString());
            Calc.Text = orderResponse.Price.ToString(CultureInfo.InvariantCulture);
            e.Source = orderResponse;
            PercClick(BtnOnePerc, e);
        }

        private void CalcTotal_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var fillResponse = (FillResponse)CalcTotal.Tag;
            Clipboard.SetText(fillResponse.Price.ToString(CultureInfo.InvariantCulture));
            BailoutRate.Text = fillResponse.Price.ToString(CultureInfo.InvariantCulture);
            OrderSize.Text = fillResponse.Size.ToString(CultureInfo.InvariantCulture);
            OrderPrice.Text = (Math.Round(fillResponse.Price + (fillResponse.Price * (decimal) 0.01),2)).ToString(CultureInfo.InvariantCulture);
        }

        private void StaleBtcMouseDown(object sender, MouseButtonEventArgs e)
        {
            OrderSize.Text = _staleBtc.ToString(CultureInfo.InvariantCulture);
        }

        protected override void OnClosed(EventArgs e)
        {
            Cef.Shutdown();
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

            if (result == MessageBoxResult.No) return;

            _gdaxService.CancelAllOrders();
        }

        private void CalculateBailout(object sender, RoutedEventArgs e)
        {
            var total = _openOrders.Sum(fill => fill.Size * Convert.ToDecimal(BailoutRate.Text)) + _eurBalance;

            if (DayStartBalance.Text == "")
            {
                MessageBox.Show($"You must enter a 'Day Start' balance.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            var dayStart = ConvertToDecimal(DayStartBalance.Text);
            if (dayStart == -1) return;
            var perc = ((total - dayStart) / dayStart) * 100;
            BailoutTotal.Content = $"€{Math.Round(total,2)}\t{Math.Round(perc,2)}%";
        }

        private void PlaceOrder(object sender, RoutedEventArgs e)
        {
            var sizeText = OrderSize.Text;
            var priceText = OrderPrice.Text;
            OrderSize.Text = "";
            OrderPrice.Text = "";
            var btn = (Button) sender;
            if (btn == null) return;

            decimal size = ConvertToDecimal(sizeText), price = ConvertToDecimal(priceText);
            if (size == -1 || price == -1) return;
            
            Enum.TryParse(btn.Tag.ToString(), out OrderSide side);
            if (side == OrderSide.Buy && price >= _eurPrice || side == OrderSide.Sell && price <= _eurPrice)
            {
                MessageBox.Show($"Your order is on the wrong side of the market.\r\nThis will result in the limit order being converted into a market order and you will be charge a fee.\r\nThis order will not be placed.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (Math.Abs(((_eurPrice-price)/_eurPrice)*100) >= 20)
            {
                MessageBox.Show($"The price you have set is more than 20% away from the market.\r\nThis order will not be placed.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (side == OrderSide.Sell && size > _staleBtc || side == OrderSide.Buy && (size * price) > _eurAvailableBalance)
            {
                MessageBox.Show($"Insufficient Funds.\r\nThis order will not be placed.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }


            var result = MessageBox.Show($"Are you sure you want to place a {btn.Tag} order?", "Place Order", MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.No) return;

            var order = _gdaxService.OrdersService.PlaceLimitOrderAsync(side, ProductType.BtcEur, size, price);
        }

        private void QuickBtc(object sender, RoutedEventArgs e)
        {
            var btn = (Button) sender;
            if (btn == null) return;
            var btnTag = btn.Tag.ToString();
            string value;
            if(btnTag.Contains("%"))
            {
                var priceText = OrderPrice.Text;
                var price = ConvertToDecimal(priceText);
                if (price == -1) return;
                var s = btnTag.TrimEnd('%');
                var tag = Convert.ToDecimal(s);
                if (tag == -1) return;
                value = Math.Round((((_eurBalance - (decimal)0.8) / price) * tag) / 100, 10).ToString(CultureInfo.InvariantCulture); 
            }
            else
            {
                value = btnTag;
            }
            OrderSize.Text = value;
        }

        private decimal ConvertToDecimal(string d, bool supressMessages = false)
        {
            try
            {
                return Convert.ToDecimal(d);
            }
            catch (FormatException)
            {
                if (!supressMessages)
                    MessageBox.Show($"The number you have entered is not a valid input.", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
            }
            catch (OverflowException)
            {
                if (!supressMessages)
                    MessageBox.Show($"The number you have entered is outside the bounds of a decimal number.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            return -1;
        }

        private void ApproxValue(object sender, TextChangedEventArgs e)
        {
            var sizeText = OrderSize.Text;
            var priceText = OrderPrice.Text;
            decimal size = ConvertToDecimal(sizeText, true), price = ConvertToDecimal(priceText, true);
            if (size == -1 || price == -1) return;

            ApproxBuy.Content = $"≈ €{Math.Round((size * price), 2)}";
        }

        private void ToggleOnChart(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            if (btn == null) return;
            var btnTag = btn.Tag.ToString();
            if (btnTag == "off")
            {
                _webBrowser.Visibility = Visibility.Hidden;
                btn.Content = "◉";
                btn.Tag = "on";
            }
            else if (btnTag == "on")
            {
                _webBrowser.Visibility = Visibility.Visible;
                btn.Content = "◎";
                btn.Tag = "off";
            }
        }
    }
}
