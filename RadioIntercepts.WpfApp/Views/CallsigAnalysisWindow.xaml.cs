using RadioIntercepts.Application.Services;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.WpfApp.ViewModels;
using RadioIntercepts.WpfApp.Services;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace RadioIntercepts.WpfApp.Views
{
    public partial class CallsignAnalysisWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public CallsignAnalysisWindow(
            IServiceProvider serviceProvider,
            IChartService chartService)
        {
            _serviceProvider = serviceProvider;

            InitializeComponent();

            // Создаем ViewModel с действием для открытия окна
            DataContext = new CallsignAnalysisViewModel(
                serviceProvider.GetRequiredService<AppDbContext>(),
                chartService,
                OpenMessagesWindow);
        }

        private void OpenMessagesWindow(string primaryCallsign, string secondaryCallsign)
        {
            if (string.IsNullOrWhiteSpace(primaryCallsign) || string.IsNullOrWhiteSpace(secondaryCallsign))
                return;

            // Создаем новое окно с новым экземпляром DbContext через ServiceProvider
            var messagesWindow = new MessagesWindow(_serviceProvider, primaryCallsign, secondaryCallsign)
            {
                Owner = this,
                Title = $"Сообщения: {primaryCallsign} & {secondaryCallsign}"
            };

            messagesWindow.Show();
        }

        // Обработчик двойного клика для DataGrid
        private void AssociatedCallsignsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is CallsignAnalysisViewModel viewModel)
            {
                viewModel.OpenAssociatedMessagesCommand.Execute(null);
            }
        }
    }
}