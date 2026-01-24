using DocumentFormat.OpenXml.InkML;
using RadioIntercepts.Analysis.Interfaces.Services;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.WpfApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RadioIntercepts.WpfApp.Views
{
    /// <summary>
    /// Interaction logic for GraphWindow.xaml
    /// </summary>
    public partial class GraphWindow : Window
    {
        public GraphWindow(IAdvancedGraphAnalysisService service, AppDbContext context)
        {
            InitializeComponent();
            DataContext = new GraphViewModel(service, context);
        }
    }
}
