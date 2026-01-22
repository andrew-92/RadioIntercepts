using Microsoft.Extensions.DependencyInjection;
using RadioIntercepts.WpfApp.Views;
using System.Windows;

namespace RadioIntercepts.WpfApp
{

    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //MessageBox.Show("OnStartup started"); // ← Проверка запуска

            var serviceProvider = WpfApp.Startup.ConfigureServices();

            //MessageBox.Show("DI configured"); // ← Проверка создания ServiceProvider

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();

            //MessageBox.Show("MainWindow resolved"); // ← Проверка разрешения MainWindow

            mainWindow.Show();
        }
    }

}