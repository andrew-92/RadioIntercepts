using RadioIntercepts.Core.Interfaces;
using System.Windows;

namespace RadioIntercepts.WpfApp.Views
{
    public partial class ReportsWindow : Window
    {
        private readonly IMessageRepository _repository;

        public ReportsWindow(IMessageRepository repository)
        {
            InitializeComponent();
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        // Логика отчетов с использованием _repository
        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            // TODO: логика сохранения сообщения
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
