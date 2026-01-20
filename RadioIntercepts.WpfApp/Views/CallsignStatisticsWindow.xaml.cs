using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.WpfApp.ViewModels;
using System.Windows;

namespace RadioIntercepts.WpfApp.Views
{
    public partial class CallsignStatisticsWindow : Window
    {
        public CallsignStatisticsWindow(AppDbContext context)
        {
            InitializeComponent();
            DataContext = new CallsignStatisticsViewModel(context);
        }
    }
}