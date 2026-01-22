//// WpfApp/ViewModels/Reports/ReportViewModel.cs
//using CommunityToolkit.Mvvm.Input;
//using GalaSoft.MvvmLight.CommandWpf;
//using RadioIntercepts.Analysis.Interfaces.Services;
//using RadioIntercepts.Core.Models.Reports;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Windows.Input;

//namespace RadioIntercepts.WpfApp.ViewModels
//{
//    public class ReportViewModel : INotifyPropertyChanged 
//    {
//        private readonly IReportService _reportService;
//        private readonly IAlertService _alertService; 

//        // Коллекции для привязки
//        private List<ReportTemplateViewModel> _templates = new();
//        private List<GeneratedReportViewModel> _generatedReports = new();
//        private ReportTemplateViewModel _selectedTemplate;
//        private GeneratedReportViewModel _selectedReport;
//        private bool _isGenerating;
//        private string _statusMessage;

//        // Команды
//        public ICommand GenerateReportCommand { get; }
//        public ICommand DownloadReportCommand { get; }
//        public ICommand DeleteReportCommand { get; }
//        public ICommand RefreshReportsCommand { get; }
//        public ICommand RetryFailedReportCommand { get; }

//        public ReportViewModel(IReportService reportService, IAlertService alertService)
//        {
//            _reportService = reportService;
//            _alertService = alertService;

//            // Инициализация команд
//            GenerateReportCommand = new RelayCommand<ReportTemplateViewModel>(GenerateReport, CanGenerateReport);
//            DownloadReportCommand = new RelayCommand<GeneratedReportViewModel>(DownloadReport, CanDownloadReport);
//            DeleteReportCommand = new RelayCommand<GeneratedReportViewModel>(DeleteReport, CanDeleteReport);
//            RefreshReportsCommand = new RelayCommand(async () => await LoadDataAsync());
//            RetryFailedReportCommand = new RelayCommand<string>(async (id) => await RetryReportAsync(id));

//            // Загрузка данных
//            LoadDataAsync().ConfigureAwait(false);
//        }

//        public List<ReportTemplateViewModel> Templates
//        {
//            get => _templates;
//            set
//            {
//                _templates = value;
//                OnPropertyChanged();
//            }
//        }

//        public List<GeneratedReportViewModel> GeneratedReports
//        {
//            get => _generatedReports;
//            set
//            {
//                _generatedReports = value;
//                OnPropertyChanged();
//            }
//        }

//        public ReportTemplateViewModel SelectedTemplate
//        {
//            get => _selectedTemplate;
//            set
//            {
//                _selectedTemplate = value;
//                OnPropertyChanged();
//                OnPropertyChanged(nameof(CanGenerateSelected));
//                ((RelayCommand<ReportTemplateViewModel>)GenerateReportCommand).RaiseCanExecuteChanged();
//            }
//        }

//        public GeneratedReportViewModel SelectedReport
//        {
//            get => _selectedReport;
//            set
//            {
//                _selectedReport = value;
//                OnPropertyChanged();
//                ((RelayCommand<GeneratedReportViewModel>)DownloadReportCommand).RaiseCanExecuteChanged();
//                ((RelayCommand<GeneratedReportViewModel>)DeleteReportCommand).RaiseCanExecuteChanged();
//            }
//        }

//        public bool IsGenerating
//        {
//            get => _isGenerating;
//            set
//            {
//                _isGenerating = value;
//                OnPropertyChanged();
//            }
//        }

//        public string StatusMessage
//        {
//            get => _statusMessage;
//            set
//            {
//                _statusMessage = value;
//                OnPropertyChanged();
//            }
//        }

//        public bool CanGenerateSelected => SelectedTemplate != null;

//        private async Task LoadDataAsync()
//        {
//            try
//            {
//                StatusMessage = "Загрузка данных...";

//                // Загрузка шаблонов
//                var templates = await _reportService.GetReportTemplatesAsync();
//                Templates = templates.Select(t => new ReportTemplateViewModel(t)).ToList();

