using RadioIntercepts.Infrastructure.Repositories;
using System;
using System.Windows;

namespace RadioIntercepts.WpfApp.Views
{
    public partial class MessageEditWindow : Window
    {
        private readonly IMessageRepository _repository;

        public MessageEditWindow(IMessageRepository repository)
        {
            InitializeComponent();
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        // Тут остальной код окна
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // TODO: логика сохранения сообщения
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // TODO: логика сохранения сообщения
        }
    }
}
