using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Analysis.Interfaces.Services;
using RadioIntercepts.Core.Charts;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RadioIntercepts.WpfApp.ViewModels
{
    public partial class StatisticsViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly IChartService _chartService;

        // Коллекции для фильтров
        [ObservableProperty]
        private ObservableCollection<Area> _areas = new();

        [ObservableProperty]
        private ObservableCollection<string> _frequencies = new();

        // Значения фильтров
        [ObservableProperty]
        private DateTime? _dateFrom;

        [ObservableProperty]
        private DateTime? _dateTo;

        [ObservableProperty]
        private string? _selectedArea;

        [ObservableProperty]
        private string? _selectedFrequency;

        // Результаты
        [ObservableProperty]
        private ChartData? _dayOfWeekChart;

        [ObservableProperty]
        private ChartData? _hourChart;

        [ObservableProperty]
        private int _totalMessagesCount;

        [ObservableProperty]
        private bool _isLoading;

        // Текст выбранных фильтров
        [ObservableProperty]
        private string _filtersText = "Фильтры не применены";

        public StatisticsViewModel(AppDbContext context, IChartService chartService)
        {
            _context = context;
            _chartService = chartService;

            DateTo = DateTime.Today;
            DateFrom = DateTime.Today.AddDays(-30);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadFilterDataAsync();
            await LoadStatisticsAsync();
        }

        private async Task LoadFilterDataAsync()
        {
            try
            {
                var areasList = await _context.Areas
                    .AsNoTracking()
                    .OrderBy(a => a.Name)
                    .ToListAsync();

                Areas.Clear();
                foreach (var area in areasList)
                    Areas.Add(area);

                var frequenciesList = await _context.Frequencies
                    .AsNoTracking()
                    .OrderBy(f => f.Value)
                    .Select(f => f.Value)
                    .ToListAsync();

                Frequencies.Clear();
                foreach (var freq in frequenciesList)
                    Frequencies.Add(freq);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка загрузки фильтров: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ApplyFiltersAsync()
        {
            await LoadStatisticsAsync();
            UpdateFiltersText();
        }

        [RelayCommand]
        private void ClearFilters()
        {
            DateFrom = DateTime.Today.AddDays(-30);
            DateTo = DateTime.Today;
            SelectedArea = null;
            SelectedFrequency = null;

            _ = ApplyFiltersAsync();
        }

        private async Task LoadStatisticsAsync()
        {
            IsLoading = true;
            try
            {
                TotalMessagesCount = await _chartService.GetTotalMessagesCountAsync(
                    DateFrom,
                    DateTo,
                    SelectedArea,
                    SelectedFrequency);

                DayOfWeekChart = await _chartService.GetActivityByDayOfWeekAsync(
                    DateFrom,
                    DateTo,
                    SelectedArea,
                    SelectedFrequency);

                HourChart = await _chartService.GetActivityByHourAsync(
                    DateFrom,
                    DateTo,
                    SelectedArea,
                    SelectedFrequency);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка загрузки статистики: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateFiltersText()
        {
            var filters = new StringBuilder();

            if (DateFrom.HasValue || DateTo.HasValue)
            {
                filters.Append("Период: ");
                if (DateFrom.HasValue)
                    filters.Append($"{DateFrom:dd.MM.yyyy}");
                filters.Append(" - ");
                if (DateTo.HasValue)
                    filters.Append($"{DateTo:dd.MM.yyyy}");
                filters.Append("; ");
            }

            if (!string.IsNullOrEmpty(SelectedArea))
                filters.Append($"Area: {SelectedArea}; ");

            if (!string.IsNullOrEmpty(SelectedFrequency))
                filters.Append($"Frequency: {SelectedFrequency}; ");

            FiltersText = filters.Length > 0
                ? filters.ToString().TrimEnd(' ', ';')
                : "Фильтры не применены";
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(DateFrom) ||
                e.PropertyName == nameof(DateTo) ||
                e.PropertyName == nameof(SelectedArea) ||
                e.PropertyName == nameof(SelectedFrequency))
            {
                UpdateFiltersText();
            }
        }
    }
}
