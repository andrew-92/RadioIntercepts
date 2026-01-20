// WpfApp/ViewModels/SemanticSearchViewModel.cs
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
using System.DirectoryServices;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RadioIntercepts.WpfApp.ViewModels
{
    public partial class SemanticSearchViewModel : ObservableObject
    {
        private readonly ISemanticSearchService _searchService;
        private readonly AppDbContext _context;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private DateTime? _dateFrom;

        [ObservableProperty]
        private DateTime? _dateTo;

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
        private MessageType? _selectedMessageType;

        [ObservableProperty]
        private double _minSimilarity = 0.3;

        [ObservableProperty]
        private int _maxResults = 100;

        [ObservableProperty]
        private ObservableCollection<SemanticSearchResult> _searchResults = new();

        [ObservableProperty]
        private ObservableCollection<KeywordAnalysis> _keywordAnalysis = new();

        [ObservableProperty]
        private ObservableCollection<MessageCategory> _messageCategories = new();

        [ObservableProperty]
        private ObservableCollection<MessageCluster> _messageClusters = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedTab = "Search";

        [ObservableProperty]
        private string _exampleText = string.Empty;

        [ObservableProperty]
        private int _maxSimilarExamples = 5;

        [ObservableProperty]
        private bool _includeOpposite = false;

        [ObservableProperty]
        private ObservableCollection<MessageType> _messageTypes = new();

        [ObservableProperty]
        private ObservableCollection<string> _suggestedQueries = new();

        public SemanticSearchViewModel(
            ISemanticSearchService searchService,
            AppDbContext context)
        {
            _searchService = searchService;
            _context = context;

            DateTo = DateTime.Today;
            DateFrom = DateTime.Today.AddDays(-30);

            InitializeMessageTypes();
            _ = InitializeAsync();
        }

        private void InitializeMessageTypes()
        {
            MessageTypes.Clear();
            foreach (MessageType type in Enum.GetValues(typeof(MessageType)))
            {
                MessageTypes.Add(type);
            }
        }

        private async Task InitializeAsync()
        {
            await LoadFilterDataAsync();
            await LoadMessageCategoriesAsync();
            LoadSuggestedQueries();
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
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                MessageBox.Show("Введите поисковый запрос", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            SearchResults.Clear();

            try
            {
                var query = new SemanticSearchQuery
                {
                    Query = SearchQuery,
                    DateFrom = DateFrom,
                    DateTo = DateTo,
                    Area = SelectedArea,
                    Frequency = SelectedFrequency,
                    Callsigns = SelectedCallsigns.Any() ? SelectedCallsigns.ToList() : null,
                    MessageType = SelectedMessageType,
                    MinSimilarity = MinSimilarity,
                    MaxResults = MaxResults
                };

                var results = await _searchService.SearchAsync(query);

                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }

                if (!SearchResults.Any())
                {
                    MessageBox.Show("По вашему запросу ничего не найдено", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SearchByExampleAsync()
        {
            if (string.IsNullOrWhiteSpace(ExampleText))
            {
                MessageBox.Show("Введите пример текста для поиска", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            SearchResults.Clear();

            try
            {
                var request = new SearchByExampleRequest
                {
                    ExampleText = ExampleText,
                    MaxSimilarExamples = MaxSimilarExamples,
                    IncludeOpposite = IncludeOpposite
                };

                var results = await _searchService.SearchByExampleAsync(request);

                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }

                if (!SearchResults.Any())
                {
                    MessageBox.Show("По вашему примеру ничего не найдено", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска по примеру: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AnalyzeKeywordsAsync()
        {
            IsLoading = true;
            try
            {
                var analysis = await _searchService.AnalyzeKeywordsAsync(DateFrom, DateTo);
                KeywordAnalysis.Clear();

                foreach (var item in analysis)
                {
                    KeywordAnalysis.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка анализа ключевых слов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadMessageCategoriesAsync()
        {
            IsLoading = true;
            try
            {
                var categories = await _searchService.GetMessageCategoriesAsync();
                MessageCategories.Clear();

                foreach (var category in categories)
                {
                    MessageCategories.Add(category);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ClusterMessagesAsync()
        {
            IsLoading = true;
            try
            {
                var clusters = await _searchService.ClusterMessagesByContentAsync(5);
                MessageClusters.Clear();

                foreach (var cluster in clusters)
                {
                    MessageClusters.Add(cluster);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка кластеризации сообщений: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void UseSuggestedQuery(string query)
        {
            SearchQuery = query;
            _ = SearchAsync();
        }

        [RelayCommand]
        private void UseCategoryAsQuery(MessageCategory category)
        {
            if (category?.Keywords?.Any() == true)
            {
                SearchQuery = string.Join(" ", category.Keywords.Take(3));
                _ = SearchAsync();
            }
        }

        [RelayCommand]
        private void UseClusterAsQuery(MessageCluster cluster)
        {
            if (cluster?.Keywords?.Any() == true)
            {
                SearchQuery = string.Join(" ", cluster.Keywords.Take(3));
                _ = SearchAsync();
            }
        }

        [RelayCommand]
        private async Task FindSimilarToSelectedAsync()
        {
            var selectedResult = SearchResults.FirstOrDefault(r => r.IsSelected);
            if (selectedResult == null)
            {
                MessageBox.Show("Выберите сообщение для поиска похожих", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                var similarMessages = await _searchService.FindSimilarMessagesAsync(
                    selectedResult.Message.Id, 10);

                // Показываем похожие сообщения
                var similarResults = new List<SemanticSearchResult>();
                foreach (var message in similarMessages)
                {
                    similarResults.Add(new SemanticSearchResult
                    {
                        Message = message,
                        SimilarityScore = 0.8, // Примерное значение
                        Snippet = message.Dialog.Length > 100
                            ? message.Dialog.Substring(0, 100) + "..."
                            : message.Dialog
                    });
                }

                SearchResults.Clear();
                foreach (var result in similarResults)
                {
                    SearchResults.Add(result);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска похожих сообщений: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchQuery = string.Empty;
            ExampleText = string.Empty;
            SelectedArea = null;
            SelectedFrequency = null;
            SelectedCallsigns.Clear();
            SelectedMessageType = null;
            DateFrom = DateTime.Today.AddDays(-30);
            DateTo = DateTime.Today;
            MinSimilarity = 0.3;
            MaxResults = 100;
            SearchResults.Clear();
        }

        [RelayCommand]
        private void ClearSearchResults()
        {
            SearchResults.Clear();
        }

        [RelayCommand]
        private async Task ExportResultsAsync(string format = "csv")
        {
            try
            {
                string content = SelectedTab switch
                {
                    "Search" => ExportSearchResultsToCsv(),
                    "Keywords" => ExportKeywordsToCsv(),
                    "Categories" => ExportCategoriesToCsv(),
                    "Clusters" => ExportClustersToCsv(),
                    _ => ""
                };

                if (string.IsNullOrEmpty(content))
                {
                    MessageBox.Show("Нет данных для экспорта", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|JSON файлы (*.json)|*.json",
                    FileName = $"semantic_search_{SelectedTab}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialog.FileName, content);
                    MessageBox.Show($"Результаты экспортированы в {dialog.FileName}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ExportSearchResultsToCsv()
        {
            var lines = new List<string>
            {
                "Дата,Зона,Частота,Позывные,Тип,Сходство,Совпавшие слова,Сниппет"
            };

            lines.AddRange(SearchResults.Select(r =>
                $"{r.Message.DateTime:yyyy-MM-dd HH:mm}," +
                $"\"{r.Message.Area.Name}\"," +
                $"\"{r.Message.Frequency.Value}\"," +
                $"\"{r.Message.CallsignsText}\"," +
                $"\"{r.DetectedType?.ToString() ?? ""}\"," +
                $"{r.SimilarityScore:F3}," +
                $"\"{string.Join("; ", r.MatchedKeywords)}\"," +
                $"\"{EscapeCsv(r.Snippet)}\""));

            return string.Join(Environment.NewLine, lines);
        }

        private string ExportKeywordsToCsv()
        {
            var lines = new List<string>
            {
                "Ключевое слово,Частота,TF-IDF,Позывные,Зоны,Первое появление,Последнее появление"
            };

            lines.AddRange(KeywordAnalysis.Select(k =>
                $"\"{k.Keyword}\"," +
                $"{k.Frequency}," +
                $"{k.TFIDF:F4}," +
                $"\"{string.Join("; ", k.RelatedCallsigns.Take(5))}\"," +
                $"\"{string.Join("; ", k.RelatedAreas.Take(3))}\"," +
                $"{k.FirstSeen:yyyy-MM-dd}," +
                $"{k.LastSeen:yyyy-MM-dd}"));

            return string.Join(Environment.NewLine, lines);
        }

        private string ExportCategoriesToCsv()
        {
            var lines = new List<string>
            {
                "Категория,Описание,Ключевые слова,Примеры,Количество сообщений"
            };

            lines.AddRange(MessageCategories.Select(c =>
                $"\"{c.Name}\"," +
                $"\"{EscapeCsv(c.Description)}\"," +
                $"\"{string.Join("; ", c.Keywords)}\"," +
                $"\"{string.Join("; ", c.ExamplePhrases)}\"," +
                $"{c.MessageCount}"));

            return string.Join(Environment.NewLine, lines);
        }

        private string ExportClustersToCsv()
        {
            var lines = new List<string>
            {
                "Кластер,Ключевые слова,Размер,Среднее сходство,Сообщения"
            };

            lines.AddRange(MessageClusters.Select(c =>
                $"{c.Id}," +
                $"\"{string.Join("; ", c.Keywords.Take(10))}\"," +
                $"{c.Size}," +
                $"{c.AverageSimilarity:F3}," +
                $"\"{string.Join("; ", c.Messages.Take(3).Select(m => $"{m.DateTime:HH:mm}: {Truncate(m.Dialog, 50)}"))}\""));

            return string.Join(Environment.NewLine, lines);
        }

        private void LoadSuggestedQueries()
        {
            SuggestedQueries.Clear();

            var suggestions = new[]
            {
                "координаты цели",
                "раненые нужна помощь",
                "техника на позиции",
                "атака отход",
                "боеприпасы топливо",
                "обнаружил противника",
                "пеленг азимут",
                "состояние техники",
                "медицинская помощь",
                "передислокация войск"
            };

            foreach (var suggestion in suggestions)
            {
                SuggestedQueries.Add(suggestion);
            }
        }

        private string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Экранируем кавычки
            return text.Replace("\"", "\"\"");
        }

        private string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }

        partial void OnSelectedTabChanged(string value)
        {
            switch (value)
            {
                case "Keywords":
                    _ = AnalyzeKeywordsAsync();
                    break;
                case "Categories":
                    _ = LoadMessageCategoriesAsync();
                    break;
                case "Clusters":
                    _ = ClusterMessagesAsync();
                    break;
            }
        }
    }

    // Расширим SemanticSearchResult для поддержки выделения в UI
    public partial class SemanticSearchResult : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;
    }
}