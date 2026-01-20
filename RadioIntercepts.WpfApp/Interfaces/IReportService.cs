// Core/Interfaces/Services/IReportService.cs
using RadioIntercepts.Core.Models;

namespace RadioIntercepts.WpfApp.Interfaces
{
    public interface IReportService
    {
        // Управление шаблонами
        Task<ReportTemplate> CreateTemplateAsync(ReportTemplate template);
        Task<ReportTemplate> UpdateTemplateAsync(int id, ReportTemplate template);
        Task DeleteTemplateAsync(int id);
        Task<ReportTemplate> GetTemplateAsync(int id);
        Task<IEnumerable<ReportTemplate>> GetTemplatesAsync();
        Task<IEnumerable<ReportTemplate>> GetTemplatesByTypeAsync(ReportType type);

        // Генерация отчетов
        Task<GeneratedReport> GenerateReportAsync(
            int templateId,
            Dictionary<string, object> parameters);

        Task<GeneratedReport> GenerateReportAsync(
            string templateName,
            Dictionary<string, object> parameters);

        Task<GeneratedReport> GenerateDailySummaryAsync(
            DateTime date,
            ReportFormat format = ReportFormat.Pdf);

        Task<GeneratedReport> GenerateCallsignDossierAsync(
            string callsign,
            DateTime startDate,
            DateTime endDate,
            ReportFormat format = ReportFormat.Pdf);

        Task<GeneratedReport> GenerateAreaActivityReportAsync(
            string area,
            DateTime startDate,
            DateTime endDate,
            ReportFormat format = ReportFormat.Pdf);

        // Управление сгенерированными отчетами
        Task<GeneratedReport> GetGeneratedReportAsync(string reportId);
        Task<IEnumerable<GeneratedReport>> GetGeneratedReportsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? templateName = null);
        Task DeleteGeneratedReportAsync(string reportId);
        Task<byte[]> DownloadReportAsync(string reportId);

        // Статус и мониторинг
        Task<ReportStatus> GetReportStatusAsync(string reportId);
        Task<IEnumerable<GeneratedReport>> GetPendingReportsAsync();
        Task RetryFailedReportAsync(string reportId);

        // Данные для отчетов
        Task<DailySummaryReport> GetDailySummaryDataAsync(DateTime date);
        Task<CallsignDossier> GetCallsignDossierDataAsync(
            string callsign,
            DateTime startDate,
            DateTime endDate);
        Task<AreaActivityReport> GetAreaActivityDataAsync(
            string area,
            DateTime startDate,
            DateTime endDate);
    }

    public enum ReportFormat
    {
        Pdf,
        Excel,
        Word,
        Html,
        Csv,
        Json
    }
}