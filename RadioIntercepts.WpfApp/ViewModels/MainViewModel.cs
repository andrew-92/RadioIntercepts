using CommunityToolkit.Mvvm.ComponentModel;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Repositories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace RadioIntercepts.WpfApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly MessageRepository _repository;

        [ObservableProperty]
        private ObservableCollection<Message> messages;

        public MainViewModel(MessageRepository repository)
        {
            _repository = repository;
            LoadMessages();
        }

        private async void LoadMessages()
        {
            var list = await _repository.GetAllAsync();
            Messages = new ObservableCollection<Message>(list);
        }
    }
}