//                // Загрузка сгенерированных отчетов
//                var reports = await _reportService.GetGeneratedReportsAsync();
//                GeneratedReports = reports.Select(r => new GeneratedReportViewModel(r)).ToList();

//                StatusMessage = $"Загружено {Templates.Count} шаблонов и {GeneratedReports.Count} отчетов";
//            }
//            catch (Exception ex)
//            {
//                StatusMessage = $"Ошибка загрузки: {ex.Message}";
//            }
//        }

//        private bool CanGenerateReport(ReportTemplateViewModel template)
//        {
//            return template != null && !IsGenerating;
//        }

//        private async void GenerateReport(ReportTemplateViewModel template)
//        {
//            if (template == null) return;

//            try
//            {
//                IsGenerating = true;
//                StatusMessage = $"Генерация отчета '{template.Name}'...";

//                // Собираем параметры из ViewModel шаблона
//                var parameters = template.Parameters
//                    .Where(p => p.Value != null)
//                    .ToDictionary(p => p.Name, p => p.Value);

//                var report = await _reportService.GenerateReportAsync(template.Id, parameters);

//                // Обновляем список отчетов
//                await LoadDataAsync();

//                StatusMessage = $"Отчет '{template.Name}' поставлен в очередь на генерацию";
//            }
//            catch (Exception ex)
//            {
//                StatusMessage = $"Ошибка генерации отчета: {ex.Message}";
//            }
//            finally
//            {
//                IsGenerating = false;
//            }
//        }

//        private bool CanDownloadReport(GeneratedReportViewModel report)
//        {
//            return report != null && report.IsReady;
//        }

//        private async void DownloadReport(GeneratedReportViewModel report)
//        {
//            if (report == null) return;

//            try
//            {
//                StatusMessage = $"Загрузка отчета '{report.TemplateName}'...";

//                var content = await _reportService.DownloadReportContentAsync(report.ReportId);

//                // Здесь должна быть логика сохранения файла
//                // Например, через SaveFileDialog

//                StatusMessage = $"Отчет '{report.TemplateName}' загружен";
//            }
//            catch (Exception ex)
//            {
//                StatusMessage = $"Ошибка загрузки отчета: {ex.Message}";
//            }
//        }

//        private bool CanDeleteReport(GeneratedReportViewModel report)
//        {
//            return report != null;
//        }

//        private async void DeleteReport(GeneratedReportViewModel report)
//        {
//            if (report == null) return;

//            try
//            {
//                await _reportService.DeleteGeneratedReportAsync(report.ReportId);

//                // Обновляем список
//                GeneratedReports = GeneratedReports.Where(r => r.ReportId != report.ReportId).ToList();

//                StatusMessage = $"Отчет '{report.TemplateName}' удален";
//            }
//            catch (Exception ex)
//            {
//                StatusMessage = $"Ошибка удаления отчета: {ex.Message}";
//            }
//        }

//        private async Task RetryReportAsync(string reportId)
//        {
//            try
//            {
//                await _reportService.RetryFailedReportAsync(reportId);
//                await LoadDataAsync();
//                StatusMessage = "Повторная генерация отчета запущена";
//            }
//            catch (Exception ex)
//            {
//                StatusMessage = $"Ошибка повторной генерации: {ex.Message}";
//            }
//        }

//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    // ViewModel для шаблона отчета
//    public class ReportTemplateViewModel : INotifyPropertyChanged
//    {
//        private readonly ReportTemplate _template;
//        private List<ReportParameterViewModel> _parameters = new();

//        public ReportTemplateViewModel(ReportTemplate template)
//        {
//            _template = template;

//            // Инициализация параметров
//            Parameters = template.Parameters.Select(p => new ReportParameterViewModel(p)).ToList();
//        }

//        public int Id => _template.Id;

