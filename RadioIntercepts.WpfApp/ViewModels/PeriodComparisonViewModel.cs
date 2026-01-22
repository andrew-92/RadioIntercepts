// WpfApp/ViewModels/PeriodComparisonViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Application.Services;
using RadioIntercepts.Analysis.Interfaces.Services;
using RadioIntercepts.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using RadioIntercepts.Analysis.Services.PeriodComparison;
using RadioIntercepts.Core.Models.PeriodComparison;

namespace RadioIntercepts.WpfApp.ViewModels
{
    public partial class PeriodComparisonViewModel : ObservableObject
    {
        private readonly IPeriodComparisonService _comparisonService;
        private readonly AppDbContext _context;

        [ObservableProperty]
        private DateTime _startDate1 = DateTime.Today.AddDays(-30);

        [ObservableProperty]
        private DateTime _endDate1 = DateTime.Today;

        [ObservableProperty]
        private DateTime _startDate2 = DateTime.Today.AddDays(-60);

        [ObservableProperty]
        private DateTime _endDate2 = DateTime.Today.AddDays(-31);

        [ObservableProperty]
        private ObservableCollection<Area> _areas = new();

        [ObservableProperty]
        private ObservableCollection<string> _frequencies = new();

        [ObservableProperty]
        private ObservableCollection<string> _callsigns = new();

        [ObservableProperty]
        private string? _selectedArea;

        [ObservableProperty]
        private string? _selectedFrequency;

        [ObservableProperty]
        private ObservableCollection<string> _selectedCallsigns = new();

        [ObservableProperty]
        private PeriodComparisonResult? _comparisonResult;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedComparisonType = "Overall";

        [ObservableProperty]
        private ObservableCollection<CallsignActivityShift> _activityShifts = new();

        public PeriodComparisonViewModel(
            IPeriodComparisonService comparisonService,
            AppDbContext context)
        {
            _comparisonService = comparisonService;
            _context = context;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadFilterDataAsync();
        }

