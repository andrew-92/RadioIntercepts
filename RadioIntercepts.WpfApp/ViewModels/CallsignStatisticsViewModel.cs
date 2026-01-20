using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RadioIntercepts.WpfApp.ViewModels
{
    public partial class CallsignStatisticsViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<Area> _areas = new();

        [ObservableProperty]
        private ObservableCollection<string> _frequencies = new();

        [ObservableProperty]
        private DateTime? _dateFrom;

        [ObservableProperty]
        private DateTime? _dateTo;

        [ObservableProperty]
        private string? _selectedArea;

        [ObservableProperty]
        private string? _selectedFrequency;

        [ObservableProperty]
        private ObservableCollection<CallsignStatisticItem> _statistics = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _totalMessagesCount;

        // Свойства для диаграммы
        [ObservableProperty]
        private SeriesCollection _pieChartSeries = new();

        [ObservableProperty]
        private List<string> _pieChartLabels = new();

        public CallsignStatisticsViewModel(AppDbContext context)
        {
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
                var areasList = await _context.Areas
                    .AsNoTracking()
                    .OrderBy(a => a.Name)
                    .ToListAsync();

                Areas.Clear();
                foreach (var area in areasList)
                {
                    Areas.Add(area);
                }

                var frequenciesList = await _context.Frequencies
                    .AsNoTracking()
                    .OrderBy(f => f.Value)
                    .Select(f => f.Value)
                    .ToListAsync();

                Frequencies.Clear();
                foreach (var frequency in frequenciesList)
                {
                    Frequencies.Add(frequency);
                }

                DateTo = DateTime.Now;
                DateFrom = DateTime.Now.AddDays(-30);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных фильтров: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task LoadStatisticsAsync()
        {
            if (!DateFrom.HasValue || !DateTo.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите период дат",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DateFrom > DateTo)
            {
                MessageBox.Show("Дата 'С' не может быть позже даты 'По'",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            Statistics.Clear();
            PieChartSeries.Clear();
            PieChartLabels.Clear();

            try
            {
                IQueryable<Message> messagesQuery = _context.Messages
                    .AsNoTracking()
                    .Include(m => m.Area)
                    .Include(m => m.Frequency)
                    .Include(m => m.MessageCallsigns)
                        .ThenInclude(mc => mc.Callsign)
                    .Where(m => m.DateTime >= DateFrom.Value && m.DateTime <= DateTo.Value);

                if (!string.IsNullOrWhiteSpace(SelectedArea))
                {
                    var area = Areas.FirstOrDefault(a => a.Name == SelectedArea);
                    if (area != null)
                    {
                        messagesQuery = messagesQuery.Where(m => m.Area.Key == area.Key);
                    }
                }

                if (!string.IsNullOrWhiteSpace(SelectedFrequency))
                {
                    messagesQuery = messagesQuery.Where(m => m.Frequency.Value == SelectedFrequency);
                }

                var messagesWithCallsigns = await messagesQuery
                    .Select(m => new
                    {
                        MessageId = m.Id,
                        Callsigns = m.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList()
                    })
                    .ToListAsync();

                TotalMessagesCount = messagesWithCallsigns.Count;

                if (TotalMessagesCount == 0)
                {
                    MessageBox.Show("Нет данных за выбранный период с указанными фильтрами",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var callsignStats = messagesWithCallsigns
                    .SelectMany(m => m.Callsigns)
                    .GroupBy(callsign => callsign)
                    .Select(group => new CallsignStatisticData
                    {
                        Callsign = group.Key,
                        MessageCount = group.Count(),
                        Percentage = (double)group.Count() / TotalMessagesCount * 100
                    })
                    .OrderByDescending(x => x.MessageCount)
                    .ThenBy(x => x.Callsign)
                    .ToList();

                // Обновляем коллекцию статистики
                Statistics.Clear();
                foreach (var stat in callsignStats)
                {
                    Statistics.Add(new CallsignStatisticItem
                    {
                        Callsign = stat.Callsign,
                        MessageCount = stat.MessageCount,
                        Percentage = stat.Percentage
                    });
                }

                // Создаем данные для круговой диаграммы
                UpdatePieChartData(callsignStats);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdatePieChartData(List<CallsignStatisticData> stats)
        {
            PieChartSeries.Clear();
            PieChartLabels.Clear();

            // Ограничиваем количество отображаемых элементов для читаемости диаграммы
            int maxItemsToShow = 10;
            var itemsToShow = stats.Take(maxItemsToShow).ToList();
            var otherItems = stats.Skip(maxItemsToShow).ToList();

            // Цвета для диаграммы
            var colors = new[]
            {
                System.Windows.Media.Color.FromRgb(33, 150, 243), // Blue
                System.Windows.Media.Color.FromRgb(244, 67, 54),  // Red
                System.Windows.Media.Color.FromRgb(76, 175, 80),  // Green
                System.Windows.Media.Color.FromRgb(255, 193, 7),  // Yellow
                System.Windows.Media.Color.FromRgb(156, 39, 176), // Purple
                System.Windows.Media.Color.FromRgb(0, 188, 212),  // Cyan
                System.Windows.Media.Color.FromRgb(255, 87, 34),  // Deep Orange
                System.Windows.Media.Color.FromRgb(121, 85, 72),  // Brown
                System.Windows.Media.Color.FromRgb(96, 125, 139), // Blue Grey
                System.Windows.Media.Color.FromRgb(233, 30, 99),  // Pink
            };

            // Добавляем основные позывные
            for (int i = 0; i < itemsToShow.Count; i++)
            {
                var stat = itemsToShow[i];
                var color = colors[i % colors.Length];

                PieChartSeries.Add(new PieSeries
                {
                    Title = stat.Callsign,
                    Values = new ChartValues<double> { stat.MessageCount },
                    DataLabels = true,
                    LabelPoint = point => $"{stat.Callsign}: {point.Y} ({point.Participation:P1})",
                    Fill = new System.Windows.Media.SolidColorBrush(color)
                });

                PieChartLabels.Add(stat.Callsign);
            }

            // Если есть другие позывные, объединяем их в "Прочие"
            if (otherItems.Any())
            {
                int otherCount = otherItems.Sum(x => x.MessageCount);
                double otherPercentage = otherItems.Sum(x => x.Percentage);

                PieChartSeries.Add(new PieSeries
                {
                    Title = "Прочие",
                    Values = new ChartValues<double> { otherCount },
                    DataLabels = true,
                    LabelPoint = point => $"Прочие: {point.Y} ({point.Participation:P1})",
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158))
                });

                PieChartLabels.Add("Прочие");
            }
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SelectedArea = null;
            SelectedFrequency = null;
            DateTo = DateTime.Now;
            DateFrom = DateTime.Now.AddDays(-30);
            Statistics.Clear();
            PieChartSeries.Clear();
            PieChartLabels.Clear();
            TotalMessagesCount = 0;
        }
    }

    public class CallsignStatisticItem
    {
        public string Callsign { get; set; }
        public int MessageCount { get; set; }
        public double Percentage { get; set; }

        public string PercentageFormatted => $"{Percentage:F2}%";
        public string CountFormatted => $"{MessageCount} сообщ.";
    }

    public class CallsignStatisticData
    {
        public string Callsign { get; set; }
        public int MessageCount { get; set; }
        public double Percentage { get; set; }
    }
}