//        public string Name
//        {
//            get => _template.Name;
//            set
//            {
//                if (_template.Name != value)
//                {
//                    _template.Name = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public string Description
//        {
//            get => _template.Description;
//            set
//            {
//                if (_template.Description != value)
//                {
//                    _template.Description = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public ReportType Type => _template.Type;

//        public string TypeDisplayName => GetTypeDisplayName(_template.Type);

//        public List<ReportParameterViewModel> Parameters
//        {
//            get => _parameters;
//            set
//            {
//                _parameters = value;
//                OnPropertyChanged();
//            }
//        }

//        public DateTime CreatedAt => _template.CreatedAt;

//        public DateTime UpdatedAt => _template.UpdatedAt;

//        public string TemplatePath => _template.TemplatePath;

//        private string GetTypeDisplayName(ReportType type)
//        {
//            return type switch
//            {
//                ReportType.DailySummary => "Ежедневная сводка",
//                ReportType.CallsignActivity => "Активность позывного",
//                ReportType.AreaAnalysis => "Анализ зоны",
//                ReportType.CommunicationFlow => "Поток коммуникаций",
//                ReportType.AlertSummary => "Сводка алертов",
//                ReportType.PatternAnalysis => "Анализ паттернов",
//                ReportType.Custom => "Пользовательский",
//                _ => "Неизвестный"
//            };
//        }

//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    // ViewModel для параметра отчета
//    public class ReportParameterViewModel : INotifyPropertyChanged
//    {
//        private readonly ReportParameter _parameter;
//        private object _value;
//        private bool _hasError;
//        private string _errorMessage;

//        public ReportParameterViewModel(ReportParameter parameter)
//        {
//            _parameter = parameter;

//            // Устанавливаем значение по умолчанию
//            if (!string.IsNullOrEmpty(parameter.DefaultValue))
//            {
//                Value = ConvertValue(parameter.DefaultValue, parameter.Type);
//            }
//        }

//        public string Name
//        {
//            get => _parameter.Name;
//            set
//            {
//                if (_parameter.Name != value)
//                {
//                    _parameter.Name = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public string DisplayName => GetDisplayName(_parameter.Name);

//        public string Type => _parameter.Type;

//        public object Value
//        {
//            get => _value;
//            set
//            {
//                if (_value != value)
//                {
//                    _value = value;
//                    Validate();
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public bool Required => _parameter.Required;

//        public List<string> Options => _parameter.Options;

//        public bool HasOptions => Options != null && Options.Any();

//        public bool HasError
//        {
//            get => _hasError;
//            set
//            {
//                _hasError = value;
//                OnPropertyChanged();
//            }
//        }

//        public string ErrorMessage
//        {
//            get => _errorMessage;
//            set
//            {
//                _errorMessage = value;
//                OnPropertyChanged();
//            }
//        }

//        private object ConvertValue(string value, string type)
//        {
//            try
//            {
//                return type.ToLower() switch
//                {
//                    "date" => DateTime.Parse(value),
//                    "int" => int.Parse(value),
//                    "bool" => bool.Parse(value),
//                    "string" => value,
//                    "list" => value,
//                    _ => value
//                };
//            }
//            catch
//            {
//                return value;
//            }
//        }

//        private void Validate()
//        {
//            HasError = false;
//            ErrorMessage = null;

//            if (Required && (Value == null || string.IsNullOrEmpty(Value.ToString())))
//            {
//                HasError = true;
//                ErrorMessage = "Обязательное поле";
//                return;
//            }

//            if (HasOptions && Value != null && !Options.Contains(Value.ToString()))
//            {
//                HasError = true;
//                ErrorMessage = $"Допустимые значения: {string.Join(", ", Options)}";
//                return;
//            }
//        }

//        private string GetDisplayName(string name)
//        {
//            var displayNames = new Dictionary<string, string>
//            {
//                ["date"] = "Дата",
//                ["startDate"] = "Дата начала",
//                ["endDate"] = "Дата окончания",
//                ["callsign"] = "Позывной",
//                ["area"] = "Зона",
//                ["format"] = "Формат",
//                ["includeAlerts"] = "Включать алерты",
//                ["includePatterns"] = "Включать паттерны",
//                ["includeNetwork"] = "Включать сеть взаимодействий",
//                ["includeHeatmap"] = "Включать тепловую карту",
//                ["minInteractions"] = "Минимальное количество взаимодействий",
//                ["minSeverity"] = "Минимальная важность"
//            };

