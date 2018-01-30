using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GdaxViewer.Properties;

namespace GdaxViewer
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            ApiKey.Text = Settings.Default.ApiKey;
            ApiSecret.Password = Settings.Default.ApiSecret;
            ApiPassPhrase.Text = Settings.Default.ApiPassPhrase;
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            bool b = CanSaveSettings().Result;
            this.Close();
        }

        private async Task<bool> CanSaveSettings()
        {
            var canSave = true;
            if (string.IsNullOrEmpty(ApiKey.Text))
            {
                ApiKey.BorderBrush = new SolidColorBrush(Colors.Red);
                canSave = false;
            }
            if (string.IsNullOrEmpty(ApiSecret.Password))
            {
                ApiSecret.BorderBrush = new SolidColorBrush(Colors.Red);
                canSave = false;
            }
            if (string.IsNullOrEmpty(ApiPassPhrase.Text))
            {
                ApiPassPhrase.BorderBrush = new SolidColorBrush(Colors.Red);
                canSave = false;
            }

            //canSave = await GdaxService.CanConnect(ApiKey.Text, ApiSecret.Password, ApiPassPhrase.Text);

            if (!canSave) return false;

            Settings.Default.ApiKey = ApiKey.Text;
            Settings.Default.ApiSecret = ApiSecret.Password;
            Settings.Default.ApiPassPhrase = ApiPassPhrase.Text;
            Settings.Default.Save();
            return true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (CanSaveSettings().Result)
            {
                base.OnClosing(e);
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
