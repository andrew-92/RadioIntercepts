//// WpfApp/ViewModels/AlertSystemViewModel.cs
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using RadioIntercepts.Application.Services;
//using RadioIntercepts.Analysis.Interfaces.Services;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Windows;
//using RadioIntercepts.Core.Models.Alerts;

//namespace RadioIntercepts.WpfApp.ViewModels
//{
//    public partial class AlertSystemViewModel : ObservableObject
//    {
//        private readonly IAlertService _alertService;
//        private System.Threading.Timer _alertCheckTimer;

//        [ObservableProperty]
//        private ObservableCollection<Alert> _activeAlerts = new();

//        [ObservableProperty]
//        private ObservableCollection<Alert> _allAlerts = new();

//        [ObservableProperty]
//        private ObservableCollection<AlertRule> _alertRules = new();

//        [ObservableProperty]
//        private ObservableCollection<AlertStatistics> _alertStatistics = new();

//        [ObservableProperty]
//        private Alert? _selectedAlert;

//        [ObservableProperty]
//        private AlertRule? _selectedRule;

//        [ObservableProperty]
//        private AlertStatistics? _currentStats;

//        [ObservableProperty]
//        private bool _isLoading;

//        [ObservableProperty]
//        private bool _autoRefreshEnabled = true;

//        [ObservableProperty]
//        private int _autoRefreshInterval = 60; // секунды

//        [ObservableProperty]
//        private string _filterText = string.Empty;

//        [ObservableProperty]
//        private AlertSeverity? _filterSeverity;

//        [ObservableProperty]
//        private AlertStatus? _filterStatus;

//        [ObservableProperty]
//        private DateTime? _filterDateFrom;

//        [ObservableProperty]
//        private DateTime? _filterDateTo;

//        [ObservableProperty]
//        private int _unreadAlertCount = 0;

//        [ObservableProperty]
//        private bool _showOnlyUnread = false;

//        [ObservableProperty]
//        private bool _playAlertSound = true;

//        public AlertSystemViewModel(IAlertService alertService)
//        {
//            _alertService = alertService;
//            FilterDateTo = DateTime.Today;
//            FilterDateFrom = DateTime.Today.AddDays(-7);

//            _ = InitializeAsync();

//            // Запускаем таймер для автоматической проверки
//            StartAutoRefresh();
//        }

//        private async Task InitializeAsync()
//        {
//            await LoadAlertRulesAsync();
//            await LoadActiveAlertsAsync();
//            await LoadAlertStatisticsAsync();
//        }