//            return displayNames.ContainsKey(name) ? displayNames[name] : name;
//        }

//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    // ViewModel для сгенерированного отчета
//    public class GeneratedReportViewModel : INotifyPropertyChanged
//    {
//        private readonly GeneratedReport _report;
//        private bool _isSelected;

//        public GeneratedReportViewModel(GeneratedReport report)
//        {
//            _report = report;
//        }

//        public string ReportId => _report.ReportId;

//        public string TemplateName => _report.TemplateName;

//        public DateTime GeneratedAt => _report.GeneratedAt;

//        public string GeneratedAtDisplay => GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss");

//        public Dictionary<string, object> Parameters => _report.Parameters;

//        public string ParametersDisplay => FormatParameters(_report.Parameters);

//        public string ContentType => _report.ContentType;

//        public string FileName => _report.FileName;

//        public ReportStatus Status => _report.Status;

//        public string StatusDisplay => GetStatusDisplayName(_report.Status);

//        public string StatusColor => GetStatusColor(_report.Status);

//        public bool IsReady => _report.Status == ReportStatus.Completed;

//        public bool IsPending => _report.Status == ReportStatus.Pending;

//        public bool IsGenerating => _report.Status == ReportStatus.Generating;

//        public bool IsFailed => _report.Status == ReportStatus.Failed;

//        public string ErrorMessage => _report.ErrorMessage;

//        public bool HasError => !string.IsNullOrEmpty(_report.ErrorMessage);

//        public bool IsSelected
//        {
//            get => _isSelected;
//            set
//            {
//                _isSelected = value;
//                OnPropertyChanged();
//            }
//        }

//        private string FormatParameters(Dictionary<string, object> parameters)
//        {
//            if (parameters == null || !parameters.Any())
//                return "Нет параметров";

//            return string.Join(", ", parameters.Select(p => $"{p.Key}: {p.Value}"));
//        }

//        private string GetStatusDisplayName(ReportStatus status)
//        {
//            return status switch
//            {
//                ReportStatus.Pending => "В очереди",
//                ReportStatus.Generating => "Генерируется",
//                ReportStatus.Completed => "Готов",
//                ReportStatus.Failed => "Ошибка",
//                _ => "Неизвестно"
//            };
//        }

//        private string GetStatusColor(ReportStatus status)
//        {
//            return status switch
//            {
//                ReportStatus.Pending => "#FFA500", // Оранжевый
//                ReportStatus.Generating => "#1E90FF", // Синий
//                ReportStatus.Completed => "#32CD32", // Зеленый
//                ReportStatus.Failed => "#FF4500", // Красный
//                _ => "#808080" // Серый
//            };
//        }

//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    // ViewModel для формы генерации отчета
//    public class GenerateReportViewModel : INotifyPropertyChanged
//    {
//        private ReportTemplateViewModel _selectedTemplate;
//        private DateTime _date = DateTime.Today;
//        private DateTime _startDate = DateTime.Today.AddDays(-7);
//        private DateTime _endDate = DateTime.Today;
//        private string _callsign;
//        private string _area;
//        private string _format = "pdf";
//        private bool _includeAlerts = true;
//        private bool _includePatterns = true;
//        private bool _includeNetwork = true;
//        private bool _includeHeatmap = true;
//        private int _minInteractions = 3;
//        private string _minSeverity = "Low";

//        public List<ReportTemplateViewModel> AvailableTemplates { get; set; } = new();

