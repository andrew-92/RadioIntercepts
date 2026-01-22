using RadioIntercepts.Core.Models.Alerts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Interfaces.Services
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
}