//        [RelayCommand]
//        private async Task LoadAlertRulesAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                var rules = await _alertService.GetAlertRulesAsync();
//                AlertRules.Clear();
//                foreach (var rule in rules)
//                {
//                    AlertRules.Add(rule);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка загрузки правил: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task LoadActiveAlertsAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                var alerts = await _alertService.GetActiveAlertsAsync();
//                ActiveAlerts.Clear();
//                foreach (var alert in alerts)
//                {
//                    ActiveAlerts.Add(alert);
//                }
//                UpdateUnreadCount();
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка загрузки активных алертов: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task LoadAllAlertsAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                var alerts = await _alertService.GetAlertsAsync(
//                    FilterDateFrom,
//                    FilterDateTo,
//                    FilterSeverity,
//                    FilterStatus);

//                AllAlerts.Clear();
//                foreach (var alert in alerts)
//                {
//                    if (!string.IsNullOrWhiteSpace(FilterText) &&
//                        !alert.Title.Contains(FilterText, StringComparison.OrdinalIgnoreCase) &&
//                        !alert.Description.Contains(FilterText, StringComparison.OrdinalIgnoreCase) &&
//                        !alert.RelatedCallsigns.Any(c => c.Contains(FilterText, StringComparison.OrdinalIgnoreCase)))
//                        continue;

//                    if (ShowOnlyUnread && alert.Status != AlertStatus.Active)
//                        continue;

//                    AllAlerts.Add(alert);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка загрузки алертов: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task LoadAlertStatisticsAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                var stats = await _alertService.GetAlertStatisticsAsync(FilterDateFrom, FilterDateTo);
//                CurrentStats = stats;

//                // Обновляем историю статистики
//                AlertStatistics.Clear();

//                // Добавляем статистику за разные периоды
//                var periods = new[]
//                {
//                    (DateTime.Today.AddDays(-1), DateTime.Today, "Последние 24 часа"),
//                    (DateTime.Today.AddDays(-7), DateTime.Today, "Последняя неделя"),
//                    (DateTime.Today.AddDays(-30), DateTime.Today, "Последний месяц")
//                };

//                foreach (var (start, end, name) in periods)
//                {
//                    var periodStats = await _alertService.GetAlertStatisticsAsync(start, end);
//                    AlertStatistics.Add(periodStats);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task CheckAllRulesAsync()
//        {
//            IsLoading = true;
//            try
//            {
//                var newAlerts = await _alertService.CheckAllRulesAsync();

//                if (newAlerts.Any())
//                {
//                    MessageBox.Show($"Обнаружено {newAlerts.Count} новых алертов",
//                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

//                    await LoadActiveAlertsAsync();
//                    await LoadAllAlertsAsync();

//                    // Воспроизводим звук, если включено
//                    if (PlayAlertSound && newAlerts.Any(a => a.Severity >= AlertSeverity.High))
//                    {
//                        PlayAlertSound();
//                    }
//                }
//                else
//                {
//                    MessageBox.Show("Новых алертов не обнаружено",
//                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка проверки правил: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task CheckSelectedRuleAsync()
//        {
//            if (SelectedRule == null)
//            {
//                MessageBox.Show("Выберите правило для проверки",
//                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            IsLoading = true;
//            try
//            {
//                var newAlerts = await _alertService.CheckRuleAsync(SelectedRule.Id);

//                if (newAlerts.Any())
//                {
//                    MessageBox.Show($"Правило '{SelectedRule.Name}' обнаружило {newAlerts.Count} алертов",
//                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

//                    await LoadActiveAlertsAsync();
//                    await LoadAllAlertsAsync();
//                }
//                else
//                {
//                    MessageBox.Show($"Правило '{SelectedRule.Name}' не обнаружило алертов",
//                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка проверки правила: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        [RelayCommand]
//        private async Task AcknowledgeAlertAsync()
//        {
//            if (SelectedAlert == null)
//            {
//                MessageBox.Show("Выберите алерт для подтверждения",
//                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            try
//            {
//                await _alertService.AcknowledgeAlertAsync(SelectedAlert.Id, "Operator");
//                await LoadActiveAlertsAsync();
//                await LoadAllAlertsAsync();

//                MessageBox.Show("Алерт подтвержден", "Успех",
//                    MessageBoxButton.OK, MessageBoxImage.Information);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка подтверждения алерта: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        [RelayCommand]
//        private async Task ResolveAlertAsync()
//        {
//            if (SelectedAlert == null)
//            {
//                MessageBox.Show("Выберите алерт для решения",
//                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            var dialog = new System.Windows.Controls.TextInputDialog(
//                "Заметки по решению:",
//                "Решение алерта",
//                "");

//            if (dialog.ShowDialog() == true)
//            {
//                try
//                {
//                    await _alertService.ResolveAlertAsync(SelectedAlert.Id, "Operator", dialog.ResponseText);
//                    await LoadActiveAlertsAsync();
//                    await LoadAllAlertsAsync();

//                    MessageBox.Show("Алерт решен", "Успех",
//                        MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show($"Ошибка решения алерта: {ex.Message}",
//                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//        }

//        [RelayCommand]
//        private async Task MarkAsFalseAlarmAsync()
//        {
//            if (SelectedAlert == null)
//            {
//                MessageBox.Show("Выберите алерт для отметки как ложное срабатывание",
//                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            var dialog = new System.Windows.Controls.TextInputDialog(
//                "Причина ложного срабатывания:",
//                "Ложное срабатывание",
//                "");

//            if (dialog.ShowDialog() == true)
//            {
//                try
//                {
//                    await _alertService.MarkAsFalseAlarmAsync(SelectedAlert.Id, "Operator", dialog.ResponseText);
//                    await LoadActiveAlertsAsync();
//                    await LoadAllAlertsAsync();

//                    MessageBox.Show("Алерт отмечен как ложное срабатывание", "Успех",
//                        MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show($"Ошибка: {ex.Message}",
//                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//        }

//        [RelayCommand]
//        private async Task DeleteAlertAsync()
//        {
//            if (SelectedAlert == null)
//            {
//                MessageBox.Show("Выберите алерт для удаления",
//                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            var result = MessageBox.Show(
//                $"Вы уверены, что хотите удалить алерт '{SelectedAlert.Title}'?",
//                "Подтверждение удаления",
//                MessageBoxButton.YesNo,
//                MessageBoxImage.Warning);

//            if (result == MessageBoxResult.Yes)
//            {
//                try
//                {
//                    await _alertService.DeleteAlertAsync(SelectedAlert.Id);
//                    await LoadActiveAlertsAsync();
//                    await LoadAllAlertsAsync();

//                    MessageBox.Show("Алерт удален", "Успех",
//                        MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show($"Ошибка удаления алерта: {ex.Message}",
//                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//        }

//        [RelayCommand]
//        private async Task CreateNewRuleAsync()
//        {
//            var dialog = new AlertRuleDialog();
//            if (dialog.ShowDialog() == true && dialog.Rule != null)
//            {
//                try
//                {
//                    var newRule = await _alertService.CreateAlertRuleAsync(dialog.Rule);
//                    await LoadAlertRulesAsync();

//                    MessageBox.Show($"Правило '{newRule.Name}' создано", "Успех",
//                        MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show($"Ошибка создания правила: {ex.Message}",
//                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//        }

//        [RelayCommand]
//        private async Task EditRuleAsync()
//        {
//            if (SelectedRule == null)
//            {
//                MessageBox.Show("Выберите правило для редактирования",
//                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            var dialog = new AlertRuleDialog(SelectedRule);
//            if (dialog.ShowDialog() == true && dialog.Rule != null)
//            {
//                try
//                {
//                    var updatedRule = await _alertService.UpdateAlertRuleAsync(dialog.Rule);
//                    await LoadAlertRulesAsync();

//                    MessageBox.Show($"Правило '{updatedRule.Name}' обновлено", "Успех",
//                        MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show($"Ошибка обновления правила: {ex.Message}",
//                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//        }

//        [RelayCommand]
//        private async Task ToggleRuleAsync()
//        {
//            if (SelectedRule == null)
//            {
//                MessageBox.Show("Выберите правило",
//                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            try
//            {
//                var newState = !SelectedRule.IsEnabled;
//                await _alertService.ToggleRuleAsync(SelectedRule.Id, newState);
//                await LoadAlertRulesAsync();

//                var stateText = newState ? "включено" : "выключено";
//                MessageBox.Show($"Правило '{SelectedRule.Name}' {stateText}", "Успех",
//                    MessageBoxButton.OK, MessageBoxImage.Information);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка изменения состояния правила: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        [RelayCommand]
//        private async Task DeleteRuleAsync()
//        {
//            if (SelectedRule == null)
//            {
//                MessageBox.Show("Выберите правило для удаления",
//                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            var result = MessageBox.Show(
//                $"Вы уверены, что хотите удалить правило '{SelectedRule.Name}'?",
//                "Подтверждение удаления",
//                MessageBoxButton.YesNo,
//                MessageBoxImage.Warning);

//            if (result == MessageBoxResult.Yes)
//            {
//                try
//                {
//                    await _alertService.DeleteAlertRuleAsync(SelectedRule.Id);
//                    await LoadAlertRulesAsync();

//                    MessageBox.Show("Правило удалено", "Успех",
//                        MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show($"Ошибка удаления правила: {ex.Message}",
//                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//        }

//        [RelayCommand]
//        private void MarkAllAsRead()
//        {
//            // Обновляем локальный флаг "прочитано"
//            // В реальном приложении это может быть отдельное поле в БД
//            foreach (var alert in ActiveAlerts.ToList())
//            {
//                // Можно добавить логику отметки как прочитанных
//            }

//            UpdateUnreadCount();
//        }

//        [RelayCommand]
//        private void ClearFilters()
//        {
//            FilterText = string.Empty;
//            FilterSeverity = null;
//            FilterStatus = null;
//            FilterDateFrom = DateTime.Today.AddDays(-7);
//            FilterDateTo = DateTime.Today;
//            ShowOnlyUnread = false;

//            _ = LoadAllAlertsAsync();
//        }

//        [RelayCommand]
//        private async Task ExportAlertsAsync(string format = "csv")
//        {
//            try
//            {
//                string content = ExportToCsv();

//                var dialog = new Microsoft.Win32.SaveFileDialog
//                {
//                    Filter = "CSV файлы (*.csv)|*.csv|JSON файлы (*.json)|*.json",
//                    FileName = $"alerts_export_{DateTime.Now:yyyyMMdd_HHmm}.csv"
//                };

//                if (dialog.ShowDialog() == true)
//                {
//                    System.IO.File.WriteAllText(dialog.FileName, content);
//                    MessageBox.Show($"Алерты экспортированы в {dialog.FileName}",
//                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка экспорта: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        private string ExportToCsv()
//        {
//            var lines = new List<string>
//            {
//                "ID,Время обнаружения,Статус,Важность,Правило,Заголовок,Описание,Позывные,Зоны,Частоты,Примечания"
//            };

//            foreach (var alert in AllAlerts)
//            {
//                lines.Add(
//                    $"{alert.Id}," +
//                    $"{alert.DetectedAt:yyyy-MM-dd HH:mm}," +
//                    $"{alert.Status}," +
//                    $"{alert.Severity}," +
//                    $"{alert.Rule?.Name ?? "Unknown"}," +
//                    $"\"{EscapeCsv(alert.Title)}\"," +
//                    $"\"{EscapeCsv(alert.Description)}\"," +
//                    $"\"{string.Join("; ", alert.RelatedCallsigns)}\"," +
//                    $"\"{string.Join("; ", alert.RelatedAreas)}\"," +
//                    $"\"{string.Join("; ", alert.RelatedFrequencies)}\"," +
//                    $"\"{EscapeCsv(alert.ResolutionNotes ?? "")}\""
//                );
//            }

//            return string.Join(Environment.NewLine, lines);
//        }

//        private void StartAutoRefresh()
//        {
//            _alertCheckTimer = new System.Threading.Timer(
//                async _ => await AutoRefreshCallback(),
//                null,
//                TimeSpan.FromSeconds(AutoRefreshInterval),
//                TimeSpan.FromSeconds(AutoRefreshInterval));
//        }

//        private async Task AutoRefreshCallback()
//        {
//            if (!AutoRefreshEnabled)
//                return;

//            // Проверяем новые алерты только если приложение активно
//            if (System.Windows.Application.Current?.MainWindow?.IsActive == true)
//            {
//                try
//                {
//                    var newAlerts = await _alertService.CheckAllRulesAsync();
//                    if (newAlerts.Any())
//                    {
//                        // Обновляем UI в основном потоке
//                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
//                        {
//                            await LoadActiveAlertsAsync();
//                            await LoadAllAlertsAsync();

//                            // Показываем уведомление
//                            if (newAlerts.Any(a => a.Severity >= AlertSeverity.Medium))
//                            {
//                                ShowAlertNotification(newAlerts);
//                            }
//                        });
//                    }
//                }
//                catch
//                {
//                    // Игнорируем ошибки при автообновлении
//                }
//            }
//        }

//        private void ShowAlertNotification(List<Alert> newAlerts)
//        {
//            var criticalAlerts = newAlerts.Where(a => a.Severity >= AlertSeverity.High).ToList();
//            var otherAlerts = newAlerts.Where(a => a.Severity < AlertSeverity.High).ToList();

//            string message;
//            if (criticalAlerts.Any())
//            {
//                message = $"Обнаружено {criticalAlerts.Count} критических алертов:\n";
//                message += string.Join("\n", criticalAlerts.Take(3).Select(a => $"• {a.Title}"));
//            }
//            else if (otherAlerts.Any())
//            {
//                message = $"Обнаружено {otherAlerts.Count} новых алертов";
//            }
//            else
//            {
//                return;
//            }

//            // Показываем всплывающее уведомление
//            var notification = new System.Windows.Forms.NotifyIcon
//            {
//                Icon = System.Drawing.SystemIcons.Information,
//                Visible = true,
//                BalloonTipTitle = "Обнаружены новые алерты",
//                BalloonTipText = message,
//                BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info
//            };

//            notification.ShowBalloonTip(5000);

//            // Воспроизводим звук
//            if (PlayAlertSound)
//            {
//                PlayAlertSound();
//            }
//        }

//        private void PlayAlertSound()
//        {
//            // Воспроизведение системного звука
//            System.Media.SystemSounds.Exclamation.Play();
//        }

//        private void UpdateUnreadCount()
//        {
//            UnreadAlertCount = ActiveAlerts.Count;
//        }

//        private string EscapeCsv(string text)
//        {
//            if (string.IsNullOrEmpty(text))
//                return "";

//            // Экранируем кавычки
//            return text.Replace("\"", "\"\"");
//        }

//        partial void OnAutoRefreshEnabledChanged(bool value)
//        {
//            if (value)
//            {
//                StartAutoRefresh();
//            }
//            else
//            {
//                _alertCheckTimer?.Dispose();
//            }
//        }

//        partial void OnAutoRefreshIntervalChanged(int value)
//        {
//            if (AutoRefreshEnabled)
//            {
//                _alertCheckTimer?.Dispose();
//                StartAutoRefresh();
//            }
//        }

//        partial void OnFilterTextChanged(string value)
//        {
//            _ = LoadAllAlertsAsync();
//        }

//        partial void OnFilterSeverityChanged(AlertSeverity? value)
//        {
//            _ = LoadAllAlertsAsync();
//        }

//        partial void OnFilterStatusChanged(AlertStatus? value)
//        {
//            _ = LoadAllAlertsAsync();
//        }

//        partial void OnFilterDateFromChanged(DateTime? value)
//        {
//            _ = LoadAllAlertsAsync();
//            _ = LoadAlertStatisticsAsync();
//        }

//        partial void OnFilterDateToChanged(DateTime? value)
//        {
//            _ = LoadAllAlertsAsync();
//            _ = LoadAlertStatisticsAsync();
//        }

//        partial void OnShowOnlyUnreadChanged(bool value)
//        {
//            _ = LoadAllAlertsAsync();
//        }
//    }

//    // Диалог для создания/редактирования правил (нужно реализовать в UI)
//    public class AlertRuleDialog : System.Windows.Window
//    {
//        public AlertRule? Rule { get; private set; }

//        public AlertRuleDialog(AlertRule? existingRule = null)
//        {
//            // Реализация диалогового окна для правил
//            // Это упрощенный вариант - в реальном приложении нужно создать полноценное окно
//        }
//    }
//}