//        public ReportTemplateViewModel SelectedTemplate
//        {
//            get => _selectedTemplate;
//            set
//            {
//                _selectedTemplate = value;
//                OnPropertyChanged();
//                OnPropertyChanged(nameof(IsDailySummary));
//                OnPropertyChanged(nameof(IsCallsignReport));
//                OnPropertyChanged(nameof(IsAreaReport));
//                OnPropertyChanged(nameof(IsCommunicationFlow));
//                OnPropertyChanged(nameof(IsAlertSummary));
//            }
//        }

//        // Общие параметры
//        public DateTime Date
//        {
//            get => _date;
//            set
//            {
//                _date = value;
//                OnPropertyChanged();
//            }
//        }

//        public DateTime StartDate
//        {
//            get => _startDate;
//            set
//            {
//                _startDate = value;
//                OnPropertyChanged();
//            }
//        }

//        public DateTime EndDate
//        {
//            get => _endDate;
//            set
//            {
//                _endDate = value;
//                OnPropertyChanged();
//            }
//        }

//        // Параметры для досье позывного
//        public string Callsign
//        {
//            get => _callsign;
//            set
//            {
//                _callsign = value;
//                OnPropertyChanged();
//            }
//        }

//        // Параметры для анализа зоны
//        public string Area
//        {
//            get => _area;
//            set
//            {
//                _area = value;
//                OnPropertyChanged();
//            }
//        }

//        // Формат отчета
//        public string Format
//        {
//            get => _format;
//            set
//            {
//                _format = value;
//                OnPropertyChanged();
//            }
//        }

//        public List<string> AvailableFormats => new() { "pdf", "excel", "word", "html", "csv", "json" };

//        // Дополнительные параметры
//        public bool IncludeAlerts
//        {
//            get => _includeAlerts;
//            set
//            {
//                _includeAlerts = value;
//                OnPropertyChanged();
//            }
//        }

//        public bool IncludePatterns
//        {
//            get => _includePatterns;
//            set
//            {
//                _includePatterns = value;
//                OnPropertyChanged();
//            }
//        }

//        public bool IncludeNetwork
//        {
//            get => _includeNetwork;
//            set
//            {
//                _includeNetwork = value;
//                OnPropertyChanged();
//            }
//        }

//        public bool IncludeHeatmap
//        {
//            get => _includeHeatmap;
//            set
//            {
//                _includeHeatmap = value;
//                OnPropertyChanged();
//            }
//        }

//        public int MinInteractions
//        {
//            get => _minInteractions;
//            set
//            {
//                _minInteractions = value;
//                OnPropertyChanged();
//            }
//        }

//        public string MinSeverity
//        {
//            get => _minSeverity;
//            set
//            {
//                _minSeverity = value;
//                OnPropertyChanged();
//            }
//        }

//        public List<string> SeverityOptions => new() { "Info", "Low", "Medium", "High", "Critical" };

//        // Флаги для отображения соответствующих полей
//        public bool IsDailySummary => SelectedTemplate?.Type == ReportType.DailySummary;
//        public bool IsCallsignReport => SelectedTemplate?.Type == ReportType.CallsignActivity;
//        public bool IsAreaReport => SelectedTemplate?.Type == ReportType.AreaAnalysis;
//        public bool IsCommunicationFlow => SelectedTemplate?.Type == ReportType.CommunicationFlow;
//        public bool IsAlertSummary => SelectedTemplate?.Type == ReportType.AlertSummary;

//        public Dictionary<string, object> GetParameters()
//        {
//            var parameters = new Dictionary<string, object>();

//            // Базовые параметры
//            if (IsDailySummary)
//            {
//                parameters["date"] = Date.ToString("yyyy-MM-dd");
//            }
//            else
//            {
//                parameters["startDate"] = StartDate.ToString("yyyy-MM-dd");
//                parameters["endDate"] = EndDate.ToString("yyyy-MM-dd");
//            }

//            // Специфичные параметры
//            if (IsCallsignReport && !string.IsNullOrEmpty(Callsign))
//            {
//                parameters["callsign"] = Callsign;
//                parameters["includeNetwork"] = IncludeNetwork;
//            }

