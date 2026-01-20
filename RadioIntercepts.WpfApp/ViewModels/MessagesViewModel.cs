using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RadioIntercepts.WpfApp.ViewModels
{
    public partial class MessagesViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        public ObservableCollection<Message> Messages { get; } = new();
        public ICollectionView MessagesView { get; }

        private const int PageSize = 50;
        private int _page = 1;

        public int Page
        {
            get => _page;
            set => SetProperty(ref _page, value);
        }

        public ObservableCollection<Area> Areas { get; } = new();
        public ObservableCollection<string> Frequencies { get; } = new();
        public ObservableCollection<string> Callsigns { get; } = new();

        [ObservableProperty] private string? filterArea;
        [ObservableProperty] private string? filterFrequency;
        [ObservableProperty] private string? filterCallsign;
        [ObservableProperty] private DateTime? filterDateFrom;
        [ObservableProperty] private DateTime? filterDateTo;

        // Новые свойства для двух позывных
        public string PrimaryCallsign { get; set; }
        public string SecondaryCallsign { get; set; }
        public bool IsDualCallsignMode { get; set; }

        public MessagesViewModel(AppDbContext context, string primaryCallsign = null, string secondaryCallsign = null)
        {
            _context = context;

            MessagesView = CollectionViewSource.GetDefaultView(Messages);

            if (!string.IsNullOrEmpty(primaryCallsign) && !string.IsNullOrEmpty(secondaryCallsign))
            {
                PrimaryCallsign = primaryCallsign;
                SecondaryCallsign = secondaryCallsign;
                IsDualCallsignMode = true;
                FilterCallsign = $"{primaryCallsign} & {secondaryCallsign}";
            }

            _ = InitializeAsync();
        }

        public MessagesViewModel(AppDbContext context) : this(context, null, null) { }

        private async Task InitializeAsync()
        {
            await LoadFilterDictionariesAsync();

            if (IsDualCallsignMode)
            {
                await SearchAsync();
            }
            else
            {
                await LoadAsync();
            }
        }

        private async Task LoadFilterDictionariesAsync()
        {
            Areas.Clear();
            foreach (var a in await _context.Areas
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync())
                Areas.Add(a);

            Frequencies.Clear();
            foreach (var f in await _context.Frequencies
                .AsNoTracking()
                .OrderBy(x => x.Value)
                .Select(x => x.Value)
                .ToListAsync())
                Frequencies.Add(f);

            Callsigns.Clear();
            foreach (var c in await _context.Callsigns
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync())
                Callsigns.Add(c);
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            Page = 1;
            await LoadAsync();
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            Page++;
            await LoadAsync();
        }

        [RelayCommand]
        private async Task PrevPageAsync()
        {
            if (Page > 1)
            {
                Page--;
                await LoadAsync();
            }
        }

        private async Task LoadAsync()
        {
            IQueryable<Message> query = _context.Messages
                .AsNoTracking()
                .Include(x => x.Area)
                .Include(x => x.Frequency)
                .Include(x => x.MessageCallsigns)
                    .ThenInclude(x => x.Callsign);

            if (!string.IsNullOrWhiteSpace(FilterArea))
            {
                var selectedArea = Areas.FirstOrDefault(a => a.Name == FilterArea);
                if (selectedArea != null)
                {
                    query = query.Where(x => x.Area.Key == selectedArea.Key);
                }
                else
                {
                    query = query.Where(x => x.Area.Name.Contains(FilterArea));
                }
            }

            if (!string.IsNullOrWhiteSpace(FilterFrequency))
                query = query.Where(x => x.Frequency.Value.Contains(FilterFrequency));

            // Логика для двух позывных
            if (IsDualCallsignMode && !string.IsNullOrWhiteSpace(PrimaryCallsign) &&
                !string.IsNullOrWhiteSpace(SecondaryCallsign))
            {
                // Ищем сообщения, где есть ОБА позывных
                query = query.Where(x =>
                    x.MessageCallsigns.Any(mc => mc.Callsign.Name == PrimaryCallsign) &&
                    x.MessageCallsigns.Any(mc => mc.Callsign.Name == SecondaryCallsign));
            }
            // Старая логика для одного позывного
            else if (!string.IsNullOrWhiteSpace(FilterCallsign) && !IsDualCallsignMode)
            {
                query = query.Where(x =>
                    x.MessageCallsigns.Any(cs =>
                        cs.Callsign.Name.Contains(FilterCallsign)));
            }

            if (FilterDateFrom.HasValue)
                query = query.Where(x => x.DateTime >= FilterDateFrom.Value);

            if (FilterDateTo.HasValue)
                query = query.Where(x => x.DateTime <= FilterDateTo.Value);

            var data = await query
                .OrderByDescending(x => x.DateTime)
                .Skip((Page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            Messages.Clear();
            foreach (var msg in data)
                Messages.Add(msg);
        }
    }
}