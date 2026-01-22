//// WpfApp/ViewModels/TemporalAnalysisViewModel.cs
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using Microsoft.EntityFrameworkCore;
//using RadioIntercepts.Application.Services;
//using RadioIntercepts.Infrastructure.Data;
//using RadioIntercepts.Analysis.Interfaces.Services;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Windows;
//using static System.Runtime.InteropServices.JavaScript.JSType;
//using RadioIntercepts.Analysis.Services.TemporalPatterns;
//using RadioIntercepts.Core.Models.TemporalAnalysis;

//namespace RadioIntercepts.WpfApp.ViewModels
//{
//    public partial class TemporalAnalysisViewModel : ObservableObject
//    {
//        private readonly ITemporalAnalysisService _temporalService;
//        private readonly AppDbContext _context;

//        [ObservableProperty]
//        private ObservableCollection<Area> _areas = new();

//        [ObservableProperty]
//        private ObservableCollection<string> _frequencies = new();

//        [ObservableProperty]
//        private ObservableCollection<string> _callsigns = new();

//        [ObservableProperty]
//        private DateTime? _dateFrom;

//        [ObservableProperty]
//        private DateTime? _dateTo;

//        [ObservableProperty]
//        private string? _selectedArea;

//        [ObservableProperty]
//        private string? _selectedFrequency;

//        [ObservableProperty]
//        private string? _selectedCallsign;

//        [ObservableProperty]
//        private int _slotDurationHours = 1;

//        [ObservableProperty]
//        private TimeSlotAnalysis? _timeSlotAnalysis;

//        [ObservableProperty]
//        private ObservableCollection<TemporalPattern> _detectedPatterns = new();

//        [ObservableProperty]
//        private ObservableCollection<AnomalyDetectionResult> _detectedAnomalies = new();

//        [ObservableProperty]
//        private ObservableCollection<PredictionResult> _predictions = new();

//        [ObservableProperty]
//        private ObservableCollection<DayOfWeekActivity> _dayOfWeekActivities = new();

//        [ObservableProperty]
//        private ObservableCollection<HourActivity> _hourActivities = new();

//        [ObservableProperty]
//        private bool _isLoading;

//        [ObservableProperty]
//        private string _selectedAnalysisType = "Slots";

//        [ObservableProperty]
//        private TimeSpan _minSilentDuration = TimeSpan.FromHours(4);

//        [ObservableProperty]
//        private int _predictionHoursAhead = 24;

//        [ObservableProperty]
//        private ObservableCollection<DateTime> _peakTimes = new();

//        [ObservableProperty]
//        private ObservableCollection<DateTime> _silentPeriods = new();

//        public TemporalAnalysisViewModel(
//            ITemporalAnalysisService temporalService,
//            AppDbContext context)
//        {
//            _temporalService = temporalService;
//            _context = context;

//            DateTo = DateTime.Today;
//            DateFrom = DateTime.Today.AddDays(-30);

//            _ = InitializeAsync();
//        }

//        private async Task InitializeAsync()
//        {
//            await LoadFilterDataAsync();
//            await AnalyzeActivitySlotsAsync();
//        }

//        private async Task LoadFilterDataAsync()
//        {
//            try
//            {
//                // Загрузка Areas
//                var areasList = await _context.Areas
//                    .AsNoTracking()
//                    .OrderBy(a => a.Name)
//                    .ToListAsync();

//                Areas.Clear();
//                foreach (var area in areasList)
//                {
//                    Areas.Add(area);
//                }

//                // Загрузка Frequencies
//                var frequenciesList = await _context.Frequencies
//                    .AsNoTracking()
//                    .OrderBy(f => f.Value)
//                    .Select(f => f.Value)
//                    .ToListAsync();

//                Frequencies.Clear();
//                foreach (var freq in frequenciesList)
//                {
//                    Frequencies.Add(freq);
//                }

//                // Загрузка Callsigns
//                var callsignsList = await _context.Callsigns
//                    .AsNoTracking()
//                    .OrderBy(c => c.Name)
//                    .Select(c => c.Name)
//                    .ToListAsync();