//            if (IsAreaReport && !string.IsNullOrEmpty(Area))
//            {
//                parameters["area"] = Area;
//                parameters["includeHeatmap"] = IncludeHeatmap;
//            }

//            if (IsCommunicationFlow)
//            {
//                parameters["minInteractions"] = MinInteractions;
//            }

//            if (IsAlertSummary)
//            {
//                parameters["minSeverity"] = MinSeverity;
//            }

//            // Общие параметры
//            parameters["format"] = Format;

//            if (SelectedTemplate?.Name == "Ежедневная сводка")
//            {
//                parameters["includeAlerts"] = IncludeAlerts;
//                parameters["includePatterns"] = IncludePatterns;
//            }

//            return parameters;
//        }

//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    // ViewModel для статистики отчетов
//    public class ReportStatisticsViewModel : INotifyPropertyChanged
//    {
//        private readonly IReportService _reportService;
//        private DateTime _startDate = DateTime.Today.AddDays(-30);
//        private DateTime _endDate = DateTime.Today;
//        private bool _isLoading;
//        private string _statusMessage;

//        // Статистика
//        private int _totalReports;
//        private int _completedReports;
//        private int _failedReports;
//        private int _pendingReports;
//        private TimeSpan _averageGenerationTime;
//        private Dictionary<string, int> _reportsByTemplate = new();
//        private Dictionary<string, int> _reportsByFormat = new();

//        // Команды
//        public ICommand RefreshStatisticsCommand { get; }
//        public ICommand ExportStatisticsCommand { get; }

//        public ReportStatisticsViewModel(IReportService reportService)
//        {
//            _reportService = reportService;

//            RefreshStatisticsCommand = new RelayCommand(async () => await LoadStatisticsAsync());
//            ExportStatisticsCommand = new RelayCommand(ExportStatistics);

//            LoadStatisticsAsync().ConfigureAwait(false);
//        }

//        public DateTime StartDate
//        {
//            get => _startDate;
//            set
//            {
//                _startDate = value;
//                OnPropertyChanged();
//            }
//        }

//        public DateTime EndDate
//        {
//            get => _endDate;
//            set
//            {
//                _endDate = value;
//                OnPropertyChanged();
//            }
//        }

//        public bool IsLoading
//        {
//            get => _isLoading;
//            set
//            {
//                _isLoading = value;
//                OnPropertyChanged();
//            }
//        }

//        public string StatusMessage
//        {
//            get => _statusMessage;
//            set
//            {
//                _statusMessage = value;
//                OnPropertyChanged();
//            }
//        }

//        public int TotalReports
//        {
//            get => _totalReports;
//            set
//            {
//                _totalReports = value;
//                OnPropertyChanged();
//            }
//        }

//        public int CompletedReports
//        {
//            get => _completedReports;
//            set
//            {
//                _completedReports = value;
//                OnPropertyChanged();
//            }
//        }

//        public int FailedReports
//        {
//            get => _failedReports;
//            set
//            {
//                _failedReports = value;
//                OnPropertyChanged();
//            }
//        }

//        public int PendingReports
//        {
//            get => _pendingReports;
//            set
//            {
//                _pendingReports = value;
//                OnPropertyChanged();
//            }
//        }

//        public TimeSpan AverageGenerationTime
//        {
//            get => _averageGenerationTime;
//            set
//            {
//                _averageGenerationTime = value;
//                OnPropertyChanged();
//                OnPropertyChanged(nameof(AverageGenerationTimeDisplay));
//            }
//        }

//        public string AverageGenerationTimeDisplay =>
//            AverageGenerationTime.TotalSeconds > 0
//                ? $"{AverageGenerationTime.TotalSeconds:F1} сек"
//                : "Нет данных";

//        public Dictionary<string, int> ReportsByTemplate
//        {
//            get => _reportsByTemplate;
//            set
//            {
//                _reportsByTemplate = value;
//                OnPropertyChanged();
//            }
//        }