        private async Task LoadFilterDataAsync()
        {
            try
            {
                // Загрузка Areas
                var areasList = await _context.Areas
                    .AsNoTracking()
                    .OrderBy(a => a.Name)
                    .ToListAsync();

                Areas.Clear();
                foreach (var area in areasList)
                {
                    Areas.Add(area);
                }

                // Загрузка Frequencies
                var frequenciesList = await _context.Frequencies
                    .AsNoTracking()
                    .OrderBy(f => f.Value)
                    .Select(f => f.Value)
                    .ToListAsync();

                Frequencies.Clear();
                foreach (var freq in frequenciesList)
                {
                    Frequencies.Add(freq);
                }

                // Загрузка Callsigns
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
                MessageBox.Show($"Ошибка загрузки фильтров: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ComparePeriodsAsync()
        {
            if (StartDate1 >= EndDate1 || StartDate2 >= EndDate2)
            {
                MessageBox.Show("Дата начала периода должна быть раньше даты окончания",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var request = new PeriodComparisonRequest
                {
                    StartDate1 = StartDate1,
                    EndDate1 = EndDate1,
                    StartDate2 = StartDate2,
                    EndDate2 = EndDate2,
                    Area = SelectedArea,
                    Frequency = SelectedFrequency,
                    Callsigns = SelectedCallsigns.Any() ? SelectedCallsigns.ToList() : null
                };

                ComparisonResult = await _comparisonService.ComparePeriodsAsync(request);

                // Загружаем сдвиги активности позывных
                await LoadActivityShiftsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сравнения периодов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadActivityShiftsAsync()
        {
            try
            {
                var shifts = await _comparisonService.GetCallsignActivityShiftsAsync(
                    StartDate1, EndDate1, StartDate2, EndDate2);

                ActivityShifts.Clear();
                foreach (var shift in shifts)
                {
                    ActivityShifts.Add(shift);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки сдвигов активности: {ex.Message}");
            }
        }

        [RelayCommand]
        private void SwapPeriods()
        {
            (StartDate1, StartDate2) = (StartDate2, StartDate1);
            (EndDate1, EndDate2) = (EndDate2, EndDate1);
        }

        [RelayCommand]
        private void SetLastWeekComparison()
        {
            EndDate1 = DateTime.Today;
            StartDate1 = DateTime.Today.AddDays(-7);
            EndDate2 = DateTime.Today.AddDays(-7);
            StartDate2 = DateTime.Today.AddDays(-14);
        }

        [RelayCommand]
        private void SetLastMonthComparison()
        {
            EndDate1 = DateTime.Today;
            StartDate1 = DateTime.Today.AddDays(-30);
            EndDate2 = DateTime.Today.AddDays(-30);
            StartDate2 = DateTime.Today.AddDays(-60);
        }

        [RelayCommand]
        private void SetLastYearComparison()
        {
            EndDate1 = DateTime.Today;
            StartDate1 = DateTime.Today.AddDays(-365);
            EndDate2 = DateTime.Today.AddDays(-365);
            StartDate2 = DateTime.Today.AddDays(-730);
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SelectedArea = null;
            SelectedFrequency = null;
            SelectedCallsigns.Clear();
            ComparisonResult = null;
            ActivityShifts.Clear();
        }

        [RelayCommand]
        private async Task ExportComparisonAsync(string format = "csv")
        {
            if (ComparisonResult == null)
            {
                MessageBox.Show("Нет данных для экспорта", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string content = ExportToCsv();

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|JSON файлы (*.json)|*.json",
                    FileName = $"period_comparison_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialog.FileName, content);
                    MessageBox.Show($"Сравнение экспортировано в {dialog.FileName}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ExportToCsv()
        {
            if (ComparisonResult == null)
                return string.Empty;

            var lines = new List<string>();

            // Основные метрики
            lines.Add("Метрика,Период 1,Период 2,Изменение %");
            lines.Add($"Общее количество сообщений,{ComparisonResult.Period1.TotalMessages},{ComparisonResult.Period2.TotalMessages},{ComparisonResult.Metrics.TotalMessagesChange:F1}%");
            lines.Add($"Уникальных позывных,{ComparisonResult.Period1.UniqueCallsigns},{ComparisonResult.Period2.UniqueCallsigns},{ComparisonResult.Metrics.UniqueCallsignsChange:F1}%");
            lines.Add($"Сообщений в день,{ComparisonResult.Period1.MessagesPerDay:F1},{ComparisonResult.Period2.MessagesPerDay:F1},{ComparisonResult.Metrics.MessagesPerDayChange:F1}%");

            // Сравнение позывных
            lines.Add("");
            lines.Add("Сравнение позывных");
            lines.Add("Позывной,Период 1,Период 2,Изменение %,Вклад в период 1 %,Вклад в период 2 %");
            foreach (var callsign in ComparisonResult.CallsignComparisons.Take(20))
            {
                lines.Add($"{callsign.Callsign},{callsign.CountPeriod1},{callsign.CountPeriod2},{callsign.ChangePercent:F1}%,{callsign.ContributionPeriod1:F1}%,{callsign.ContributionPeriod2:F1}%");
            }

            // Сравнение зон
            lines.Add("");
            lines.Add("Сравнение зон");
            lines.Add("Зона,Период 1,Период 2,Изменение %");
            foreach (var area in ComparisonResult.AreaComparisons.Take(10))
            {
                lines.Add($"{area.Area},{area.CountPeriod1},{area.CountPeriod2},{area.ChangePercent:F1}%");
            }

            // Сравнение типов сообщений
            lines.Add("");
            lines.Add("Сравнение типов сообщений");
            lines.Add("Тип,Период 1,Период 2,Изменение %,Вклад в период 1 %,Вклад в период 2 %");
            foreach (var type in ComparisonResult.MessageTypeComparisons)
            {
                lines.Add($"{type.MessageType},{type.CountPeriod1},{type.CountPeriod2},{type.ChangePercent:F1}%,{type.ContributionPeriod1:F1}%,{type.ContributionPeriod2:F1}%");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}