using Microsoft.Extensions.DependencyInjection;
using RadioIntercepts.Core.Interfaces;
using System;
using System.Windows;

namespace RadioIntercepts.WpfApp.Views
{
    public partial class MainWindow : Window
    {
        private readonly IMessageRepository _repository;
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(IMessageRepository repository, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        } 

        private void ShowAllMessages_Click(object sender, RoutedEventArgs e)
        {
            var window = _serviceProvider.GetRequiredService<MessagesWindow>();
            window.ShowDialog();
        }
        private void AddEditMessage_Click(object sender, RoutedEventArgs e)
        {
            var window = _serviceProvider.GetRequiredService<MessageEditWindow>();
            window.ShowDialog();
        }

        private void ParseMessages_Click(object sender, RoutedEventArgs e)
        {
            var window = _serviceProvider.GetRequiredService<MessageParserWindow>();
            window.ShowDialog();
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            var window = _serviceProvider.GetRequiredService<DashboardWindow>();
            window.ShowDialog();
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            var window = _serviceProvider.GetRequiredService<ReportsWindow>();
            window.ShowDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