//        public Dictionary<string, int> ReportsByFormat
//        {
//            get => _reportsByFormat;
//            set
//            {
//                _reportsByFormat = value;
//                OnPropertyChanged();
//            }
//        }

//        public double SuccessRate => TotalReports > 0 ? (double)CompletedReports / TotalReports * 100 : 0;

//        public double FailureRate => TotalReports > 0 ? (double)FailedReports / TotalReports * 100 : 0;

//        private async Task LoadStatisticsAsync()
//        {
//            try
//            {
//                IsLoading = true;
//                StatusMessage = "Загрузка статистики...";

//                var stats = await _reportService.GetReportStatisticsAsync(StartDate, EndDate);

//                TotalReports = stats.TotalReports;
//                CompletedReports = stats.CompletedReports;
//                FailedReports = stats.FailedReports;
//                PendingReports = stats.PendingReports;
//                AverageGenerationTime = stats.AverageGenerationTime;

//                ReportsByTemplate = stats.ReportsByTemplate
//                    .OrderByDescending(kv => kv.Value)
//                    .ToDictionary(kv => kv.Key, kv => kv.Value);

//                ReportsByFormat = stats.ReportsByFormat
//                    .OrderByDescending(kv => kv.Value)
//                    .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);

//                StatusMessage = $"Статистика загружена: {TotalReports} отчетов";
//            }
//            catch (Exception ex)
//            {
//                StatusMessage = $"Ошибка загрузки статистики: {ex.Message}";
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        private void ExportStatistics()
//        {
//            // Логика экспорта статистики в файл
//            StatusMessage = "Экспорт статистики...";
//        }

//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    // ViewModel для данных ежедневной сводки
//    public class DailySummaryViewModel : INotifyPropertyChanged
//    {
//        private readonly IReportService _reportService;
//        private DateTime _selectedDate = DateTime.Today;
//        private bool _isLoading;
//        private DailySummaryReport _reportData;

//        public DailySummaryViewModel(IReportService reportService)
//        {
//            _reportService = reportService;
//        }

//        public DateTime SelectedDate
//        {
//            get => _selectedDate;
//            set
//            {
//                _selectedDate = value;
//                OnPropertyChanged();
//                LoadDailySummaryAsync().ConfigureAwait(false);
//            }
//        }

//        public bool IsLoading
//        {
//            get => _isLoading;
//            set
//            {
//                _isLoading = value;
//                OnPropertyChanged();
//            }
//        }

//        public DailySummaryReport ReportData
//        {
//            get => _reportData;
//            set
//            {
//                _reportData = value;
//                OnPropertyChanged();
//                OnPropertyChanged(nameof(HasData));
//            }
//        }

//        public bool HasData => ReportData != null;

//        public string TotalMessagesDisplay => ReportData?.TotalMessages.ToString("N0") ?? "0";

//        public string UniqueCallsignsDisplay => ReportData?.UniqueCallsigns.ToString("N0") ?? "0";

//        public string ActiveAreasDisplay => ReportData?.ActiveAreas.ToString("N0") ?? "0";

//        public List<CallsignActivity> TopCallsigns => ReportData?.TopCallsigns ?? new();

//        public List<AreaActivity> TopAreas => ReportData?.TopAreas ?? new();

//        public List<AlertSummary> Alerts => ReportData?.Alerts ?? new();

//        public List<PatternSummary> DetectedPatterns => ReportData?.DetectedPatterns ?? new();

//        public List<KeyObservation> Observations => ReportData?.Observations ?? new();

//        private async Task LoadDailySummaryAsync()
//        {
//            try
//            {
//                IsLoading = true;
//                ReportData = await _reportService.GetDailySummaryDataAsync(SelectedDate);
//            }
//            catch (Exception ex)
//            {
//                // Логирование ошибки
//                Console.WriteLine($"Error loading daily summary: {ex.Message}");
//                ReportData = null;
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    // ViewModel для досье позывного
//    public class CallsignDossierViewModel : INotifyPropertyChanged
//    {
//        private readonly IReportService _reportService;
//        private string _selectedCallsign;
//        private DateTime _startDate = DateTime.Today.AddDays(-30);
//        private DateTime _endDate = DateTime.Today;
//        private bool _isLoading;
//        private CallsignDossier _dossierData;

