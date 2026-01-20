using Microsoft.Win32;
using RadioIntercepts.Application.Parsers;
using RadioIntercepts.Application.Parsers.Sources;
using RadioIntercepts.Application.Services;
using RadioIntercepts.Infrastructure.Repositories;
using System.Windows;
using System.Threading.Tasks;

namespace RadioIntercepts.WpfApp.Views
{
    public partial class MessageParserWindow : Window
    {
        private readonly IMessageRepository _repository;
        private readonly IMessageProcessingService _processingService;

        public MessageParserWindow(IMessageRepository repository, IMessageProcessingService processingService)
        {
            InitializeComponent();
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _processingService = processingService ?? throw new ArgumentNullException(nameof(processingService));
        }

        private void Parse_Click(object sender, RoutedEventArgs e)
        {
            // TODO: логика сохранения сообщения
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // TODO: логика сохранения сообщения
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void LoadFromWhatsappFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() != true) return;

            // Используем новую архитектуру
            var dataSource = new WhatsappDataSource();
            var parser = new WhatsappRadioMessageParser();

            var messages = await _processingService.ProcessAsync(dataSource, parser, openFileDialog.FileName);

            foreach (var msg in messages)
            {
                if (msg != null)
                    await _repository.AddAsync(msg);
            }

            MessageBox.Show($"{messages.Count} сообщений успешно распарсено и сохранено!");
        }
    }
}