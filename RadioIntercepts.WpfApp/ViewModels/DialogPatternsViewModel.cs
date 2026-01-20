// WpfApp/ViewModels/DialogPatternsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RadioIntercepts.WpfApp.Services;
using RadioIntercepts.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RadioIntercepts.WpfApp.ViewModels
{
    public partial class DialogPatternsViewModel : ObservableObject
    {
        private readonly IDialogPatternAnalyzer _analyzer;

        [ObservableProperty]
        private ObservableCollection<PhrasePattern> _commonPhrases = new();

        [ObservableProperty]
        private ObservableCollection<DialogSequence> _dialogSequences = new();

        [ObservableProperty]
        private ObservableCollection<RoleAnalysisResult> _roleAnalysis = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedAnalysisType = "Phrases";

        [ObservableProperty]
        private string _filterText = string.Empty;

        [ObservableProperty]
        private MessageType? _selectedMessageType;

        [ObservableProperty]
        private int _minFrequency = 5;

        [ObservableProperty]
        private int _sequenceLength = 3;

        [ObservableProperty]
        private ObservableCollection<MessageType> _messageTypes = new();

        [ObservableProperty]
        private RoleAnalysisResult? _selectedRoleAnalysis;

        [ObservableProperty]
        private ObservableCollection<StyleMetric> _styleMetrics = new();

        public DialogPatternsViewModel(IDialogPatternAnalyzer analyzer)
        {
            _analyzer = analyzer;
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
            await AnalyzeCommonPhrasesAsync();
        }

        [RelayCommand]
        private async Task AnalyzeCommonPhrasesAsync()
        {
            IsLoading = true;
            try
            {
                var phrases = await _analyzer.FindCommonPhrasesAsync(MinFrequency);
                CommonPhrases.Clear();

                foreach (var phrase in phrases)
                {
                    if (SelectedMessageType.HasValue && phrase.MessageType != SelectedMessageType.Value)
                        continue;

                    if (!string.IsNullOrWhiteSpace(FilterText) &&
                        !phrase.Phrase.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                        continue;

                    CommonPhrases.Add(phrase);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка анализа фраз: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AnalyzeDialogSequencesAsync()
        {
            IsLoading = true;
            try
            {
                var sequences = await _analyzer.AnalyzeDialogSequencesAsync(SequenceLength);
                DialogSequences.Clear();

                foreach (var sequence in sequences)
                {
                    if (!string.IsNullOrWhiteSpace(FilterText) &&
                        !sequence.Callsigns.Any(c => c.Contains(FilterText, StringComparison.OrdinalIgnoreCase)) &&
                        !sequence.Pattern.Any(p => p.ToString().Contains(FilterText, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    DialogSequences.Add(sequence);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка анализа последовательностей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AnalyzeRolesAsync()
        {
            IsLoading = true;
            try
            {
                var roles = await _analyzer.AnalyzeRolesAsync();
                RoleAnalysis.Clear();

                foreach (var role in roles)
                {
                    if (!string.IsNullOrWhiteSpace(FilterText) &&
                        !role.Callsign.Contains(FilterText, StringComparison.OrdinalIgnoreCase) &&
                        !role.Role.ToString().Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                        continue;

                    RoleAnalysis.Add(role);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка анализа ролей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AnalyzeStyleAsync()
        {
            if (SelectedRoleAnalysis == null)
            {
                MessageBox.Show("Выберите позывной для анализа стиля",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                var metrics = await _analyzer.CalculateStyleMetricsAsync(SelectedRoleAnalysis.Callsign);
                StyleMetrics.Clear();

                foreach (var metric in metrics.OrderBy(m => m.Key))
                {
                    StyleMetrics.Add(new StyleMetric
                    {
                        Name = TranslateMetricName(metric.Key),
                        Value = metric.Value,
                        FormattedValue = FormatMetricValue(metric.Key, metric.Value)
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка анализа стиля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ClassifyAllMessagesAsync()
        {
            IsLoading = true;
            try
            {
                var classifications = await _analyzer.ClassifyMessagesAsync();

                // Статистика по типам
                var typeStats = classifications
                    .GroupBy(c => c.Value)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Count = g.Count(),
                        Percentage = (double)g.Count() / classifications.Count * 100
                    })
                    .OrderByDescending(t => t.Count)
                    .ToList();

                string statsText = "Классификация сообщений:\n";
                foreach (var stat in typeStats)
                {
                    statsText += $"{stat.Type}: {stat.Count} ({stat.Percentage:F1}%)\n";
                }

                MessageBox.Show(statsText, "Результаты классификации",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка классификации: {ex.Message}",
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
            SelectedMessageType = null;
            MinFrequency = 5;
            SequenceLength = 3;

            // Обновляем анализ в зависимости от выбранного типа
            if (SelectedAnalysisType == "Phrases")
                _ = AnalyzeCommonPhrasesAsync();
            else if (SelectedAnalysisType == "Sequences")
                _ = AnalyzeDialogSequencesAsync();
            else if (SelectedAnalysisType == "Roles")
                _ = AnalyzeRolesAsync();
        }

        [RelayCommand]
        private async Task ExportAnalysisAsync(string format = "csv")
        {
            try
            {
                string content = SelectedAnalysisType switch
                {
                    "Phrases" => ExportPhrasesToCsv(),
                    "Sequences" => ExportSequencesToCsv(),
                    "Roles" => ExportRolesToCsv(),
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
                    FileName = $"dialog_analysis_{SelectedAnalysisType}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
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

        private string ExportPhrasesToCsv()
        {
            var lines = new List<string> { "Фраза,Тип,Частота,Уверенность,Позывные" };
            lines.AddRange(CommonPhrases.Select(p =>
                $"\"{p.Phrase}\",{p.MessageType},{p.Frequency},{p.Confidence:F2},\"{string.Join("; ", p.AssociatedCallsigns)}\""));
            return string.Join(Environment.NewLine, lines);
        }

        private string ExportSequencesToCsv()
        {
            var lines = new List<string> { "Паттерн,Позывные,Частота,Средняя длительность,Успешность" };
            lines.AddRange(DialogSequences.Select(s =>
                $"\"{string.Join("->", s.Pattern)}\",\"{string.Join("; ", s.Callsigns)}\",{s.Frequency},{s.AverageDuration.TotalMinutes:F1},{s.SuccessRate:P1}\""));
            return string.Join(Environment.NewLine, lines);
        }

        private string ExportRolesToCsv()
        {
            var lines = new List<string> { "Позывной,Роль,Уверенность,Признаки" };
            lines.AddRange(RoleAnalysis.Select(r =>
            {
                var features = string.Join("; ", r.RoleFeatures.Select(rf => $"{rf.Key}:{rf.Value:F2}"));
                return $"{r.Callsign},{r.Role},{r.RoleConfidence:F2},\"{features}\"";
            }));
            return string.Join(Environment.NewLine, lines);
        }

        partial void OnSelectedAnalysisTypeChanged(string value)
        {
            switch (value)
            {
                case "Phrases":
                    _ = AnalyzeCommonPhrasesAsync();
                    break;
                case "Sequences":
                    _ = AnalyzeDialogSequencesAsync();
                    break;
                case "Roles":
                    _ = AnalyzeRolesAsync();
                    break;
            }
        }

        partial void OnFilterTextChanged(string value)
        {
            RefreshFilteredData();
        }

        partial void OnSelectedMessageTypeChanged(MessageType? value)
        {
            RefreshFilteredData();
        }

        partial void OnMinFrequencyChanged(int value)
        {
            if (SelectedAnalysisType == "Phrases")
                _ = AnalyzeCommonPhrasesAsync();
        }

        partial void OnSequenceLengthChanged(int value)
        {
            if (SelectedAnalysisType == "Sequences")
                _ = AnalyzeDialogSequencesAsync();
        }

        private void RefreshFilteredData()
        {
            if (SelectedAnalysisType == "Phrases")
                ApplyPhraseFilters();
            else if (SelectedAnalysisType == "Sequences")
                ApplySequenceFilters();
            else if (SelectedAnalysisType == "Roles")
                ApplyRoleFilters();
        }

        private void ApplyPhraseFilters()
        {
            // Фильтрация уже происходит в AnalyzeCommonPhrasesAsync
            _ = AnalyzeCommonPhrasesAsync();
        }

        private void ApplySequenceFilters()
        {
            _ = AnalyzeDialogSequencesAsync();
        }

        private void ApplyRoleFilters()
        {
            _ = AnalyzeRolesAsync();
        }

        private string TranslateMetricName(string name)
        {
            return name switch
            {
                "AvgMessageLength" => "Средняя длина сообщения",
                "QuestionRatio" => "Доля вопросов",
                "ExclamationRatio" => "Доля восклицаний",
                "DigitRatio" => "Доля числовых данных",
                "VocabularySize" => "Размер словарного запаса",
                "WordRepetition" => "Коэффициент повторения слов",
                _ => name
            };
        }

        private string FormatMetricValue(string name, double value)
        {
            return name switch
            {
                "AvgMessageLength" => $"{value:F0} символов",
                "QuestionRatio" or "ExclamationRatio" or "DigitRatio" or "WordRepetition" => $"{value:P1}",
                "VocabularySize" => $"{value:F0} слов",
                _ => value.ToString("F2")
            };
        }
    }

    public class StyleMetric
    {
        public string Name { get; set; } = null!;
        public double Value { get; set; }
        public string FormattedValue { get; set; } = null!;
    }
}