//        public CallsignDossierViewModel(IReportService reportService)
//        {
//            _reportService = reportService;
//        }

//        public string SelectedCallsign
//        {
//            get => _selectedCallsign;
//            set
//            {
//                _selectedCallsign = value;
//                OnPropertyChanged();
//                LoadDossierAsync().ConfigureAwait(false);
//            }
//        }

//        public DateTime StartDate
//        {
//            get => _startDate;
//            set
//            {
//                _startDate = value;
//                OnPropertyChanged();
//                LoadDossierAsync().ConfigureAwait(false);
//            }
//        }

//        public DateTime EndDate
//        {
//            get => _endDate;
//            set
//            {
//                _endDate = value;
//                OnPropertyChanged();
//                LoadDossierAsync().ConfigureAwait(false);
//            }
//        }

//        public bool IsLoading
//        {
//            get => _isLoading;
//            set
//            {
//                _isLoading = value;
//                OnPropertyChanged();
//            }
//        }

//        public CallsignDossier DossierData
//        {
//            get => _dossierData;
//            set
//            {
//                _dossierData = value;
//                OnPropertyChanged();
//                OnPropertyChanged(nameof(HasData));
//            }
//        }

//        public bool HasData => DossierData != null;

//        private async Task LoadDossierAsync()
//        {
//            if (string.IsNullOrEmpty(SelectedCallsign))
//                return;

//            try
//            {
//                IsLoading = true;
//                DossierData = await _reportService.GetCallsignDossierDataAsync(
//                    SelectedCallsign, StartDate, EndDate);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error loading callsign dossier: {ex.Message}");
//                DossierData = null;
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    // ViewModel для анализа зоны
//    public class AreaAnalysisViewModel : INotifyPropertyChanged
//    {
//        private readonly IReportService _reportService;
//        private string _selectedArea;
//        private DateTime _startDate = DateTime.Today.AddDays(-30);
//        private DateTime _endDate = DateTime.Today;
//        private bool _isLoading;
//        private AreaActivityReport _reportData;

//        public AreaAnalysisViewModel(IReportService reportService)
//        {
//            _reportService = reportService;
//        }

//        public string SelectedArea
//        {
//            get => _selectedArea;
//            set
//            {
//                _selectedArea = value;
//                OnPropertyChanged();
//                LoadAreaAnalysisAsync().ConfigureAwait(false);
//            }
//        }

//        public DateTime StartDate
//        {
//            get => _startDate;
//            set
//            {
//                _startDate = value;
//                OnPropertyChanged();
//                LoadAreaAnalysisAsync().ConfigureAwait(false);
//            }
//        }

//        public DateTime EndDate
//        {
//            get => _endDate;
//            set
//            {
//                _endDate = value;
//                OnPropertyChanged();
//                LoadAreaAnalysisAsync().ConfigureAwait(false);
//            }
//        }

//        public bool IsLoading
//        {
//            get => _isLoading;
//            set
//            {
//                _isLoading = value;
//                OnPropertyChanged();
//            }
//        }

//        public AreaActivityReport ReportData
//        {
//            get => _reportData;
//            set
//            {
//                _reportData = value;
//                OnPropertyChanged();
//                OnPropertyChanged(nameof(HasData));
//            }
//        }

//        public bool HasData => ReportData != null;

//        private async Task LoadAreaAnalysisAsync()
//        {
//            if (string.IsNullOrEmpty(SelectedArea))
//                return;

//            try
//            {
//                IsLoading = true;
//                ReportData = await _reportService.GetAreaActivityDataAsync(
//                    SelectedArea, StartDate, EndDate);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error loading area analysis: {ex.Message}");
//                ReportData = null;
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }
//}