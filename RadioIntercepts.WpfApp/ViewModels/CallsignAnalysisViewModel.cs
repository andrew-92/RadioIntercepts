using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Application.Services;
using RadioIntercepts.Core.Charts;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.WpfApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RadioIntercepts.WpfApp.ViewModels
{
    public partial class CallsignAnalysisViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly IChartService _chartService;
        private readonly Action<string, string> _openMessagesWindowAction;

        [ObservableProperty]
        private ObservableCollection<string> _callsigns = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
        private string? _selectedCallsign;

        [ObservableProperty]
        private Callsign? _callsignInfo;

        [ObservableProperty]
        private Message? _firstMessage;

        [ObservableProperty]
        private Message? _lastMessage;

        [ObservableProperty]
        private int _totalMessagesCount;

        [ObservableProperty]
        private ChartData? _dayOfWeekChart;

        [ObservableProperty]
        private ChartData? _hourChart;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _showAnalysisPanel = false;

        [ObservableProperty]
        private bool _showFirstMessage = false;

        [ObservableProperty]
        private bool _showLastMessage = false;

        [ObservableProperty]
        private bool _showDayChart = false;

        [ObservableProperty]
        private bool _showHourChart = false;

        private ObservableCollection<AssociatedCallsignViewModel> _allAssociatedCallsigns = new();

        [ObservableProperty]
        private ObservableCollection<AssociatedCallsignViewModel> _filteredAssociatedCallsigns = new();

        [ObservableProperty]
        private bool _showAssociatedCallsigns = false;

        [ObservableProperty]
        private string _associatedCallsignsFilter = string.Empty;

        [ObservableProperty]
        private AssociatedCallsignViewModel? _selectedAssociatedCallsign;

        [ObservableProperty]
        private int _filteredAssociatedCallsignsCount = 0;

        [ObservableProperty]
        private int _totalAssociatedCallsignsCount = 0;

        public CallsignAnalysisViewModel(
            AppDbContext context,
            IChartService chartService,
            Action<string, string> openMessagesWindowAction = null)
        {
            _context = context;
            _chartService = chartService;
            _openMessagesWindowAction = openMessagesWindowAction;
            _ = LoadCallsignsAsync();
        }

        private async Task LoadCallsignsAsync()
        {
            try
            {
                var callsignsList = await _context.Callsigns
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .Select(c => c.Name)
                    .ToListAsync();

                Callsigns.Clear();
                foreach (var callsign in callsignsList)
                {
                    Callsigns.Add(callsign);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки позывных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanAnalyze))]
        private async Task AnalyzeAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedCallsign))
                return;

            IsLoading = true;
            ResetAnalysisData();

            try
            {
                CallsignInfo = await _context.Callsigns
                    .FirstOrDefaultAsync(c => c.Name == SelectedCallsign);

                if (CallsignInfo == null)
                {
                    MessageBox.Show($"Позывной '{SelectedCallsign}' не найден",
                        "Информация", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                ShowAnalysisPanel = true;

                var messagesQuery = _context.MessageCallsigns
                    .Include(mc => mc.Message)
                        .ThenInclude(m => m.Area)
                    .Include(mc => mc.Message)
                        .ThenInclude(m => m.Frequency)
                    .Where(mc => mc.Callsign.Name == SelectedCallsign)
                    .Select(mc => mc.Message);

                FirstMessage = await messagesQuery
                    .OrderBy(m => m.DateTime)
                    .FirstOrDefaultAsync();

                LastMessage = await messagesQuery
                    .OrderByDescending(m => m.DateTime)
                    .FirstOrDefaultAsync();

                TotalMessagesCount = await messagesQuery.CountAsync();

                ShowFirstMessage = FirstMessage != null;
                ShowLastMessage = LastMessage != null;

                await LoadAssociatedCallsignsAsync();

                if (TotalMessagesCount > 0)
                {
                    DayOfWeekChart = await _chartService.GetCallsignActivityByDayOfWeekAsync(SelectedCallsign);
                    HourChart = await _chartService.GetCallsignActivityByHourAsync(SelectedCallsign);

                    ShowDayChart = DayOfWeekChart != null;
                    ShowHourChart = HourChart != null;
                }
                else
                {
                    ShowDayChart = false;
                    ShowHourChart = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка анализа: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                ShowAnalysisPanel = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAssociatedCallsignsAsync()
        {
            try
            {
                var messageIds = await _context.MessageCallsigns
                    .Where(mc => mc.Callsign.Name == SelectedCallsign)
                    .Select(mc => mc.MessageId)
                    .Distinct()
                    .ToListAsync();

                var associatedCallsigns = await _context.MessageCallsigns
                    .Where(mc => messageIds.Contains(mc.MessageId))
                    .Where(mc => mc.Callsign.Name != SelectedCallsign)
                    .GroupBy(mc => mc.Callsign.Name)
                    .Select(g => new AssociatedCallsignViewModel
                    {
                        Callsign = g.Key,
                        MessageCount = g.Count()
                    })
                    .OrderByDescending(x => x.MessageCount)
                    .ThenBy(x => x.Callsign)
                    .ToListAsync();

                _allAssociatedCallsigns.Clear();
                foreach (var item in associatedCallsigns)
                {
                    _allAssociatedCallsigns.Add(item);
                }

                TotalAssociatedCallsignsCount = _allAssociatedCallsigns.Count;

                ApplyAssociatedCallsignsFilter();

                ShowAssociatedCallsigns = _allAssociatedCallsigns.Any();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки связанных позывных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                ShowAssociatedCallsigns = false;
            }
        }

        private void ApplyAssociatedCallsignsFilter()
        {
            if (string.IsNullOrWhiteSpace(AssociatedCallsignsFilter))
            {
                FilteredAssociatedCallsigns.Clear();
                foreach (var item in _allAssociatedCallsigns)
                {
                    FilteredAssociatedCallsigns.Add(item);
                }
                FilteredAssociatedCallsignsCount = FilteredAssociatedCallsigns.Count;
            }
            else
            {
                var filter = AssociatedCallsignsFilter.ToLower();

                FilteredAssociatedCallsigns.Clear();
                foreach (var item in _allAssociatedCallsigns)
                {
                    if (item.Callsign.ToLower().Contains(filter))
                    {
                        FilteredAssociatedCallsigns.Add(item);
                    }
                }
                FilteredAssociatedCallsignsCount = FilteredAssociatedCallsigns.Count;
            }
        }

        [RelayCommand]
        private void ClearAssociatedCallsignsFilter()
        {
            AssociatedCallsignsFilter = string.Empty;
        }

        [RelayCommand]
        private void OpenAssociatedMessages()
        {
            if (SelectedAssociatedCallsign == null || string.IsNullOrWhiteSpace(SelectedCallsign))
                return;

            // Вызываем действие для открытия окна
            _openMessagesWindowAction?.Invoke(SelectedCallsign, SelectedAssociatedCallsign.Callsign);
        }

        partial void OnAssociatedCallsignsFilterChanged(string value)
        {
            ApplyAssociatedCallsignsFilter();
        }

        private void ResetAnalysisData()
        {
            CallsignInfo = null;
            FirstMessage = null;
            LastMessage = null;
            TotalMessagesCount = 0;
            DayOfWeekChart = null;
            HourChart = null;

            _allAssociatedCallsigns.Clear();
            FilteredAssociatedCallsigns.Clear();
            AssociatedCallsignsFilter = string.Empty;
            FilteredAssociatedCallsignsCount = 0;
            TotalAssociatedCallsignsCount = 0;

            ShowFirstMessage = false;
            ShowLastMessage = false;
            ShowDayChart = false;
            ShowHourChart = false;
            ShowAnalysisPanel = false;
            ShowAssociatedCallsigns = false;
        }

        private bool CanAnalyze() => !string.IsNullOrWhiteSpace(SelectedCallsign);

        partial void OnSelectedCallsignChanged(string? value)
        {
            ResetAnalysisData();
        }
    }
}