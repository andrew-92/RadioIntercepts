// WpfApp/ViewModels/CommunicationFlowViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Application.Services;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.WpfApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RadioIntercepts.WpfApp.ViewModels
{
    public partial class CommunicationFlowViewModel : ObservableObject
    {
        private readonly ICommunicationFlowService _flowService;
        private readonly AppDbContext _context;

        [ObservableProperty]
        private CommunicationFlow? _currentFlow;

        [ObservableProperty]
        private ParallelTimeline? _currentTimeline;

        [ObservableProperty]
        private ObservableCollection<CommunicationPattern> _detectedPatterns = new();

        [ObservableProperty]
        private ObservableCollection<string> _commonFlows = new();

        [ObservableProperty]
        private Dictionary<string, double> _flowMetrics = new();

        [ObservableProperty]
        private ObservableCollection<string> _areas = new();

        [ObservableProperty]
        private ObservableCollection<string> _callsigns = new();

        [ObservableProperty]
        private DateTime? _dateFrom;

        [ObservableProperty]
        private DateTime? _dateTo;

        [ObservableProperty]
        private string? _selectedArea;

        [ObservableProperty]
        private string? _selectedCallsign;

        [ObservableProperty]
        private string? _selectedCallsign2;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedVisualization = "Sankey";

        [ObservableProperty]
        private int _maxNodes = 50;

        [ObservableProperty]
        private ObservableCollection<CommunicationFlow> _comparisonFlows = new();

        [ObservableProperty]
        private string _filterText = string.Empty;

        [ObservableProperty]
        private bool _showFlowDetails = false;

        [ObservableProperty]
        private FlowStatistics? _selectedFlowStatistics;

        public CommunicationFlowViewModel(
            ICommunicationFlowService flowService,
            AppDbContext context)
        {
            _flowService = flowService;
            _context = context;

            DateTo = DateTime.Today;
            DateFrom = DateTime.Today.AddDays(-1); // По умолчанию последние сутки

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadFilterDataAsync();
            await BuildDefaultVisualizationAsync();
        }

        private async Task LoadFilterDataAsync()
        {
            try
            {
                // Загрузка Areas
                var areasList = await _context.Areas
                    .AsNoTracking()
                    .OrderBy(a => a.Name)
                    .Select(a => a.Name)
                    .ToListAsync();

                Areas.Clear();
                foreach (var area in areasList)
                {
                    Areas.Add(area);
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
        private async Task BuildDefaultVisualizationAsync()
        {
            if (!DateFrom.HasValue || !DateTo.HasValue)
            {
                MessageBox.Show("Укажите период времени", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                switch (SelectedVisualization)
                {
                    case "Sankey":
                        await BuildSankeyDiagramAsync();
                        break;
                    case "CallsignFlow":
                        await BuildCallsignFlowAsync();
                        break;
                    case "AreaFlow":
                        await BuildAreaFlowAsync();
                        break;
                    case "Timeline":
                        await BuildParallelTimelineAsync();
                        break;
                    case "CallsignTimeline":
                        await BuildCallsignTimelineAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка построения визуализации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task BuildSankeyDiagramAsync()
        {
            if (!DateFrom.HasValue || !DateTo.HasValue)
                return;

            CurrentFlow = await _flowService.BuildSankeyDiagramAsync(
                DateFrom.Value, DateTo.Value, SelectedArea, null, MaxNodes);

            if (CurrentFlow != null)
            {
                SelectedFlowStatistics = CurrentFlow.Statistics.GetValueOrDefault("overall");
                ShowFlowDetails = true;
            }
        }

        [RelayCommand]
        private async Task BuildCallsignFlowAsync()
        {
            if (!DateFrom.HasValue || !DateTo.HasValue || string.IsNullOrEmpty(SelectedCallsign))
            {
                MessageBox.Show("Укажите период времени и выберите позывной", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentFlow = await _flowService.BuildCallsignFlowAsync(
                SelectedCallsign, DateFrom.Value, DateTo.Value);

            if (CurrentFlow != null)
            {
                SelectedFlowStatistics = CurrentFlow.Statistics.GetValueOrDefault("overall");
                ShowFlowDetails = true;
            }
        }

        [RelayCommand]
        private async Task BuildAreaFlowAsync()
        {
            if (!DateFrom.HasValue || !DateTo.HasValue || string.IsNullOrEmpty(SelectedArea))
            {
                MessageBox.Show("Укажите период времени и выберите зону", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentFlow = await _flowService.BuildAreaFlowAsync(
                SelectedArea, DateFrom.Value, DateTo.Value);

            if (CurrentFlow != null)
            {
                SelectedFlowStatistics = CurrentFlow.Statistics.GetValueOrDefault("overall");
                ShowFlowDetails = true;
            }
        }

        [RelayCommand]
        private async Task BuildParallelTimelineAsync()
        {
            if (!DateFrom.HasValue || !DateTo.HasValue)
                return;

            List<string> selectedCallsigns = null;
            if (!string.IsNullOrEmpty(SelectedCallsign))
                selectedCallsigns = new List<string> { SelectedCallsign };

            List<string> selectedAreas = null;
            if (!string.IsNullOrEmpty(SelectedArea))
                selectedAreas = new List<string> { SelectedArea };

            CurrentTimeline = await _flowService.BuildParallelTimelineAsync(
                DateFrom.Value, DateTo.Value, selectedCallsigns, selectedAreas);
        }

        [RelayCommand]
        private async Task BuildCallsignTimelineAsync()
        {
            if (!DateFrom.HasValue || !DateTo.HasValue || string.IsNullOrEmpty(SelectedCallsign))
            {
                MessageBox.Show("Укажите период времени и выберите позывной", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentTimeline = await _flowService.BuildCallsignTimelineAsync(
                SelectedCallsign, DateFrom.Value, DateTo.Value);
        }

        [RelayCommand]
        private async Task BuildConversationTimelineAsync(long startMessageId)
        {
            IsLoading = true;
            try
            {
                CurrentTimeline = await _flowService.BuildConversationTimelineAsync(startMessageId, 20);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка построения timeline беседы: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task DetectPatternsAsync()
        {
            if (!DateFrom.HasValue || !DateTo.HasValue)
            {
                MessageBox.Show("Укажите период времени", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                var patterns = await _flowService.DetectCommunicationPatternsAsync(DateFrom.Value, DateTo.Value);

                DetectedPatterns.Clear();
                foreach (var pattern in patterns)
                {
                    DetectedPatterns.Add(pattern);
                }

                if (!DetectedPatterns.Any())
                {
                    MessageBox.Show("Паттерны общения не обнаружены", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обнаружения паттернов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task FindCommonFlowsAsync()
        {
            IsLoading = true;
            try
            {
                var flows = await _flowService.FindCommonFlowsAsync(3);

                CommonFlows.Clear();
                foreach (var flow in flows)
                {
                    CommonFlows.Add(flow);
                }

                if (!CommonFlows.Any())
                {
                    MessageBox.Show("Общие потоки не обнаружены", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска общих потоков: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CalculateMetricsAsync()
        {
            if (!DateFrom.HasValue || !DateTo.HasValue)
            {
                MessageBox.Show("Укажите период времени", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                var metrics = await _flowService.CalculateFlowMetricsAsync(DateFrom.Value, DateTo.Value);

                FlowMetrics.Clear();
                foreach (var metric in metrics)
                {
                    FlowMetrics[metric.Key] = metric.Value;
                }

                // Отображаем метрики
                if (FlowMetrics.Any())
                {
                    var metricsText = "Метрики потока общения:\n\n";
                    foreach (var metric in FlowMetrics)
                    {
                        metricsText += $"{metric.Key}: {metric.Value:F3}\n";
                    }

                    MessageBox.Show(metricsText, "Метрики потока",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчета метрик: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ComparePeriodsAsync()
        {
            if (!DateFrom.HasValue || !DateTo.HasValue)
            {
                MessageBox.Show("Укажите период времени", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаем два периода для сравнения
            var period1Start = DateFrom.Value;
            var period1End = DateFrom.Value.AddHours(12);
            var period2Start = DateTo.Value.AddHours(-12);
            var period2End = DateTo.Value;

            IsLoading = true;
            try
            {
                var flows = await _flowService.CompareTimePeriodsAsync(
                    period1Start, period1End, period2Start, period2End);

                ComparisonFlows.Clear();
                foreach (var flow in flows)
                {
                    ComparisonFlows.Add(flow);
                }

                MessageBox.Show($"Построено {flows.Count} потоков для сравнения", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
        private async Task ExportFlowDataAsync(string format = "json")
        {
            if (CurrentFlow == null)
            {
                MessageBox.Show("Нет данных для экспорта", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var content = await _flowService.ExportFlowDataAsync(CurrentFlow, format);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = format.ToLower() == "csv"
                        ? "CSV файлы (*.csv)|*.csv"
                        : "JSON файлы (*.json)|*.json",
                    FileName = $"communication_flow_{DateTime.Now:yyyyMMdd_HHmm}.{format}"
                };

                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialog.FileName, content);
                    MessageBox.Show($"Данные экспортированы в {dialog.FileName}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ExportTimelineDataAsync(string format = "json")
        {
            if (CurrentTimeline == null)
            {
                MessageBox.Show("Нет данных для экспорта", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var content = await _flowService.ExportTimelineDataAsync(CurrentTimeline, format);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = format.ToLower() == "csv"
                        ? "CSV файлы (*.csv)|*.csv"
                        : "JSON файлы (*.json)|*.json",
                    FileName = $"timeline_{DateTime.Now:yyyyMMdd_HHmm}.{format}"
                };

                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialog.FileName, content);
                    MessageBox.Show($"Данные экспортированы в {dialog.FileName}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SelectedArea = null;
            SelectedCallsign = null;
            SelectedCallsign2 = null;
            DateFrom = DateTime.Today.AddDays(-1);
            DateTo = DateTime.Today;
            MaxNodes = 50;
            FilterText = string.Empty;

            CurrentFlow = null;
            CurrentTimeline = null;
            DetectedPatterns.Clear();
            CommonFlows.Clear();
            FlowMetrics.Clear();
            ComparisonFlows.Clear();
            ShowFlowDetails = false;
        }

        [RelayCommand]
        private void ToggleFlowDetails()
        {
            ShowFlowDetails = !ShowFlowDetails;
        }

        partial void OnSelectedVisualizationChanged(string value)
        {
            // Сбрасываем текущую визуализацию при смене типа
            CurrentFlow = null;
            CurrentTimeline = null;
            ShowFlowDetails = false;
        }

        partial void OnDateFromChanged(DateTime? value)
        {
            // Автоматически обновляем визуализацию при изменении даты
            if (value.HasValue && DateTo.HasValue && (DateTo.Value - value.Value).TotalDays <= 7)
            {
                _ = BuildDefaultVisualizationAsync();
            }
        }

        partial void OnDateToChanged(DateTime? value)
        {
            if (DateFrom.HasValue && value.HasValue && (value.Value - DateFrom.Value).TotalDays <= 7)
            {
                _ = BuildDefaultVisualizationAsync();
            }
        }

        partial void OnSelectedAreaChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _ = BuildDefaultVisualizationAsync();
            }
        }

        partial void OnSelectedCallsignChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value) && SelectedVisualization.StartsWith("Callsign"))
            {
                _ = BuildDefaultVisualizationAsync();
            }
        }
    }
}