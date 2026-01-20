using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace RadioIntercepts.WpfApp.Views
{
    public partial class DashboardWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public DashboardWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
        }

        private void CallsignAnalysisButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var callsignWindow = scope.ServiceProvider.GetRequiredService<CallsignAnalysisWindow>();
                callsignWindow.Owner = this;
                callsignWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var statisticsWindow = scope.ServiceProvider.GetRequiredService<StatisticsWindow>();
                statisticsWindow.Owner = this;
                statisticsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CallsignStatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                // Замените YourNewWindow на имя вашего нового окна
                var newWindow = scope.ServiceProvider.GetRequiredService<CallsignStatisticsWindow>();
                newWindow.Owner = this;
                newWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}