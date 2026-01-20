using System.Windows;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.WpfApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace RadioIntercepts.WpfApp.Views
{
    public partial class MessagesWindow : Window
    {
        public MessagesWindow(IServiceProvider serviceProvider, string primaryCallsign = null, string secondaryCallsign = null)
        {
            InitializeComponent();

            // Создаем новый экземпляр DbContext через ServiceProvider
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            if (!string.IsNullOrEmpty(primaryCallsign) && !string.IsNullOrEmpty(secondaryCallsign))
            {
                // Создаем ViewModel для двух позывных
                DataContext = new MessagesViewModel(context, primaryCallsign, secondaryCallsign);
            }
            else
            {
                // Обычный режим
                DataContext = new MessagesViewModel(context);
            }
        }
    }
}