//                Callsigns.Clear();
//                foreach (var callsign in callsignsList)
//                {
//                    Callsigns.Add(callsign);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка загрузки фильтров: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        [RelayCommand]
//        private async Task AnalyzeActivitySlotsAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                TimeSlotAnalysis = await _temporalService.AnalyzeActivitySlotsAsync(
//                    DateFrom, DateTo, SelectedArea, SelectedFrequency, SlotDurationHours);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка анализа временных слотов: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task DetectPatternsAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                var patterns = await _temporalService.DetectTemporalPatternsAsync(DateFrom, DateTo);
//                DetectedPatterns.Clear();
//                foreach (var pattern in patterns)
//                {
//                    DetectedPatterns.Add(pattern);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка обнаружения паттернов: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task DetectAnomaliesAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                var anomalies = await _temporalService.DetectAnomaliesAsync(DateFrom, DateTo);
//                DetectedAnomalies.Clear();
//                foreach (var anomaly in anomalies)
//                {
//                    DetectedAnomalies.Add(anomaly);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка обнаружения аномалий: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task GeneratePredictionsAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                var predictions = await _temporalService.PredictActivityAsync(
//                    SelectedCallsign, PredictionHoursAhead);

//                Predictions.Clear();
//                foreach (var prediction in predictions)
//                {
//                    Predictions.Add(prediction);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка генерации прогнозов: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task AnalyzeDayOfWeekAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                var patterns = await _temporalService.AnalyzeDayOfWeekPatternsAsync(SelectedCallsign);
//                DayOfWeekActivities.Clear();

//                foreach (var pattern in patterns)
//                {
//                    DayOfWeekActivities.Add(new DayOfWeekActivity
//                    {
//                        DayOfWeek = pattern.Key,
//                        DayName = TranslateDayOfWeek(pattern.Key),
//                        MessageCount = pattern.Value,
//                        Percentage = patterns.Values.Sum() > 0
//                            ? (double)pattern.Value / patterns.Values.Sum() * 100
//                            : 0
//                    });
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка анализа по дням недели: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task AnalyzeHourlyAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                var patterns = await _temporalService.AnalyzeHourlyPatternsAsync(SelectedCallsign);
//                HourActivities.Clear();

//                foreach (var pattern in patterns)
//                {
//                    HourActivities.Add(new HourActivity
//                    {
//                        Hour = pattern.Key,
//                        MessageCount = pattern.Value,
//                        Percentage = patterns.Values.Sum() > 0
//                            ? (double)pattern.Value / patterns.Values.Sum() * 100
//                            : 0
//                    });
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка анализа по часам: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task FindSilentPeriodsAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                var periods = await _temporalService.FindSilentPeriodsAsync(MinSilentDuration, SelectedCallsign);
//                SilentPeriods.Clear();
//                foreach (var period in periods)
//                {
//                    SilentPeriods.Add(period);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка поиска периодов молчания: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task FindPeakTimesAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                var peaks = await _temporalService.FindPeakActivityTimesAsync(10, SelectedCallsign);
//                PeakTimes.Clear();
//                foreach (var peak in peaks)
//                {
//                    PeakTimes.Add(peak);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка поиска пиков активности: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private void ClearFilters()
//        {
//            SelectedArea = null;
//            SelectedFrequency = null;
//            SelectedCallsign = null;
//            DateFrom = DateTime.Today.AddDays(-30);
//            DateTo = DateTime.Today;
//            SlotDurationHours = 1;
//            MinSilentDuration = TimeSpan.FromHours(4);
//            PredictionHoursAhead = 24;

//            // Очищаем результаты
//            TimeSlotAnalysis = null;
//            DetectedPatterns.Clear();
//            DetectedAnomalies.Clear();
//            Predictions.Clear();
//            DayOfWeekActivities.Clear();
//            HourActivities.Clear();
//            PeakTimes.Clear();
//            SilentPeriods.Clear();
//        }

//        [RelayCommand]
//        private async Task ExportAnalysisAsync(string format = "csv")
//        {
//            try
//            {
//                string content = SelectedAnalysisType switch
//                {
//                    "Slots" => ExportSlotsToCsv(),
//                    "Patterns" => ExportPatternsToCsv(),
//                    "Anomalies" => ExportAnomaliesToCsv(),
//                    "Predictions" => ExportPredictionsToCsv(),
//                    "DayOfWeek" => ExportDayOfWeekToCsv(),
//                    "Hourly" => ExportHourlyToCsv(),
//                    _ => ""
//                };

//                if (string.IsNullOrEmpty(content))
//                {
//                    MessageBox.Show("Нет данных для экспорта", "Информация",
//                        MessageBoxButton.OK, MessageBoxImage.Information);
//                    return;
//                }

//                var dialog = new Microsoft.Win32.SaveFileDialog
//                {
//                    Filter = "CSV файлы (*.csv)|*.csv|JSON файлы (*.json)|*.json",
//                    FileName = $"temporal_analysis_{SelectedAnalysisType}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
//                };

//                if (dialog.ShowDialog() == true)
//                {
//                    System.IO.File.WriteAllText(dialog.FileName, content);
//                    MessageBox.Show($"Анализ экспортирован в {dialog.FileName}",
//                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка экспорта: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        private string ExportSlotsToCsv()
//        {
//            if (TimeSlotAnalysis == null) return "";

