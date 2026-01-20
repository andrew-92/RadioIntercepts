// WpfApp/ViewModels/CodeAnalysisViewModel.cs
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
    public partial class CodeAnalysisViewModel : ObservableObject
    {
        private readonly ICodeAnalysisService _codeService;
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<CodeTerm> _codeTerms = new();

        [ObservableProperty]
        private ObservableCollection<CodeUsageStatistic> _usageStatistics = new();

        [ObservableProperty]
        private ObservableCollection<SlangPattern> _slangPatterns = new();

        [ObservableProperty]
        private ObservableCollection<CallsignVocabularyProfile> _vocabularyProfiles = new();

        [ObservableProperty]
        private ObservableCollection<CodeSimilarityResult> _similarityResults = new();

        [ObservableProperty]
        private ObservableCollection<List<string>> _terminologyClusters = new();

        [ObservableProperty]
        private ObservableCollection<string> _allCallsigns = new();

        [ObservableProperty]
        private ObservableCollection<CodeTermCategory> _categories = new();

        [ObservableProperty]
        private string? _selectedCallsign1;

        [ObservableProperty]
        private string? _selectedCallsign2;

        [ObservableProperty]
        private CodeTermCategory? _selectedCategory;

        [ObservableProperty]
        private DateTime? _dateFrom;

        [ObservableProperty]
        private DateTime? _dateTo;

        [ObservableProperty]
        private string _filterText = string.Empty;

        [ObservableProperty]
        private int _minFrequency = 5;

        [ObservableProperty]
        private bool _showOnlyActive = true;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedTab = "Terms";

        [ObservableProperty]
        private CodeTerm? _selectedTerm;

        [ObservableProperty]
        private CodeUsageStatistic? _selectedStatistic;

        [ObservableProperty]
        private CallsignVocabularyProfile? _selectedProfile;

        [ObservableProperty]
        private Dictionary<string, string> _glossary = new();

        public CodeAnalysisViewModel(ICodeAnalysisService codeService, AppDbContext context)
        {
            _codeService = codeService;
            _context = context;

            DateTo = DateTime.Today;
            DateFrom = DateTime.Today.AddDays(-30);

            InitializeCategories();
            _ = InitializeAsync();
        }

        private void InitializeCategories()
        {
            Categories.Clear();
            foreach (CodeTermCategory category in Enum.GetValues(typeof(CodeTermCategory)))
            {
                Categories.Add(category);
            }
        }

        private async Task InitializeAsync()
        {
            await LoadAllCallsignsAsync();
            await LoadCodeTermsAsync();
            await BuildGlossaryAsync();
        }

        [RelayCommand]
        private async Task LoadAllCallsignsAsync()
        {
            try
            {
                var callsigns = await _context.Callsigns
                    .OrderBy(c => c.Name)
                    .Select(c => c.Name)
                    .ToListAsync();

                AllCallsigns.Clear();
                foreach (var callsign in callsigns)
                {
                    AllCallsigns.Add(callsign);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки позывных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task LoadCodeTermsAsync()
        {
            IsLoading = true;
            try
            {
                var terms = await _codeService.ExtractCodeTermsAsync(DateFrom, DateTo);

                CodeTerms.Clear();
                foreach (var term in terms)
                {
                    if (!string.IsNullOrWhiteSpace(FilterText) &&
                        !term.Term.Contains(FilterText, StringComparison.OrdinalIgnoreCase) &&
                        !term.Category.ToString().Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (SelectedCategory.HasValue && term.Category != SelectedCategory.Value)
                        continue;

                    if (ShowOnlyActive && !term.IsActive)
                        continue;

                    CodeTerms.Add(term);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки терминов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadUsageStatisticsAsync()
        {
            IsLoading = true;
            try
            {
                var statistics = await _codeService.GetCodeUsageStatisticsAsync(
                    FilterText, SelectedCategory);

                UsageStatistics.Clear();
                foreach (var stat in statistics)
                {
                    UsageStatistics.Add(stat);
                }
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

        [RelayCommand]
        private async Task LoadSlangPatternsAsync()
        {
            IsLoading = true;
            try
            {
                var patterns = await _codeService.DetectSlangPatternsAsync(MinFrequency);

                SlangPatterns.Clear();
                foreach (var pattern in patterns)
                {
                    if (!string.IsNullOrWhiteSpace(FilterText) &&
                        !pattern.Pattern.Contains(FilterText, StringComparison.OrdinalIgnoreCase) &&
                        !pattern.Meaning.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                        continue;

                    SlangPatterns.Add(pattern);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сленговых паттернов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadCallsignSpecificSlangAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedCallsign1))
            {
                MessageBox.Show("Выберите позывной для анализа", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                var patterns = await _codeService.FindCallsignSpecificSlangAsync(SelectedCallsign1);

                SlangPatterns.Clear();
                foreach (var pattern in patterns)
                {
                    SlangPatterns.Add(pattern);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки специфичного сленга: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadVocabularyProfileAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedCallsign1))
            {
                MessageBox.Show("Выберите позывной для анализа", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                var profile = await _codeService.GetCallsignVocabularyProfileAsync(SelectedCallsign1);

                VocabularyProfiles.Clear();
                VocabularyProfiles.Add(profile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки профиля лексики: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CompareVocabulariesAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedCallsign1) || string.IsNullOrWhiteSpace(SelectedCallsign2))
            {
                MessageBox.Show("Выберите два позывных для сравнения", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                var similarity = await _codeService.CalculateVocabularySimilarityAsync(
                    SelectedCallsign1, SelectedCallsign2);

                SimilarityResults.Clear();
                SimilarityResults.Add(similarity);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сравнения лексики: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ClusterByTerminologyAsync()
        {
            IsLoading = true;
            try
            {
                var clusters = await _codeService.ClusterCallsignsByTerminologyAsync(2);

                TerminologyClusters.Clear();
                foreach (var cluster in clusters)
                {
                    TerminologyClusters.Add(cluster);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка кластеризации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadNewTermsAsync()
        {
            if (!DateFrom.HasValue)
            {
                MessageBox.Show("Укажите начальную дату для поиска новых терминов", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                var newTerms = await _codeService.DetectNewTermsAsync(DateFrom.Value, 0.7);

                CodeTerms.Clear();
                foreach (var term in newTerms)
                {
                    CodeTerms.Add(term);
                }

                MessageBox.Show($"Найдено {newTerms.Count} новых терминов", "Результат",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска новых терминов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AnalyzeTermTrendsAsync()
        {
            if (!DateFrom.HasValue || !DateTo.HasValue)
            {
                MessageBox.Show("Укажите период для анализа трендов", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                var trends = await _codeService.AnalyzeTermTrendsAsync(DateFrom.Value, DateTo.Value);

                // Преобразуем в статистику для отображения
                var trendStatistics = trends.Select(kv => new CodeUsageStatistic
                {
                    Term = kv.Key,
                    Trend = kv.Value
                }).ToList();

                UsageStatistics.Clear();
                foreach (var stat in trendStatistics)
                {
                    UsageStatistics.Add(stat);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка анализа трендов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task BuildGlossaryAsync()
        {
            IsLoading = true;
            try
            {
                var glossary = await _codeService.BuildGlossaryAsync(0.0005);
                Glossary = glossary;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка построения глоссария: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task FindTermAssociationsAsync()
        {
            if (SelectedTerm == null)
            {
                MessageBox.Show("Выберите термин для поиска ассоциаций", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                var associations = await _codeService.FindTermAssociationsAsync(SelectedTerm.Term, 10);

                // Отображаем результаты
                if (associations.Any())
                {
                    var message = $"Ассоциации для термина '{SelectedTerm.Term}':\n\n";
                    foreach (var (term, contexts) in associations)
                    {
                        message += $"{term} ({contexts.Count} примеров):\n";
                        foreach (var context in contexts.Take(2))
                        {
                            message += $"  • {context}\n";
                        }
                        message += "\n";
                    }

                    MessageBox.Show(message, "Ассоциации термина",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Для термина '{SelectedTerm.Term}' не найдено ассоциаций",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска ассоциаций: {ex.Message}",
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
            FilterText = string.Empty;
            SelectedCategory = null;
            SelectedCallsign1 = null;
            SelectedCallsign2 = null;
            DateFrom = DateTime.Today.AddDays(-30);
            DateTo = DateTime.Today;
            MinFrequency = 5;
            ShowOnlyActive = true;

            CodeTerms.Clear();
            UsageStatistics.Clear();
            SlangPatterns.Clear();
            VocabularyProfiles.Clear();
            SimilarityResults.Clear();
            TerminologyClusters.Clear();
        }

        [RelayCommand]
        private async Task ExportAnalysisAsync(string format = "csv")
        {
            try
            {
                string content = SelectedTab switch
                {
                    "Terms" => ExportTermsToCsv(),
                    "Statistics" => ExportStatisticsToCsv(),
                    "Slang" => ExportSlangToCsv(),
                    "Profiles" => ExportProfilesToCsv(),
                    "Similarity" => ExportSimilarityToCsv(),
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
                    FileName = $"code_analysis_{SelectedTab}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialog.FileName, content);
                    MessageBox.Show($"Анализ экспортирован в {dialog.FileName}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ExportTermsToCsv()
        {
            var lines = new List<string>
            {
                "Термин,Категория,Частота,Отличительность,Активен,Первое появление,Последнее появление,Контексты"
            };

            foreach (var term in CodeTerms)
            {
                lines.Add(
                    $"\"{term.Term}\"," +
                    $"{term.Category}," +
                    $"{term.FrequencyScore:F6}," +
                    $"{term.DistinctivenessScore:F3}," +
                    $"{term.IsActive}," +
                    $"{term.FirstSeen:yyyy-MM-dd}," +
                    $"{term.LastSeen:yyyy-MM-dd}," +
                    $"\"{string.Join("; ", term.TypicalContexts)}\""
                );
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string ExportStatisticsToCsv()
        {
            var lines = new List<string>
            {
                "Термин,Категория,Всего использований,Уникальные позывные,Уникальные зоны,Первое использование,Последнее использование,Среднее в день,Тренд"
            };

            foreach (var stat in UsageStatistics)
            {
                lines.Add(
                    $"\"{stat.Term}\"," +
                    $"{stat.Category}," +
                    $"{stat.TotalUsageCount}," +
                    $"{stat.UniqueCallsignsCount}," +
                    $"{stat.UniqueAreasCount}," +
                    $"{stat.FirstUsage:yyyy-MM-dd}," +
                    $"{stat.LastUsage:yyyy-MM-dd}," +
                    $"{stat.AverageMessagesPerDay:F2}," +
                    $"{stat.Trend:F3}"
                );
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string ExportSlangToCsv()
        {
            var lines = new List<string>
            {
                "Паттерн,Значение,Примеры,Количество примеров,Уверенность,Позывные,Первое наблюдение,Последнее наблюдение"
            };

            foreach (var pattern in SlangPatterns)
            {
                lines.Add(
                    $"\"{pattern.Pattern}\"," +
                    $"\"{EscapeCsv(pattern.Meaning)}\"," +
                    $"\"{string.Join("; ", pattern.Examples)}\"," +
                    $"{pattern.ExampleCount}," +
                    $"{pattern.Confidence:F3}," +
                    $"\"{string.Join("; ", pattern.AssociatedCallsigns)}\"," +
                    $"{pattern.FirstObserved:yyyy-MM-dd}," +
                    $"{pattern.LastObserved:yyyy-MM-dd}"
                );
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string ExportProfilesToCsv()
        {
            var lines = new List<string>
            {
                "Позывной,Всего слов,Уникальных слов,Богатство лексики,Частые термины,Отличительные термины"
            };

            foreach (var profile in VocabularyProfiles)
            {
                lines.Add(
                    $"{profile.Callsign}," +
                    $"{profile.TotalWordsUsed}," +
                    $"{profile.UniqueWordsCount}," +
                    $"{profile.VocabularyRichness:F3}," +
                    $"\"{string.Join("; ", profile.MostFrequentTerms.Take(5))}\"," +
                    $"\"{string.Join("; ", profile.DistinctiveTerms.Take(5))}\""
                );
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string ExportSimilarityToCsv()
        {
            var lines = new List<string>
            {
                "Позывной1,Позывной2,Коэффициент сходства,Общие термины,Уникальные для 1,Уникальные для 2"
            };

            foreach (var similarity in SimilarityResults)
            {
                lines.Add(
                    $"{similarity.Callsign1}," +
                    $"{similarity.Callsign2}," +
                    $"{similarity.SimilarityScore:F3}," +
                    $"\"{string.Join("; ", similarity.CommonTerms.Take(5))}\"," +
                    $"\"{string.Join("; ", similarity.UniqueToCallsign1.Take(5))}\"," +
                    $"\"{string.Join("; ", similarity.UniqueToCallsign2.Take(5))}\""
                );
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string ExportClustersToCsv()
        {
            var lines = new List<string> { "Кластер,Позывные" };

            for (int i = 0; i < TerminologyClusters.Count; i++)
            {
                lines.Add(
                    $"Кластер {i + 1}," +
                    $"\"{string.Join("; ", TerminologyClusters[i])}\""
                );
            }

            return string.Join(Environment.NewLine, lines);
        }

        partial void OnSelectedTabChanged(string value)
        {
            switch (value)
            {
                case "Terms":
                    _ = LoadCodeTermsAsync();
                    break;
                case "Statistics":
                    _ = LoadUsageStatisticsAsync();
                    break;
                case "Slang":
                    _ = LoadSlangPatternsAsync();
                    break;
                case "Profiles":
                    VocabularyProfiles.Clear();
                    break;
                case "Similarity":
                    SimilarityResults.Clear();
                    break;
                case "Clusters":
                    _ = ClusterByTerminologyAsync();
                    break;
            }
        }

        partial void OnFilterTextChanged(string value)
        {
            switch (SelectedTab)
            {
                case "Terms":
                    _ = LoadCodeTermsAsync();
                    break;
                case "Statistics":
                    _ = LoadUsageStatisticsAsync();
                    break;
                case "Slang":
                    _ = LoadSlangPatternsAsync();
                    break;
            }
        }

        partial void OnSelectedCategoryChanged(CodeTermCategory? value)
        {
            switch (SelectedTab)
            {
                case "Terms":
                    _ = LoadCodeTermsAsync();
                    break;
                case "Statistics":
                    _ = LoadUsageStatisticsAsync();
                    break;
            }
        }

        partial void OnDateFromChanged(DateTime? value)
        {
            _ = LoadCodeTermsAsync();
        }

        partial void OnDateToChanged(DateTime? value)
        {
            _ = LoadCodeTermsAsync();
        }

        partial void OnMinFrequencyChanged(int value)
        {
            if (SelectedTab == "Slang")
                _ = LoadSlangPatternsAsync();
        }

        partial void OnShowOnlyActiveChanged(bool value)
        {
            if (SelectedTab == "Terms")
                _ = LoadCodeTermsAsync();
        }

        private string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Экранируем кавычки
            return text.Replace("\"", "\"\"");
        }
    }
}