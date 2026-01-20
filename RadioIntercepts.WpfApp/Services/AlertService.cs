// Application/Services/AlertService.cs
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.WpfApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RadioIntercepts.WpfApp.Services
{
    public interface IAlertService
    {
        // Управление правилами
        Task<List<AlertRule>> GetAlertRulesAsync(bool onlyEnabled = true);
        Task<AlertRule> GetAlertRuleAsync(int id);
        Task<AlertRule> CreateAlertRuleAsync(AlertRule rule);
        Task<AlertRule> UpdateAlertRuleAsync(AlertRule rule);
        Task DeleteAlertRuleAsync(int id);
        Task ToggleRuleAsync(int id, bool isEnabled);

        // Проверка правил
        Task<List<Alert>> CheckAllRulesAsync();
        Task<List<Alert>> CheckRuleAsync(int ruleId);
        Task<List<Alert>> CheckRulesAsync(IEnumerable<int> ruleIds);

        // Управление алертами
        Task<List<Alert>> GetActiveAlertsAsync(DateTime? since = null);
        Task<List<Alert>> GetAlertsAsync(DateTime? from = null, DateTime? to = null,
            AlertSeverity? severity = null, AlertStatus? status = null);
        Task<Alert> GetAlertAsync(long id);
        Task<Alert> AcknowledgeAlertAsync(long alertId, string user);
        Task<Alert> ResolveAlertAsync(long alertId, string user, string notes = null);
        Task<Alert> MarkAsFalseAlarmAsync(long alertId, string user, string notes = null);
        Task DeleteAlertAsync(long id);

        // Статистика
        Task<AlertStatistics> GetAlertStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<AlertHistoryEntry>> GetAlertHistoryAsync(long alertId);

        // Уведомления
        Task SendAlertNotificationsAsync(Alert alert);
        Task<List<AlertNotification>> GetPendingNotificationsAsync();
    }

    public class AlertService : IAlertService
    {
        private readonly AlertDbContext _alertContext;
        private readonly AppDbContext _dataContext;
        private readonly IDialogPatternAnalyzer _dialogAnalyzer;
        private readonly ISemanticSearchService _searchService;

        // Встроенные правила
        private readonly List<AlertRule> _builtInRules = new()
        {
            new AlertRule
            {
                Id = 1,
                Name = "Новый позывной",
                Description = "Обнаружение позывного, который не встречался ранее",
                Severity = AlertSeverity.Low,
                ConditionExpression = "NewCallsign",
                CheckInterval = TimeSpan.FromHours(1)
            },
            new AlertRule
            {
                Id = 2,
                Name = "Необычная активность",
                Description = "Резкое увеличение количества сообщений в зоне",
                Severity = AlertSeverity.Medium,
                ConditionExpression = "UnusualActivity",
                CheckInterval = TimeSpan.FromMinutes(30)
            },
            new AlertRule
            {
                Id = 3,
                Name = "Обрыв связи",
                Description = "Позывной не проявляет активности дольше обычного",
                Severity = AlertSeverity.High,
                ConditionExpression = "CommunicationBreak",
                CheckInterval = TimeSpan.FromHours(6)
            },
            new AlertRule
            {
                Id = 4,
                Name = "Обсуждение ключевых тем",
                Description = "Обсуждение критических тем (атака, ранения, техника)",
                Severity = AlertSeverity.Critical,
                ConditionExpression = "KeyTopics",
                CheckInterval = TimeSpan.FromMinutes(5)
            },
            new AlertRule
            {
                Id = 5,
                Name = "Нестандартная частота",
                Description = "Использование частоты, которая редко используется",
                Severity = AlertSeverity.Medium,
                ConditionExpression = "UnusualFrequency",
                CheckInterval = TimeSpan.FromHours(2)
            },
            new AlertRule
            {
                Id = 6,
                Name = "Смена зоны активности",
                Description = "Позывной начал активно работать в новой зоне",
                Severity = AlertSeverity.Low,
                ConditionExpression = "AreaChange",
                CheckInterval = TimeSpan.FromHours(12)
            },
            new AlertRule
            {
                Id = 7,
                Name = "Критические рапорты",
                Description = "Сообщения о ранениях, потерях или разрушениях",
                Severity = AlertSeverity.Critical,
                ConditionExpression = "CriticalReports",
                CheckInterval = TimeSpan.FromMinutes(10)
            },
            new AlertRule
            {
                Id = 8,
                Name = "Координация групп",
                Description = "Обнаружение координации между несколькими группами",
                Severity = AlertSeverity.High,
                ConditionExpression = "GroupCoordination",
                CheckInterval = TimeSpan.FromHours(1)
            },
            new AlertRule
            {
                Id = 9,
                Name = "Подозрительное молчание",
                Description = "Отсутствие активности в обычно активное время",
                Severity = AlertSeverity.Medium,
                ConditionExpression = "SuspiciousSilence",
                CheckInterval = TimeSpan.FromHours(3)
            },
            new AlertRule
            {
                Id = 10,
                Name = "Пиковая активность",
                Description = "Необычно высокая активность в нерабочее время",
                Severity = AlertSeverity.High,
                ConditionExpression = "PeakActivity",
                CheckInterval = TimeSpan.FromHours(1)
            }
        };

        // Ключевые слова для разных типов алертов
        private static readonly Dictionary<string, string[]> _keyTopics = new()
        {
            ["attack"] = new[] { "атака", "наступление", "штурм", "обстрел", "огонь", "стрельба" },
            ["casualty"] = new[] { "ранен", "убит", "потери", "кровь", "медик", "скорая" },
            ["equipment"] = new[] { "танк", "бмп", "бтр", "артиллерия", "техника", "вооружение" },
            ["movement"] = new[] { "выдвижение", "отход", "отступление", "передислокация" },
            ["critical"] = new[] { "срочно", "критично", "авария", "катастрофа", "чрезвычайно" }
        };

        public AlertService(AlertDbContext alertContext, AppDbContext dataContext,
            IDialogPatternAnalyzer dialogAnalyzer, ISemanticSearchService searchService)
        {
            _alertContext = alertContext;
            _dataContext = dataContext;
            _dialogAnalyzer = dialogAnalyzer;
            _searchService = searchService;
        }

        public async Task<List<AlertRule>> GetAlertRulesAsync(bool onlyEnabled = true)
        {
            var query = _alertContext.AlertRules.AsQueryable();

            if (onlyEnabled)
                query = query.Where(r => r.IsEnabled);

            var rules = await query
                .OrderBy(r => r.Severity)
                .ThenBy(r => r.Name)
                .ToListAsync();

            // Добавляем встроенные правила, если их нет в БД
            if (!rules.Any(r => _builtInRules.Select(br => br.Id).Contains(r.Id)))
            {
                foreach (var builtInRule in _builtInRules)
                {
                    builtInRule.CreatedAt = DateTime.UtcNow;
                    builtInRule.UpdatedAt = DateTime.UtcNow;
                    _alertContext.AlertRules.Add(builtInRule);
                }
                await _alertContext.SaveChangesAsync();
                rules = await query.ToListAsync();
            }

            return rules;
        }

        public async Task<AlertRule> GetAlertRuleAsync(int id)
        {
            var rule = await _alertContext.AlertRules.FindAsync(id);
            if (rule == null && _builtInRules.Any(r => r.Id == id))
            {
                rule = _builtInRules.First(r => r.Id == id);
            }
            return rule;
        }

        public async Task<AlertRule> CreateAlertRuleAsync(AlertRule rule)
        {
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;

            _alertContext.AlertRules.Add(rule);
            await _alertContext.SaveChangesAsync();

            return rule;
        }

        public async Task<AlertRule> UpdateAlertRuleAsync(AlertRule rule)
        {
            rule.UpdatedAt = DateTime.UtcNow;
            _alertContext.AlertRules.Update(rule);
            await _alertContext.SaveChangesAsync();

            return rule;
        }

        public async Task DeleteAlertRuleAsync(int id)
        {
            var rule = await _alertContext.AlertRules.FindAsync(id);
            if (rule != null)
            {
                _alertContext.AlertRules.Remove(rule);
                await _alertContext.SaveChangesAsync();
            }
        }

        public async Task ToggleRuleAsync(int id, bool isEnabled)
        {
            var rule = await _alertContext.AlertRules.FindAsync(id);
            if (rule != null)
            {
                rule.IsEnabled = isEnabled;
                rule.UpdatedAt = DateTime.UtcNow;
                await _alertContext.SaveChangesAsync();
            }
        }

        public async Task<List<Alert>> CheckAllRulesAsync()
        {
            var rules = await GetAlertRulesAsync();
            var allAlerts = new List<Alert>();

            foreach (var rule in rules.Where(r => r.IsEnabled))
            {
                // Проверяем, нужно ли проверять это правило сейчас
                if (rule.LastChecked.HasValue &&
                    DateTime.UtcNow - rule.LastChecked.Value < rule.CheckInterval)
                {
                    continue;
                }

                var alerts = await CheckRuleAsync(rule.Id);
                allAlerts.AddRange(alerts);

                // Обновляем время последней проверки
                rule.LastChecked = DateTime.UtcNow;
                _alertContext.AlertRules.Update(rule);
            }

            await _alertContext.SaveChangesAsync();

            // Отправляем уведомления для новых алертов
            foreach (var alert in allAlerts.Where(a => a.Id == 0))
            {
                await SendAlertNotificationsAsync(alert);
            }

            return allAlerts;
        }

        public async Task<List<Alert>> CheckRuleAsync(int ruleId)
        {
            var rule = await GetAlertRuleAsync(ruleId);
            if (rule == null || !rule.IsEnabled)
                return new List<Alert>();

            List<Alert> alerts = rule.ConditionExpression switch
            {
                "NewCallsign" => await CheckNewCallsignsAsync(rule),
                "UnusualActivity" => await CheckUnusualActivityAsync(rule),
                "CommunicationBreak" => await CheckCommunicationBreaksAsync(rule),
                "KeyTopics" => await CheckKeyTopicsAsync(rule),
                "UnusualFrequency" => await CheckUnusualFrequenciesAsync(rule),
                "AreaChange" => await CheckAreaChangesAsync(rule),
                "CriticalReports" => await CheckCriticalReportsAsync(rule),
                "GroupCoordination" => await CheckGroupCoordinationAsync(rule),
                "SuspiciousSilence" => await CheckSuspiciousSilenceAsync(rule),
                "PeakActivity" => await CheckPeakActivityAsync(rule),
                _ => await CheckCustomRuleAsync(rule)
            };

            // Сохраняем алерты в БД
            foreach (var alert in alerts)
            {
                alert.RuleId = rule.Id;
                alert.Severity = rule.Severity;
                alert.DetectedAt = DateTime.UtcNow;

                _alertContext.Alerts.Add(alert);
            }

            if (alerts.Any())
                await _alertContext.SaveChangesAsync();

            return alerts;
        }

        public async Task<List<Alert>> CheckRulesAsync(IEnumerable<int> ruleIds)
        {
            var allAlerts = new List<Alert>();

            foreach (var ruleId in ruleIds)
            {
                var alerts = await CheckRuleAsync(ruleId);
                allAlerts.AddRange(alerts);
            }

            return allAlerts;
        }

        public async Task<List<Alert>> GetActiveAlertsAsync(DateTime? since = null)
        {
            var query = _alertContext.Alerts
                .Include(a => a.Rule)
                .Where(a => a.Status == AlertStatus.Active);

            if (since.HasValue)
                query = query.Where(a => a.DetectedAt >= since.Value);

            return await query
                .OrderByDescending(a => a.Severity)
                .ThenByDescending(a => a.DetectedAt)
                .ToListAsync();
        }

        public async Task<List<Alert>> GetAlertsAsync(DateTime? from = null, DateTime? to = null,
            AlertSeverity? severity = null, AlertStatus? status = null)
        {
            var query = _alertContext.Alerts
                .Include(a => a.Rule)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(a => a.DetectedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(a => a.DetectedAt <= to.Value);
            if (severity.HasValue)
                query = query.Where(a => a.Severity == severity.Value);
            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            return await query
                .OrderByDescending(a => a.DetectedAt)
                .ToListAsync();
        }

        public async Task<Alert> GetAlertAsync(long id)
        {
            return await _alertContext.Alerts
                .Include(a => a.Rule)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Alert> AcknowledgeAlertAsync(long alertId, string user)
        {
            var alert = await GetAlertAsync(alertId);
            if (alert == null)
                return null;

            alert.Status = AlertStatus.Acknowledged;
            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.AcknowledgedBy = user;

            _alertContext.Alerts.Update(alert);
            await _alertContext.SaveChangesAsync();

            return alert;
        }

        public async Task<Alert> ResolveAlertAsync(long alertId, string user, string notes = null)
        {
            var alert = await GetAlertAsync(alertId);
            if (alert == null)
                return null;

            alert.Status = AlertStatus.Resolved;
            alert.ResolvedAt = DateTime.UtcNow;
            alert.AcknowledgedBy = user;
            alert.ResolutionNotes = notes;

            _alertContext.Alerts.Update(alert);
            await _alertContext.SaveChangesAsync();

            return alert;
        }

        public async Task<Alert> MarkAsFalseAlarmAsync(long alertId, string user, string notes = null)
        {
            var alert = await GetAlertAsync(alertId);
            if (alert == null)
                return null;

            alert.Status = AlertStatus.FalseAlarm;
            alert.ResolvedAt = DateTime.UtcNow;
            alert.AcknowledgedBy = user;
            alert.ResolutionNotes = notes;

            _alertContext.Alerts.Update(alert);
            await _alertContext.SaveChangesAsync();

            return alert;
        }

        public async Task DeleteAlertAsync(long id)
        {
            var alert = await GetAlertAsync(id);
            if (alert != null)
            {
                _alertContext.Alerts.Remove(alert);
                await _alertContext.SaveChangesAsync();
            }
        }

        public async Task<AlertStatistics> GetAlertStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var stats = new AlertStatistics
            {
                PeriodStart = startDate ?? DateTime.UtcNow.AddDays(-30),
                PeriodEnd = endDate ?? DateTime.UtcNow
            };

            var query = _alertContext.Alerts
                .Where(a => a.DetectedAt >= stats.PeriodStart && a.DetectedAt <= stats.PeriodEnd);

            stats.TotalAlerts = await query.CountAsync();
            stats.ActiveAlerts = await query.CountAsync(a => a.Status == AlertStatus.Active);

            // По уровню важности
            var severityGroups = await query
                .GroupBy(a => a.Severity)
                .Select(g => new { Severity = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var group in severityGroups)
            {
                stats.AlertsBySeverity[group.Severity] = group.Count;
            }

            // По статусу
            var statusGroups = await query
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var group in statusGroups)
            {
                stats.AlertsByStatus[group.Status] = group.Count;
            }

            // По правилам
            var ruleGroups = await query
                .Include(a => a.Rule)
                .GroupBy(a => a.Rule.Name)
                .Select(g => new { Rule = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var group in ruleGroups)
            {
                stats.AlertsByRule[group.Rule] = group.Count;
            }

            // Среднее время реакции и решения
            var resolvedAlerts = await query
                .Where(a => a.Status == AlertStatus.Resolved && a.AcknowledgedAt.HasValue)
                .ToListAsync();

            if (resolvedAlerts.Any())
            {
                var responseTimes = resolvedAlerts
                    .Select(a => (a.AcknowledgedAt.Value - a.DetectedAt).TotalSeconds)
                    .ToList();

                var resolutionTimes = resolvedAlerts
                    .Where(a => a.ResolvedAt.HasValue)
                    .Select(a => (a.ResolvedAt.Value - a.DetectedAt).TotalSeconds)
                    .ToList();

                stats.AverageResponseTime = TimeSpan.FromSeconds(responseTimes.Average());
                stats.AverageResolutionTime = TimeSpan.FromSeconds(resolutionTimes.Any() ? resolutionTimes.Average() : 0);
            }

            return stats;
        }

        public async Task<List<AlertHistoryEntry>> GetAlertHistoryAsync(long alertId)
        {
            var alert = await GetAlertAsync(alertId);
            if (alert == null)
                return new List<AlertHistoryEntry>();

            var history = new List<AlertHistoryEntry>
            {
                new AlertHistoryEntry
                {
                    Timestamp = alert.DetectedAt,
                    Action = "Detected",
                    User = "System",
                    Description = "Alert detected by rule",
                    Details = alert.Rule?.Name ?? "Unknown rule"
                }
            };

            if (alert.AcknowledgedAt.HasValue)
            {
                history.Add(new AlertHistoryEntry
                {
                    Timestamp = alert.AcknowledgedAt.Value,
                    Action = "Acknowledged",
                    User = alert.AcknowledgedBy ?? "Unknown",
                    Description = "Alert acknowledged by operator",
                    Details = alert.ResolutionNotes ?? ""
                });
            }

            if (alert.ResolvedAt.HasValue)
            {
                history.Add(new AlertHistoryEntry
                {
                    Timestamp = alert.ResolvedAt.Value,
                    Action = alert.Status == AlertStatus.FalseAlarm ? "Marked as false alarm" : "Resolved",
                    User = alert.AcknowledgedBy ?? "Unknown",
                    Description = "Alert resolved",
                    Details = alert.ResolutionNotes ?? ""
                });
            }

            return history.OrderByDescending(h => h.Timestamp).ToList();
        }

        public async Task SendAlertNotificationsAsync(Alert alert)
        {
            // Здесь должна быть реализация отправки уведомлений
            // через email, Telegram, веб-хуки и т.д.
            // В данном примере просто логируем

            Console.WriteLine($"Alert notification: {alert.Title} - {alert.Description}");

            // В реальном приложении здесь будет код для:
            // 1. Отправки email
            // 2. Отправки в Telegram
            // 3. Отправки веб-хука
            // 4. Воспроизведения звукового сигнала
            // 5. Показа всплывающего окна в UI
        }

        public async Task<List<AlertNotification>> GetPendingNotificationsAsync()
        {
            // Возвращает уведомления, которые еще не были отправлены
            // В данном примере возвращаем пустой список
            return new List<AlertNotification>();
        }

        // Методы проверки конкретных правил

        private async Task<List<Alert>> CheckNewCallsignsAsync(AlertRule rule)
        {
            var alerts = new List<Alert>();

            // Получаем сообщения за последние 24 часа
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var recentMessages = await _dataContext.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Where(m => m.DateTime >= last24Hours)
                .ToListAsync();

            // Получаем все позывные за последнюю неделю
            var lastWeek = DateTime.UtcNow.AddDays(-7);
            var existingCallsigns = await _dataContext.MessageCallsigns
                .Include(mc => mc.Message)
                .Where(mc => mc.Message.DateTime >= lastWeek && mc.Message.DateTime < last24Hours)
                .Select(mc => mc.Callsign.Name)
                .Distinct()
                .ToListAsync();

            // Находим новые позывные
            var newCallsigns = recentMessages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .Distinct()
                .Except(existingCallsigns)
                .ToList();

            foreach (var callsign in newCallsigns)
            {
                var relatedMessages = recentMessages
                    .Where(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign))
                    .ToList();

                alerts.Add(new Alert
                {
                    Title = $"Новый позывной: {callsign}",
                    Description = $"Обнаружен позывной, который не встречался ранее",
                    Details = $"Позывной {callsign} впервые обнаружен в системе. " +
                             $"Количество сообщений: {relatedMessages.Count}. " +
                             $"Первое появление: {relatedMessages.Min(m => m.DateTime):yyyy-MM-dd HH:mm}",
                    RelatedCallsigns = new List<string> { callsign },
                    RelatedAreas = relatedMessages.Select(m => m.Area.Name).Distinct().ToList(),
                    RelatedFrequencies = relatedMessages.Select(m => m.Frequency.Value).Distinct().ToList(),
                    RelatedMessageIds = relatedMessages.Select(m => m.Id).ToList()
                });
            }

            return alerts;
        }

        private async Task<List<Alert>> CheckUnusualActivityAsync(AlertRule rule)
        {
            var alerts = new List<Alert>();

            // Сравниваем активность за последний час со средней активностью
            var lastHour = DateTime.UtcNow.AddHours(-1);
            var last24Hours = DateTime.UtcNow.AddHours(-24);

            // Активность по зонам за последний час
            var recentActivity = await _dataContext.Messages
                .Include(m => m.Area)
                .Where(m => m.DateTime >= lastHour)
                .GroupBy(m => m.Area.Name)
                .Select(g => new { Area = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Area, g => g.Count);

            // Средняя активность по зонам за последние 24 часа
            var averageActivity = await _dataContext.Messages
                .Include(m => m.Area)
                .Where(m => m.DateTime >= last24Hours && m.DateTime < lastHour)
                .GroupBy(m => m.Area.Name)
                .Select(g => new { Area = g.Key, Avg = g.Count() / 24.0 }) // сообщений в час
                .ToDictionaryAsync(g => g.Area, g => g.Avg);

            foreach (var area in recentActivity.Keys)
            {
                var currentCount = recentActivity[area];
                var avgCount = averageActivity.GetValueOrDefault(area, 0);

                if (avgCount > 0 && currentCount > avgCount * 3) // В 3 раза выше среднего
                {
                    alerts.Add(new Alert
                    {
                        Title = $"Необычная активность в зоне {area}",
                        Description = $"Резкое увеличение количества сообщений",
                        Details = $"В зоне {area} за последний час зафиксировано {currentCount} сообщений, " +
                                 $"что в {currentCount / avgCount:F1} раза выше среднего показателя ({avgCount:F1} сообщений в час).",
                        RelatedAreas = new List<string> { area },
                        Severity = currentCount > avgCount * 5 ? AlertSeverity.High : AlertSeverity.Medium
                    });
                }
            }

            return alerts;
        }

        private async Task<List<Alert>> CheckCommunicationBreaksAsync(AlertRule rule)
        {
            var alerts = new List<Alert>();

            // Получаем всех позывных, которые были активны в последние 48 часов
            var last48Hours = DateTime.UtcNow.AddHours(-48);
            var activeCallsigns = await _dataContext.MessageCallsigns
                .Include(mc => mc.Message)
                .Where(mc => mc.Message.DateTime >= last48Hours)
                .Select(mc => new { mc.Callsign.Name, mc.Message.DateTime })
                .ToListAsync();

            // Группируем по позывным и находим последнее сообщение
            var callsignLastSeen = activeCallsigns
                .GroupBy(x => x.Name)
                .Select(g => new {
                    Callsign = g.Key,
                    LastSeen = g.Max(x => x.DateTime),
                    MessageCount = g.Count()
                })
                .Where(x => x.MessageCount >= 3) // Игнорируем редкие позывные
                .ToList();

            var now = DateTime.UtcNow;

            foreach (var callsign in callsignLastSeen)
            {
                var hoursSinceLastSeen = (now - callsign.LastSeen).TotalHours;

                // Если позывной обычно активен каждые 6 часов, но не активен более 12 часов
                if (hoursSinceLastSeen > 12)
                {
                    alerts.Add(new Alert
                    {
                        Title = $"Обрыв связи: {callsign.Callsign}",
                        Description = $"Позывной не проявляет активности более {hoursSinceLastSeen:F1} часов",
                        Details = $"Позывной {callsign.Callsign} последний раз был активен {callsign.LastSeen:yyyy-MM-dd HH:mm}. " +
                                 $"Всего сообщений за последние 48 часов: {callsign.MessageCount}.",
                        RelatedCallsigns = new List<string> { callsign.Callsign },
                        Severity = hoursSinceLastSeen > 24 ? AlertSeverity.Critical : AlertSeverity.High
                    });
                }
            }

            return alerts;
        }

        private async Task<List<Alert>> CheckKeyTopicsAsync(AlertRule rule)
        {
            var alerts = new List<Alert>();

            // Получаем сообщения за последний час
            var lastHour = DateTime.UtcNow.AddHours(-1);
            var recentMessages = await _dataContext.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .Where(m => m.DateTime >= lastHour)
                .ToListAsync();

            foreach (var message in recentMessages)
            {
                var messageLower = message.Dialog.ToLower();
                var detectedTopics = new List<string>();

                foreach (var topic in _keyTopics)
                {
                    if (topic.Value.Any(keyword => messageLower.Contains(keyword)))
                    {
                        detectedTopics.Add(topic.Key);
                    }
                }

                if (detectedTopics.Any())
                {
                    var topicNames = string.Join(", ", detectedTopics);
                    var callsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();

                    alerts.Add(new Alert
                    {
                        Title = $"Обсуждение ключевой темы: {topicNames}",
                        Description = $"Обнаружено сообщение по критической теме",
                        Details = $"Темы: {topicNames}\n" +
                                 $"Позывные: {string.Join(", ", callsigns)}\n" +
                                 $"Зона: {message.Area.Name}\n" +
                                 $"Время: {message.DateTime:HH:mm}\n" +
                                 $"Текст: {Truncate(message.Dialog, 200)}",
                        RelatedCallsigns = callsigns,
                        RelatedAreas = new List<string> { message.Area.Name },
                        RelatedMessageIds = new List<long> { message.Id },
                        Severity = detectedTopics.Contains("critical") || detectedTopics.Contains("casualty")
                            ? AlertSeverity.Critical
                            : AlertSeverity.High
                    });
                }
            }

            // Группируем по темам, чтобы избежать дублирования
            return alerts
                .GroupBy(a => a.Title)
                .Select(g => g.First())
                .ToList();
        }

        private async Task<List<Alert>> CheckUnusualFrequenciesAsync(AlertRule rule)
        {
            var alerts = new List<Alert>();

            // Получаем все частоты за последнюю неделю
            var lastWeek = DateTime.UtcNow.AddDays(-7);
            var frequencyUsage = await _dataContext.Messages
                .Include(m => m.Frequency)
                .Where(m => m.DateTime >= lastWeek)
                .GroupBy(m => m.Frequency.Value)
                .Select(g => new { Frequency = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalMessages = frequencyUsage.Sum(f => f.Count);
            if (totalMessages == 0)
                return alerts;

            // Находим редко используемые частоты (менее 1% от общего трафика)
            var rareFrequencies = frequencyUsage
                .Where(f => (double)f.Count / totalMessages < 0.01)
                .ToList();

            // Проверяем, использовались ли эти частоты в последний час
            var lastHour = DateTime.UtcNow.AddHours(-1);
            var recentRareUsage = await _dataContext.Messages
                .Include(m => m.Frequency)
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .Where(m => m.DateTime >= lastHour &&
                    rareFrequencies.Select(f => f.Frequency).Contains(m.Frequency.Value))
                .GroupBy(m => m.Frequency.Value)
                .Select(g => new {
                    Frequency = g.Key,
                    Messages = g.ToList(),
                    Count = g.Count()
                })
                .ToListAsync();

            foreach (var usage in recentRareUsage)
            {
                if (usage.Count > 0)
                {
                    var callsigns = usage.Messages
                        .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                        .Distinct()
                        .ToList();

                    var areas = usage.Messages
                        .Select(m => m.Area.Name)
                        .Distinct()
                        .ToList();

                    alerts.Add(new Alert
                    {
                        Title = $"Нестандартная частота: {usage.Frequency}",
                        Description = $"Использование редко применяемой частоты",
                        Details = $"Частота {usage.Frequency} использовалась {usage.Count} раз за последний час. " +
                                 $"За последнюю неделю эта частота составляла только " +
                                 $"{(rareFrequencies.First(f => f.Frequency == usage.Frequency).Count / (double)totalMessages * 100):F1}% от всего трафика.\n" +
                                 $"Позывные: {string.Join(", ", callsigns)}\n" +
                                 $"Зоны: {string.Join(", ", areas)}",
                        RelatedFrequencies = new List<string> { usage.Frequency },
                        RelatedCallsigns = callsigns,
                        RelatedAreas = areas,
                        RelatedMessageIds = usage.Messages.Select(m => m.Id).ToList()
                    });
                }
            }

            return alerts;
        }

        private async Task<List<Alert>> CheckAreaChangesAsync(AlertRule rule)
        {
            var alerts = new List<Alert>();

            // Находим позывные, которые сменили зону активности
            var last48Hours = DateTime.UtcNow.AddHours(-48);
            var last7Days = DateTime.UtcNow.AddDays(-7);

            // Получаем активность позывных по зонам за последние 7 дней
            var callsignActivity = await _dataContext.MessageCallsigns
                .Include(mc => mc.Message)
                    .ThenInclude(m => m.Area)
                .Include(mc => mc.Callsign)
                .Where(mc => mc.Message.DateTime >= last7Days)
                .Select(mc => new {
                    CallsignName = mc.Callsign.Name,
                    AreaName = mc.Message.Area.Name,
                    mc.Message.DateTime
                })
                .ToListAsync();

            // Группируем по позывным и находим основные зоны
            var callsignMainAreas = callsignActivity
                .GroupBy(x => x.CallsignName)
                .Select(g => new
                {
                    Callsign = g.Key,
                    TotalMessages = g.Count(),
                    MainArea = g.GroupBy(x => x.AreaName)
                               .OrderByDescending(ag => ag.Count())
                               .First()
                               .Key,
                    MainAreaPercentage = (double)g.GroupBy(x => x.AreaName)
                                                 .OrderByDescending(ag => ag.Count())
                                                 .First()
                                                 .Count() / g.Count() * 100
                })
                .Where(x => x.TotalMessages >= 5 && x.MainAreaPercentage > 70) // Уверенно определенная основная зона
                .ToList();

            // Проверяем активность за последние 48 часов
            foreach (var callsign in callsignMainAreas)
            {
                var recentActivity = callsignActivity
                    .Where(x => x.CallsignName == callsign.Callsign && x.DateTime >= last48Hours)
                    .ToList();

                if (recentActivity.Any())
                {
                    var recentMainArea = recentActivity
                        .GroupBy(x => x.AreaName)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;

                    if (recentMainArea != callsign.MainArea)
                    {
                        var newAreaMessages = recentActivity.Count(x => x.AreaName == recentMainArea);
                        var oldAreaMessages = recentActivity.Count(x => x.AreaName == callsign.MainArea);

                        alerts.Add(new Alert
                        {
                            Title = $"Смена зоны активности: {callsign.Callsign}",
                            Description = $"Позывной сменил основную зону активности",
                            Details = $"Позывной {callsign.Callsign} сменил основную зону активности с '{callsign.MainArea}' " +
                                     $"на '{recentMainArea}'.\n" +
                                     $"За последние 48 часов: {newAreaMessages} сообщений в новой зоне, " +
                                     $"{oldAreaMessages} сообщений в старой зоне.\n" +
                                     $"Ранее основная зона ({callsign.MainArea}) составляла {callsign.MainAreaPercentage:F1}% активности.",
                            RelatedCallsigns = new List<string> { callsign.Callsign },
                            RelatedAreas = new List<string> { callsign.MainArea, recentMainArea }
                        });
                    }
                }
            }

            return alerts;
        }

        private async Task<List<Alert>> CheckCriticalReportsAsync(AlertRule rule)
        {
            var alerts = new List<Alert>();

            // Получаем сообщения за последние 2 часа
            var last2Hours = DateTime.UtcNow.AddHours(-2);
            var recentMessages = await _dataContext.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .Where(m => m.DateTime >= last2Hours)
                .ToListAsync();

            // Критические ключевые слова
            var criticalKeywords = new[]
            {
                "ранен", "убит", "потери", "кровь", "медик", "скорая",
                "разрушен", "уничтожен", "поврежден", "авария", "катастрофа",
                "пленный", "захвачен", "сдался", "дезертир"
            };

            foreach (var message in recentMessages)
            {
                var messageLower = message.Dialog.ToLower();
                var foundKeywords = criticalKeywords.Where(k => messageLower.Contains(k)).ToList();

                if (foundKeywords.Any())
                {
                    var callsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
                    var keywordList = string.Join(", ", foundKeywords);

                    alerts.Add(new Alert
                    {
                        Title = $"Критический рапорт обнаружен",
                        Description = $"Сообщение содержит критическую информацию",
                        Details = $"Ключевые слова: {keywordList}\n" +
                                 $"Позывные: {string.Join(", ", callsigns)}\n" +
                                 $"Зона: {message.Area.Name}\n" +
                                 $"Время: {message.DateTime:HH:mm}\n" +
                                 $"Текст: {Truncate(message.Dialog, 300)}",
                        RelatedCallsigns = callsigns,
                        RelatedAreas = new List<string> { message.Area.Name },
                        RelatedMessageIds = new List<long> { message.Id },
                        Severity = AlertSeverity.Critical
                    });
                }
            }

            return alerts;
        }

        private async Task<List<Alert>> CheckGroupCoordinationAsync(AlertRule rule)
        {
            var alerts = new List<Alert>();

            // Ищем сообщения, в которых участвуют 3 и более позывных
            // и содержат слова координации
            var lastHour = DateTime.UtcNow.AddHours(-1);
            var coordinationKeywords = new[]
            {
                "координировать", "согласовывать", "совместно", "вместе", "параллельно",
                "синхронно", "одновременно", "скоординированно", "взаимодействие"
            };

            var messages = await _dataContext.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .Where(m => m.DateTime >= lastHour && m.MessageCallsigns.Count >= 3)
                .ToListAsync();

            foreach (var message in messages)
            {
                var messageLower = message.Dialog.ToLower();
                if (coordinationKeywords.Any(k => messageLower.Contains(k)))
                {
                    var callsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
                    var callsignCount = callsigns.Count;

                    alerts.Add(new Alert
                    {
                        Title = $"Координация {callsignCount} групп",
                        Description = $"Обнаружена координация между несколькими позывными",
                        Details = $"Обнаружено сообщение с координацией {callsignCount} позывных: {string.Join(", ", callsigns)}\n" +
                                 $"Зона: {message.Area.Name}\n" +
                                 $"Время: {message.DateTime:HH:mm}\n" +
                                 $"Текст: {Truncate(message.Dialog, 200)}",
                        RelatedCallsigns = callsigns,
                        RelatedAreas = new List<string> { message.Area.Name },
                        RelatedMessageIds = new List<long> { message.Id },
                        Severity = callsignCount >= 5 ? AlertSeverity.Critical : AlertSeverity.High
                    });
                }
            }

            return alerts;
        }

        private async Task<List<Alert>> CheckSuspiciousSilenceAsync(AlertRule rule)
        {
            var alerts = new List<Alert>();

            // Анализируем активность по часам для каждого позывного
            var last7Days = DateTime.UtcNow.AddDays(-7);

            // Получаем историческую активность по часам
            var historicalActivity = await _dataContext.MessageCallsigns
                .Include(mc => mc.Message)
                .Include(mc => mc.Callsign)
                .Where(mc => mc.Message.DateTime >= last7Days)
                .Select(mc => new {
                    mc.Callsign.Name,
                    Hour = mc.Message.DateTime.Hour
                })
                .ToListAsync();

            // Группируем по позывным и часам
            var callsignHourlyPatterns = historicalActivity
                .GroupBy(x => x.Name)
                .Select(g => new
                {
                    Callsign = g.Key,
                    TotalMessages = g.Count(),
                    HourlyDistribution = g.GroupBy(x => x.Hour)
                                        .ToDictionary(hg => hg.Key, hg => hg.Count())
                })
                .Where(x => x.TotalMessages >= 10) // Достаточно данных для анализа
                .ToList();

            var now = DateTime.UtcNow;
            var currentHour = now.Hour;

            // Для каждого позывного проверяем, активен ли он сейчас
            foreach (var pattern in callsignHourlyPatterns)
            {
                // Определяем обычную активность в текущий час
                var usualActivity = pattern.HourlyDistribution.GetValueOrDefault(currentHour, 0);
                var totalActivity = pattern.TotalMessages;
                var usualPercentage = (double)usualActivity / totalActivity * 100;

                // Если обычно в этот час активность > 10%, но сейчас нет сообщений
                if (usualPercentage > 10)
                {
                    // Проверяем, был ли позывной активен в последние 2 часа
                    var last2Hours = now.AddHours(-2);
                    var recentActivity = await _dataContext.MessageCallsigns
                        .Include(mc => mc.Message)
                        .Where(mc => mc.Callsign.Name == pattern.Callsign &&
                               mc.Message.DateTime >= last2Hours)
                        .AnyAsync();

                    if (!recentActivity)
                    {
                        alerts.Add(new Alert
                        {
                            Title = $"Подозрительное молчание: {pattern.Callsign}",
                            Description = $"Позывной не активен в обычно активное время",
                            Details = $"Позывной {pattern.Callsign} обычно проявляет активность в {currentHour}:00 " +
                                     $"({usualPercentage:F1}% сообщений приходится на этот час).\n" +
                                     $"Однако за последние 2 часа не зафиксировано ни одного сообщения.",
                            RelatedCallsigns = new List<string> { pattern.Callsign },
                            Severity = usualPercentage > 20 ? AlertSeverity.High : AlertSeverity.Medium
                        });
                    }
                }
            }

            return alerts;
        }

        private async Task<List<Alert>> CheckPeakActivityAsync(AlertRule rule)
        {
            var alerts = new List<Alert>();

            // Определяем "нерабочее время" (например, 23:00-05:00)
            var now = DateTime.UtcNow;
            var currentHour = now.Hour;
            var isOffHours = currentHour >= 23 || currentHour <= 5;

            if (!isOffHours)
                return alerts; // Только в нерабочее время

            // Сравниваем активность с обычной для этого времени
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var last7Days = DateTime.UtcNow.AddDays(-7);

            // Средняя активность в это время за последние 7 дней
            var historicalOffHoursActivity = await _dataContext.Messages
                .Where(m => m.DateTime >= last7Days &&
                       (m.DateTime.Hour >= 23 || m.DateTime.Hour <= 5))
                .CountAsync();

            var offHoursCount = 7 * 6; // 6 часов в сутки * 7 дней
            var avgOffHoursActivity = (double)historicalOffHoursActivity / offHoursCount;

            // Активность в последний час
            var lastHourActivity = await _dataContext.Messages
                .Where(m => m.DateTime >= now.AddHours(-1))
                .CountAsync();

            if (avgOffHoursActivity > 0 && lastHourActivity > avgOffHoursActivity * 5)
            {
                // Получаем детали активности
                var recentMessages = await _dataContext.Messages
                    .Include(m => m.MessageCallsigns)
                        .ThenInclude(mc => mc.Callsign)
                    .Include(m => m.Area)
                    .Where(m => m.DateTime >= now.AddHours(-1))
                    .ToListAsync();

                var callsigns = recentMessages
                    .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                    .Distinct()
                    .ToList();

                var areas = recentMessages
                    .Select(m => m.Area.Name)
                    .Distinct()
                    .ToList();

                alerts.Add(new Alert
                {
                    Title = $"Пиковая активность в нерабочее время",
                    Description = $"Необычно высокая активность в ночные часы",
                    Details = $"За последний час зафиксировано {lastHourActivity} сообщений, " +
                             $"что в {lastHourActivity / avgOffHoursActivity:F1} раза выше средней ночной активности " +
                             $"({avgOffHoursActivity:F1} сообщений в час).\n" +
                             $"Активные позывные: {string.Join(", ", callsigns)}\n" +
                             $"Активные зоны: {string.Join(", ", areas)}",
                    RelatedCallsigns = callsigns,
                    RelatedAreas = areas,
                    Severity = lastHourActivity > avgOffHoursActivity * 10 ? AlertSeverity.Critical : AlertSeverity.High
                });
            }

            return alerts;
        }

        private async Task<List<Alert>> CheckCustomRuleAsync(AlertRule rule)
        {
            // Здесь можно реализовать проверку пользовательских правил
            // на основе выражения в ConditionExpression
            // Это может быть простой язык запросов или regex

            var alerts = new List<Alert>();

            try
            {
                // Простая реализация через регулярные выражения
                var regex = new Regex(rule.ConditionExpression, RegexOptions.IgnoreCase);

                var lastHour = DateTime.UtcNow.AddHours(-1);
                var messages = await _dataContext.Messages
                    .Include(m => m.MessageCallsigns)
                        .ThenInclude(mc => mc.Callsign)
                    .Include(m => m.Area)
                    .Where(m => m.DateTime >= lastHour)
                    .ToListAsync();

                foreach (var message in messages)
                {
                    if (regex.IsMatch(message.Dialog))
                    {
                        var callsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();

                        alerts.Add(new Alert
                        {
                            Title = $"Срабатывание пользовательского правила: {rule.Name}",
                            Description = rule.Description,
                            Details = $"Правило: {rule.Name}\n" +
                                     $"Позывные: {string.Join(", ", callsigns)}\n" +
                                     $"Зона: {message.Area.Name}\n" +
                                     $"Время: {message.DateTime:HH:mm}\n" +
                                     $"Текст: {Truncate(message.Dialog, 200)}",
                            RelatedCallsigns = callsigns,
                            RelatedAreas = new List<string> { message.Area.Name },
                            RelatedMessageIds = new List<long> { message.Id }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем выполнение
                Console.WriteLine($"Error checking custom rule {rule.Name}: {ex.Message}");
            }

            return alerts;
        }

        private string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }
    }
}