//            var lines = new List<string> { "Начало,Конец,Сообщений,Активных позывных,Уровень активности" };
//            lines.AddRange(TimeSlotAnalysis.Slots.Select(s =>
//                $"{s.StartTime:hh\\:mm},{s.EndTime:hh\\:mm},{s.MessageCount},{s.ActiveCallsigns},{s.ActivityLevel:F2}"));
//            return string.Join(Environment.NewLine, lines);
//        }

//        private string ExportPatternsToCsv()
//        {
//            var lines = new List<string> { "Тип паттерна,Начало,Конец,Уверенность,Типичные позывные,Типичные зоны" };
//            lines.AddRange(DetectedPatterns.Select(p =>
//                $"\"{p.PatternType}\",{p.StartTime:hh\\:mm},{p.EndTime:hh\\:mm},{p.Confidence:F2},\"{string.Join("; ", p.TypicalCallsigns)}\",\"{string.Join("; ", p.TypicalAreas)}\""));
//            return string.Join(Environment.NewLine, lines);
//        }

//        private string ExportAnomaliesToCsv()
//        {
//            var lines = new List<string> { "Время,Тип,Описание,Серьезность,Позывные,Зоны" };
//            lines.AddRange(DetectedAnomalies.Select(a =>
//                $"{a.Timestamp:yyyy-MM-dd HH:mm},\"{a.Type}\",\"{a.Description}\",{a.Severity:F2},\"{string.Join("; ", a.RelatedCallsigns)}\",\"{string.Join("; ", a.RelatedAreas)}\""));
//            return string.Join(Environment.NewLine, lines);
//        }

//        private string ExportPredictionsToCsv()
//        {
//            var lines = new List<string> { "Предсказанное время,Вероятность,Событие,Уверенность" };
//            lines.AddRange(Predictions.Select(p =>
//                $"{p.PredictedTime:yyyy-MM-dd HH:mm},{p.Probability:F2},\"{p.PredictedEvent}\",{p.Confidence:F2}"));
//            return string.Join(Environment.NewLine, lines);
//        }

//        private string ExportDayOfWeekToCsv()
//        {
//            var lines = new List<string> { "День недели,Сообщений,Процент" };
//            lines.AddRange(DayOfWeekActivities.Select(d =>
//                $"{d.DayName},{d.MessageCount},{d.Percentage:F1}%"));
//            return string.Join(Environment.NewLine, lines);
//        }

//        private string ExportHourlyToCsv()
//        {
//            var lines = new List<string> { "Час,Сообщений,Процент" };
//            lines.AddRange(HourActivities.Select(h =>
//                $"{h.Hour:00}:00,{h.MessageCount},{h.Percentage:F1}%"));
//            return string.Join(Environment.NewLine, lines);
//        }

//        private string TranslateDayOfWeek(DayOfWeek day)
//        {
//            return day switch
//            {
//                DayOfWeek.Monday => "Понедельник",
//                DayOfWeek.Tuesday => "Вторник",
//                DayOfWeek.Wednesday => "Среда",
//                DayOfWeek.Thursday => "Четверг",
//                DayOfWeek.Friday => "Пятница",
//                DayOfWeek.Saturday => "Суббота",
//                DayOfWeek.Sunday => "Воскресенье",
//                _ => day.ToString()
//            };
//        }

//        partial void OnSelectedAnalysisTypeChanged(string value)
//        {
//            switch (value)
//            {
//                case "Slots":
//                    _ = AnalyzeActivitySlotsAsync();
//                    break;
//                case "Patterns":
//                    _ = DetectPatternsAsync();
//                    break;
//                case "Anomalies":
//                    _ = DetectAnomaliesAsync();
//                    break;
//                case "Predictions":
//                    _ = GeneratePredictionsAsync();
//                    break;
//                case "DayOfWeek":
//                    _ = AnalyzeDayOfWeekAsync();
//                    break;
//                case "Hourly":
//                    _ = AnalyzeHourlyAsync();
//                    break;
//            }
//        }
//    }

//    public class DayOfWeekActivity
//    {
//        public DayOfWeek DayOfWeek { get; set; }
//        public string DayName { get; set; } = null!;
//        public int MessageCount { get; set; }
//        public double Percentage { get; set; }
//        public string PercentageFormatted => $"{Percentage:F1}%";
//    }

//    public class HourActivity
//    {
//        public int Hour { get; set; }
//        public int MessageCount { get; set; }
//        public double Percentage { get; set; }
//        public string HourFormatted => $"{Hour:00}:00";
//        public string PercentageFormatted => $"{Percentage:F1}%";
//    }
//}