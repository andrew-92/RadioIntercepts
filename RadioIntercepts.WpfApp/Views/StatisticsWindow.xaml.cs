using RadioIntercepts.Application.Services;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.WpfApp.ViewModels;
using RadioIntercepts.WpfApp.Services;
using System.Windows;

namespace RadioIntercepts.WpfApp.Views
{
    public partial class StatisticsWindow : Window
    {
        public StatisticsWindow(AppDbContext context, IChartService chartService)
        {
            InitializeComponent();
            DataContext = new StatisticsViewModel(context, chartService);
        }
    }
}