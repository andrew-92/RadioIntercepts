//using Microsoft.EntityFrameworkCore;
//using RadioIntercepts.Core.Models;
//using RadioIntercepts.Infrastructure.Data;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.Json;
//using System.Threading.Tasks;
//using RadioIntercepts.Analysis.Interfaces.Services;
//using RadioIntercepts.Core.Interfaces;
//using RadioIntercepts.Core.Models.Alerts;
//using RadioIntercepts.Core.Models.Communication;
//using RadioIntercepts.Core.Models.Reports;

//namespace RadioIntercepts.Analysis.Services.Reports
//{
//    public class ReportService : IReportService
//    {
//        private readonly AppDbContext _context;
//        private readonly IAlertService _alertService;
//        private readonly ICodeAnalysisService _codeAnalysisService;
//        private readonly IDialogPatternAnalyzer _dialogAnalyzer;
//        private readonly ISemanticSearchService _searchService;

//        // Встроенные шаблоны отчетов
//        private readonly List<ReportTemplate> _builtInTemplates = new()
//        {
//            new ReportTemplate
//            {
//                Id = 1,
//                Name = "Ежедневная сводка",
//                Description = "Сводка активности за день: количество сообщений, активные позывные, алерты",
//                Type = ReportType.DailySummary,
//                TemplatePath = "Templates/DailySummary.html",
//                Parameters = new List<ReportParameter>
//                {
//                    new ReportParameter
//                    {
//                        Name = "date",
//                        Type = "date",
//                        DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
//                        Required = true
//                    },
//                    new ReportParameter
//                    {
//                        Name = "includeAlerts",
//                        Type = "bool",
//                        DefaultValue = "true",
//                        Required = false
//                    },
//                    new ReportParameter
//                    {
//                        Name = "includePatterns",
//                        Type = "bool",
//                        DefaultValue = "true",
//                        Required = false
//                    }
//                }
//            },
//            new ReportTemplate
//            {
//                Id = 2,
//                Name = "Досье позывного",
//                Description = "Подробный отчет по активности конкретного позывного",
//                Type = ReportType.CallsignActivity,
//                TemplatePath = "Templates/CallsignDossier.html",
//                Parameters = new List<ReportParameter>
//                {
//                    new ReportParameter
//                    {
//                        Name = "callsign",
//                        Type = "string",
//                        DefaultValue = "",
//                        Required = true
//                    },
//                    new ReportParameter
//                    {
//                        Name = "startDate",
//                        Type = "date",
//                        DefaultValue = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd"),
//                        Required = true
//                    },
//                    new ReportParameter
//                    {
//                        Name = "endDate",
//                        Type = "date",
//                        DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
//                        Required = true
//                    },
//                    new ReportParameter
//                    {
//                        Name = "includeNetwork",
//                        Type = "bool",
//                        DefaultValue = "true",
//                        Required = false
//                    }
//                }
//            },
//            new ReportTemplate
//            {
//                Id = 3,
//                Name = "Анализ зоны",
//                Description = "Отчет по активности в конкретной зоне",
//                Type = ReportType.AreaAnalysis,
//                TemplatePath = "Templates/AreaAnalysis.html",
//                Parameters = new List<ReportParameter>
//                {
//                    new ReportParameter
//                    {
//                        Name = "area",
//                        Type = "string",
//                        DefaultValue = "",
//                        Required = true
//                    },
//                    new ReportParameter
//                    {
//                        Name = "startDate",
//                        Type = "date",
//                        DefaultValue = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd"),
//                        Required = true
//                    },
//                    new ReportParameter
//                    {
//                        Name = "endDate",
//                        Type = "date",
//                        DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
//                        Required = true
//                    },
//                    new ReportParameter
//                    {
//                        Name = "includeHeatmap",
//                        Type = "bool",
//                        DefaultValue = "true",
//                        Required = false
//                    }
//                }
//            },
//            new ReportTemplate
//            {
//                Id = 4,
//                Name = "Поток коммуникаций",
//                Description = "Визуализация потока сообщений между позывными",
//                Type = ReportType.CommunicationFlow,
//                TemplatePath = "Templates/CommunicationFlow.html",
//                Parameters = new List<ReportParameter>
//                {
//                    new ReportParameter
//                    {
//                        Name = "startDate",
//                        Type = "date",
//                        DefaultValue = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"),
//                        Required = true
//                    },
//                    new ReportParameter
//                    {
//                        Name = "endDate",
//                        Type = "date",
//                        DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
//                        Required = true
//                    },
//                    new ReportParameter
//                    {
//                        Name = "minInteractions",
//                        Type = "int",
//                        DefaultValue = "3",
//                        Required = false
//                    }
//                }
//            },
//            new ReportTemplate
//            {
//                Id = 5,
//                Name = "Сводка алертов",
//                Description = "Отчет по сработавшим алертам за период",
//                Type = ReportType.AlertSummary,
//                TemplatePath = "Templates/AlertSummary.html",
//                Parameters = new List<ReportParameter>
//                {
//                    new ReportParameter
//                    {
//                        Name = "startDate",
//                        Type = "date",
//                        DefaultValue = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd"),
//                        Required = true
//                    },
//                    new ReportParameter
//                    {
//                        Name = "endDate",
//                        Type = "date",
//                        DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
//                        Required = true
//                    },
//                    new ReportParameter
//                    {
//                        Name = "minSeverity",
//                        Type = "list",
//                        DefaultValue = "Low",
//                        Required = false,
//                        Options = new List<string> { "Info", "Low", "Medium", "High", "Critical" }
//                    }
//                }
//            }
//        };

//        public ReportService(
//            AppDbContext context,
//            IAlertService alertService,
//            ICodeAnalysisService codeAnalysisService,
//            IDialogPatternAnalyzer dialogAnalyzer,
//            ISemanticSearchService searchService)
//        {
//            _context = context;
//            _alertService = alertService;
//            _codeAnalysisService = codeAnalysisService;
//            _dialogAnalyzer = dialogAnalyzer;
//            _searchService = searchService;
//        }

//        public async Task<List<ReportTemplate>> GetReportTemplatesAsync(bool onlyActive = true)
//        {
//            // В реальном приложении здесь был бы запрос к БД
//            // Для примера возвращаем встроенные шаблоны

//            var templates = _builtInTemplates.ToList();

//            // В будущем можно добавить загрузку из БД
//            // var query = _context.ReportTemplates.AsQueryable();
//            // if (onlyActive) query = query.Where(t => t.IsActive);
//            // return await query.ToListAsync();

//            return await templates;
//        }

//        public async Task<ReportTemplate> GetReportTemplateAsync(int id)
//        {
//            var template = _builtInTemplates.FirstOrDefault(t => t.Id == id);

//            if (template == null)
//                throw new KeyNotFoundException($"Шаблон отчета с ID {id} не найден");

//            return await Task.FromResult(template);
//        }

//        public async Task<ReportTemplate> CreateReportTemplateAsync(ReportTemplate template)
//        {
//            // В реальном приложении здесь была бы сохранение в БД
//            template.Id = _builtInTemplates.Max(t => t.Id) + 1;
//            template.CreatedAt = DateTime.UtcNow;
//            template.UpdatedAt = DateTime.UtcNow;

//            _builtInTemplates.Add(template);

//            return await Task.FromResult(template);
//        }

//        public async Task<ReportTemplate> UpdateReportTemplateAsync(ReportTemplate template)
//        {
//            var existingTemplate = _builtInTemplates.FirstOrDefault(t => t.Id == template.Id);

//            if (existingTemplate == null)
//                throw new KeyNotFoundException($"Шаблон отчета с ID {template.Id} не найден");

//            existingTemplate.Name = template.Name;
//            existingTemplate.Description = template.Description;
//            existingTemplate.Type = template.Type;
//            existingTemplate.TemplatePath = template.TemplatePath;
//            existingTemplate.Parameters = template.Parameters;
//            existingTemplate.UpdatedAt = DateTime.UtcNow;

//            return await Task.FromResult(existingTemplate);
//        }

//        public async Task DeleteReportTemplateAsync(int id)
//        {
//            var template = _builtInTemplates.FirstOrDefault(t => t.Id == id);

//            if (template != null)
//            {
//                _builtInTemplates.Remove(template);
//            }

//            await Task.CompletedTask;
//        }

//        public async Task ToggleTemplateAsync(int id, bool isActive)
//        {
//            // В реальном приложении здесь было бы обновление в БД
//            await Task.CompletedTask;
//        }

//        public async Task<GeneratedReport> GenerateReportAsync(int templateId, Dictionary<string, object> parameters)
//        {
//            var template = await GetReportTemplateAsync(templateId);

//            // Валидация параметров
//            ValidateParameters(template.Parameters, parameters);

//            // Создание объекта отчета
//            var report = new GeneratedReport
//            {
//                ReportId = Guid.NewGuid().ToString(),
//                TemplateName = template.Name,
//                GeneratedAt = DateTime.UtcNow,
//                Parameters = parameters,
//                Status = ReportStatus.Pending,
//                ContentType = "pdf", // По умолчанию PDF
//                FileName = GenerateFileName(template.Name, parameters)
//            };

//            // В реальном приложении здесь было бы сохранение в БД
//            // _context.GeneratedReports.Add(report);
//            // await _context.SaveChangesAsync();

//            // Асинхронная генерация отчета
//            _ = Task.Run(async () =>
//            {
//                await GenerateReportContentAsync(report, template, parameters);
//            });

//            return report;
//        }

//        public async Task<GeneratedReport> GenerateReportAsync(string templateName, Dictionary<string, object> parameters)
//        {
//            var template = _builtInTemplates.FirstOrDefault(t => t.Name == templateName);

//            if (template == null)
//                throw new KeyNotFoundException($"Шаблон отчета '{templateName}' не найден");

//            return await GenerateReportAsync(template.Id, parameters);
//        }

//        public async Task<GeneratedReport> GenerateDailySummaryAsync(DateTime date, ReportFormat format = ReportFormat.Pdf)
//        {
//            var parameters = new Dictionary<string, object>
//            {
//                { "date", date.ToString("yyyy-MM-dd") },
//                { "format", format.ToString().ToLower() }
//            };

//            return await GenerateReportAsync("Ежедневная сводка", parameters);
//        }

//        public async Task<GeneratedReport> GenerateCallsignDossierAsync(string callsign, DateTime startDate, DateTime endDate, ReportFormat format = ReportFormat.Pdf)
//        {
//            var parameters = new Dictionary<string, object>
//            {
//                { "callsign", callsign },
//                { "startDate", startDate.ToString("yyyy-MM-dd") },
//                { "endDate", endDate.ToString("yyyy-MM-dd") },
//                { "format", format.ToString().ToLower() }
//            };

//            return await GenerateReportAsync("Досье позывного", parameters);
//        }

//        public async Task<GeneratedReport> GenerateAreaActivityReportAsync(string area, DateTime startDate, DateTime endDate, ReportFormat format = ReportFormat.Pdf)
//        {
//            var parameters = new Dictionary<string, object>
//            {
//                { "area", area },
//                { "startDate", startDate.ToString("yyyy-MM-dd") },
//                { "endDate", endDate.ToString("yyyy-MM-dd") },
//                { "format", format.ToString().ToLower() }
//            };

//            return await GenerateReportAsync("Анализ зоны", parameters);
//        }

//        public async Task<List<GeneratedReport>> GetGeneratedReportsAsync(DateTime? from = null, DateTime? to = null, string? templateName = null)
//        {
//            // В реальном приложении здесь был бы запрос к БД
//            // var query = _context.GeneratedReports.AsQueryable();

//            // if (from.HasValue)
//            //     query = query.Where(r => r.GeneratedAt >= from.Value);
//            // if (to.HasValue)
//            //     query = query.Where(r => r.GeneratedAt <= to.Value);
//            // if (!string.IsNullOrEmpty(templateName))
//            //     query = query.Where(r => r.TemplateName == templateName);

//            // return await query.OrderByDescending(r => r.GeneratedAt).ToListAsync();

//            return await Task.FromResult(new List<GeneratedReport>());
//        }

//        public async Task<GeneratedReport> GetGeneratedReportAsync(string reportId)
//        {
//            // В реальном приложении здесь был бы запрос к БД
//            // return await _context.GeneratedReports.FirstOrDefaultAsync(r => r.ReportId == reportId);

//            return await Task.FromResult<GeneratedReport>(null);
//        }

//        public async Task DeleteGeneratedReportAsync(string reportId)
//        {
//            // В реальном приложении здесь было бы удаление из БД
//            // var report = await GetGeneratedReportAsync(reportId);
//            // if (report != null)
//            // {
//            //     _context.GeneratedReports.Remove(report);
//            //     await _context.SaveChangesAsync();
//            // }

//            await Task.CompletedTask;
//        }

//        public async Task<byte[]> DownloadReportContentAsync(string reportId)
//        {
//            var report = await GetGeneratedReportAsync(reportId);

//            if (report == null)
//                throw new KeyNotFoundException($"Отчет с ID {reportId} не найден");

//            if (report.Status != ReportStatus.Completed)
//                throw new InvalidOperationException($"Отчет еще не сгенерирован. Статус: {report.Status}");

//            return report.Content;
//        }

//        public async Task<ReportStatus> GetReportStatusAsync(string reportId)
//        {
//            var report = await GetGeneratedReportAsync(reportId);

//            return report?.Status ?? ReportStatus.Failed;
//        }

//        public async Task<List<GeneratedReport>> GetPendingReportsAsync()
//        {
//            // В реальном приложении здесь был бы запрос к БД
//            // return await _context.GeneratedReports
//            //     .Where(r => r.Status == ReportStatus.Pending || r.Status == ReportStatus.Generating)
//            //     .ToListAsync();

//            return await Task.FromResult(new List<GeneratedReport>());
//        }

//        public async Task<List<GeneratedReport>> GetFailedReportsAsync()
//        {
//            // В реальном приложении здесь был бы запрос к БД
//            // return await _context.GeneratedReports
//            //     .Where(r => r.Status == ReportStatus.Failed)
//            //     .ToListAsync();

//            return await Task.FromResult(new List<GeneratedReport>());
//        }

//        public async Task RetryFailedReportAsync(string reportId)
//        {
//            var report = await GetGeneratedReportAsync(reportId);

//            if (report == null)
//                throw new KeyNotFoundException($"Отчет с ID {reportId} не найден");

//            if (report.Status != ReportStatus.Failed)
//                throw new InvalidOperationException($"Можно повторить только неудавшиеся отчеты. Текущий статус: {report.Status}");

//            // Обновляем статус и перезапускаем генерацию
//            report.Status = ReportStatus.Pending;
//            report.ErrorMessage = null;

//            // В реальном приложении здесь было бы сохранение в БД
//            // _context.GeneratedReports.Update(report);
//            // await _context.SaveChangesAsync();

//            // Находим шаблон и параметры
//            var template = _builtInTemplates.FirstOrDefault(t => t.Name == report.TemplateName);
//            if (template != null)
//            {
//                _ = Task.Run(async () =>
//                {
//                    await GenerateReportContentAsync(report, template, report.Parameters);
//                });
//            }
//        }

//        public async Task<DailySummaryReport> GetDailySummaryDataAsync(DateTime date)
//        {
//            var report = new DailySummaryReport
//            {
//                ReportDate = date
//            };

//            // Получаем сообщения за указанный день
//            var startDate = date.Date;
//            var endDate = date.Date.AddDays(1).AddTicks(-1);

//            var messages = await _context.Messages
//                .Include(m => m.MessageCallsigns)
//                    .ThenInclude(mc => mc.Callsign)
//                .Include(m => m.Area)
//                .Where(m => m.DateTime >= startDate && m.DateTime <= endDate)
//                .ToListAsync();

//            report.TotalMessages = messages.Count;
//            report.UniqueCallsigns = messages
//                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                .Distinct()
//                .Count();

//            report.ActiveAreas = messages
//                .Select(m => m.Area.Name)
//                .Distinct()
//                .Count();

//            // Топ позывных по активности
//            report.TopCallsigns = messages
//                .SelectMany(m => m.MessageCallsigns.Select(mc => new { mc.Callsign.Name, m.DateTime, m.Area.Name }))
//                .GroupBy(x => x.Name)
//                .Select(g => new CallsignActivity
//                {
//                    Callsign = g.Key,
//                    MessageCount = g.Count(),
//                    InteractionCount = CalculateInteractionCount(g.Key, messages),
//                    ActiveAreas = g.Select(x => x.Name1).Distinct().ToList(),
//                    AverageResponseTime = CalculateAverageResponseTime(g.Key, messages),
//                    Role = DetermineCallsignRole(g.Key, messages)
//                })
//                .OrderByDescending(c => c.MessageCount)
//                .Take(10)
//                .ToList();

//            // Топ зон по активности
//            report.TopAreas = messages
//                .GroupBy(m => m.Area.Name)
//                .Select(g => new AreaActivity
//                {
//                    Area = g.Key,
//                    MessageCount = g.Count(),
//                    ActiveCallsigns = g.SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                                      .Distinct()
//                                      .Count(),
//                    PeakActivityTime = g.GroupBy(m => m.DateTime.Hour)
//                                       .OrderByDescending(hg => hg.Count())
//                                       .Select(hg => hg.First().DateTime)
//                                       .FirstOrDefault(),
//                    ActivityLevel = CalculateActivityLevel(g.Key, messages)
//                })
//                .OrderByDescending(a => a.MessageCount)
//                .Take(10)
//                .ToList();

//            // Алерты за день
//            var alerts = await _alertService.GetAlertsAsync(startDate, endDate);
//            report.Alerts = alerts
//                .GroupBy(a => a.Rule?.Name ?? "Неизвестное правило")
//                .Select(g => new AlertSummary
//                {
//                    RuleName = g.Key,
//                    Count = g.Count(),
//                    HighestSeverity = g.Max(a => a.Severity),
//                    AffectedCallsigns = g.SelectMany(a => a.RelatedCallsigns).Distinct().ToList(),
//                    FirstAlert = g.Min(a => a.DetectedAt),
//                    LastAlert = g.Max(a => a.DetectedAt)
//                })
//                .ToList();

//            // Обнаруженные паттерны
//            report.DetectedPatterns = await DetectPatternsForDayAsync(messages);

//            // Метрики коммуникаций
//            report.Metrics = CalculateCommunicationMetrics(messages);

//            // Ключевые наблюдения
//            report.Observations = await ExtractKeyObservationsAsync(messages, date);

//            return report;
//        }

//        public async Task<CallsignDossier> GetCallsignDossierDataAsync(string callsign, DateTime startDate, DateTime endDate)
//        {
//            var dossier = new CallsignDossier
//            {
//                Callsign = callsign
//            };

//            // Получаем все сообщения позывного за период
//            var messages = await _context.MessageCallsigns
//                .Include(mc => mc.Message)
//                    .ThenInclude(m => m.Area)
//                .Include(mc => mc.Callsign)
//                .Where(mc => mc.Callsign.Name == callsign &&
//                       mc.Message.DateTime >= startDate &&
//                       mc.Message.DateTime <= endDate)
//                .Select(mc => mc.Message)
//                .OrderBy(m => m.DateTime)
//                .ToListAsync();

//            if (!messages.Any())
//                return dossier;

//            dossier.FirstSeen = messages.Min(m => m.DateTime);
//            dossier.LastSeen = messages.Max(m => m.DateTime);
//            dossier.TotalMessages = messages.Count;

//            // Частые собеседники
//            dossier.FrequentInterlocutors = await FindFrequentInterlocutorsAsync(callsign, messages);

//            // Активные зоны
//            dossier.ActiveAreas = messages
//                .Select(m => m.Area.Name)
//                .Distinct()
//                .ToList();

//            // Стиль коммуникации
//            dossier.Style = AnalyzeCommunicationStyle(callsign, messages);

//            // Роль позывного
//            dossier.Role = await AnalyzeCallsignRoleAsync(callsign, messages);

//            // Ключевые взаимодействия
//            dossier.KeyInteractions = await ExtractKeyInteractionsAsync(callsign, messages);

//            // Участие в паттернах
//            dossier.PatternInvolvement = await AnalyzePatternInvolvementAsync(callsign, messages);

//            // Участие в алертах
//            dossier.Alerts = await GetAlertInvolvementAsync(callsign, startDate, endDate);

//            // Изменения в поведении
//            dossier.BehavioralChanges = await DetectBehavioralChangesAsync(callsign, messages);

//            // Рекомендации
//            dossier.Recommendations = GenerateRecommendations(dossier);

//            return dossier;
//        }

//        public async Task<AreaActivityReport> GetAreaActivityDataAsync(string area, DateTime startDate, DateTime endDate)
//        {
//            var report = new AreaActivityReport
//            {
//                Area = area,
//                ReportPeriodStart = startDate,
//                ReportPeriodEnd = endDate
//            };

//            // Получаем все сообщения в зоне за период
//            var messages = await _context.Messages
//                .Include(m => m.MessageCallsigns)
//                    .ThenInclude(mc => mc.Callsign)
//                .Include(m => m.Area)
//                .Where(m => m.Area.Name == area &&
//                       m.DateTime >= startDate &&
//                       m.DateTime <= endDate)
//                .OrderBy(m => m.DateTime)
//                .ToListAsync();

//            if (!messages.Any())
//                return report;

//            // Активные позывные в зоне
//            report.ActiveCallsigns = messages
//                .SelectMany(m => m.MessageCallsigns.Select(mc => new { mc.Callsign.Name, m.DateTime }))
//                .GroupBy(x => x.Name)
//                .Select(g => new CallsignActivity
//                {
//                    Callsign = g.Key,
//                    MessageCount = g.Count(),
//                    InteractionCount = CalculateInteractionCountInArea(g.Key, area, messages),
//                    ActiveAreas = new List<string> { area },
//                    AverageResponseTime = CalculateAverageResponseTimeInArea(g.Key, area, messages),
//                    Role = DetermineCallsignRoleInArea(g.Key, area, messages)
//                })
//                .OrderByDescending(c => c.MessageCount)
//                .ToList();

//            // Таймлайн активности
//            report.Timeline = BuildActivityTimeline(messages, startDate, endDate);

//            // Распространенные паттерны коммуникаций
//            report.CommonPatterns = await DetectCommunicationPatternsInAreaAsync(area, messages);

//            // Алерты в зоне
//            report.AreaAlerts = await GetAreaAlertsAsync(area, startDate, endDate);

//            // Метрики зоны
//            report.Metrics = CalculateAreaMetrics(area, messages);

//            // Наблюдения
//            report.Observations = await ExtractAreaObservationsAsync(area, messages);

//            // Рекомендации
//            report.Recommendations = GenerateAreaRecommendations(report);

//            return report;
//        }

//        public async Task<ReportStatistics> GetReportStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
//        {
//            var stats = new ReportStatistics
//            {
//                PeriodStart = startDate ?? DateTime.UtcNow.AddDays(-30),
//                PeriodEnd = endDate ?? DateTime.UtcNow
//            };

//            // В реальном приложении здесь был бы запрос к БД
//            // var reports = await _context.GeneratedReports
//            //     .Where(r => r.GeneratedAt >= stats.PeriodStart && r.GeneratedAt <= stats.PeriodEnd)
//            //     .ToListAsync();

//            var reports = new List<GeneratedReport>(); // Заглушка

//            stats.TotalReports = reports.Count;
//            stats.CompletedReports = reports.Count(r => r.Status == ReportStatus.Completed);
//            stats.FailedReports = reports.Count(r => r.Status == ReportStatus.Failed);
//            stats.PendingReports = reports.Count(r => r.Status == ReportStatus.Pending || r.Status == ReportStatus.Generating);

//            // По шаблонам
//            stats.ReportsByTemplate = reports
//                .GroupBy(r => r.TemplateName)
//                .ToDictionary(g => g.Key, g => g.Count());

//            // По форматам (в реальном приложении было бы из ContentType)
//            // stats.ReportsByFormat = reports
//            //     .GroupBy(r => r.ContentType)
//            //     .ToDictionary(g => Enum.Parse<ReportFormat>(g.Key, true), g => g.Count());

//            return stats;
//        }

//        // Вспомогательные методы

//        private void ValidateParameters(List<ReportParameter> templateParams, Dictionary<string, object> userParams)
//        {
//            foreach (var templateParam in templateParams)
//            {
//                if (templateParam.Required && !userParams.ContainsKey(templateParam.Name))
//                    throw new ArgumentException($"Обязательный параметр '{templateParam.Name}' отсутствует");

//                if (userParams.TryGetValue(templateParam.Name, out var value))
//                {
//                    // Проверка типа
//                    ValidateParameterType(templateParam.Type, value, templateParam.Name);

//                    // Проверка опций для списка
//                    if (templateParam.Type == "list" && templateParam.Options.Any())
//                    {
//                        var stringValue = value.ToString();
//                        if (!templateParam.Options.Contains(stringValue))
//                            throw new ArgumentException($"Значение параметра '{templateParam.Name}' должно быть одним из: {string.Join(", ", templateParam.Options)}");
//                    }
//                }
//            }
//        }

//        private void ValidateParameterType(string expectedType, object value, string paramName)
//        {
//            try
//            {
//                switch (expectedType.ToLower())
//                {
//                    case "date":
//                        if (value is string strDate)
//                            DateTime.Parse(strDate);
//                        else if (!(value is DateTime))
//                            throw new ArgumentException();
//                        break;

//                    case "int":
//                        if (value is string strInt)
//                            int.Parse(strInt);
//                        else if (!(value is int))
//                            throw new ArgumentException();
//                        break;

//                    case "bool":
//                        if (value is string strBool)
//                            bool.Parse(strBool);
//                        else if (!(value is bool))
//                            throw new ArgumentException();
//                        break;

//                    case "string":
//                        // Всегда валидно
//                        break;

//                    case "list":
//                        if (!(value is string))
//                            throw new ArgumentException();
//                        break;
//                }
//            }
//            catch
//            {
//                throw new ArgumentException($"Параметр '{paramName}' должен быть типа '{expectedType}'");
//            }
//        }

//        private async Task GenerateReportContentAsync(GeneratedReport report, ReportTemplate template, Dictionary<string, object> parameters)
//        {
//            try
//            {
//                report.Status = ReportStatus.Generating;

//                // В реальном приложении здесь была бы генерация контента
//                // В зависимости от типа отчета и шаблона

//                // Примерная логика:
//                // 1. Получить данные для отчета
//                // 2. Сгенерировать HTML через Razor
//                // 3. Конвертировать в PDF (если нужно)
//                // 4. Сохранить в report.Content

//                // Заглушка: имитация генерации
//                await Task.Delay(2000); // Имитация долгой генерации

//                var reportData = new
//                {
//                    Template = template.Name,
//                    Parameters = parameters,
//                    GeneratedAt = DateTime.UtcNow,
//                    Content = "Сгенерированный отчет"
//                };

//                report.Content = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reportData));
//                report.Status = ReportStatus.Completed;
//            }
//            catch (Exception ex)
//            {
//                report.Status = ReportStatus.Failed;
//                report.ErrorMessage = ex.Message;
//            }

//            // В реальном приложении здесь было бы сохранение в БД
//            // _context.GeneratedReports.Update(report);
//            // await _context.SaveChangesAsync();
//        }

//        private string GenerateFileName(string templateName, Dictionary<string, object> parameters)
//        {
//            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
//            var paramString = string.Join("_", parameters
//                .Where(p => p.Value != null)
//                .Select(p => $"{p.Key}_{p.Value}"))
//                .Replace(" ", "_")
//                .Replace(":", "")
//                .Replace("/", "-");

//            return $"{templateName}_{paramString}_{timestamp}.pdf";
//        }

//        private int CalculateInteractionCount(string callsign, List<Message> messages)
//        {
//            // Подсчет взаимодействий позывного с другими позывными
//            var interactions = new HashSet<string>();

//            foreach (var message in messages)
//            {
//                var messageCallsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
//                if (messageCallsigns.Contains(callsign))
//                {
//                    foreach (var otherCallsign in messageCallsigns.Where(c => c != callsign))
//                    {
//                        interactions.Add(otherCallsign);
//                    }
//                }
//            }

//            return interactions.Count;
//        }

//        private TimeSpan CalculateAverageResponseTime(string callsign, List<Message> messages)
//        {
//            var responseTimes = new List<TimeSpan>();
//            var callsignMessages = messages
//                .Where(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign))
//                .OrderBy(m => m.DateTime)
//                .ToList();

//            for (int i = 1; i < callsignMessages.Count; i++)
//            {
//                var timeDiff = callsignMessages[i].DateTime - callsignMessages[i - 1].DateTime;
//                if (timeDiff.TotalHours < 1) // Игнорируем большие промежутки
//                {
//                    responseTimes.Add(timeDiff);
//                }
//            }

//            return responseTimes.Any()
//                ? TimeSpan.FromSeconds(responseTimes.Average(ts => ts.TotalSeconds))
//                : TimeSpan.Zero;
//        }

//        private string DetermineCallsignRole(string callsign, List<Message> messages)
//        {
//            // Упрощенная логика определения роли
//            var callsignMessages = messages
//                .Where(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign))
//                .ToList();

//            if (!callsignMessages.Any())
//                return "Неизвестно";

//            // Анализ содержания сообщений
//            var allText = string.Join(" ", callsignMessages.Select(m => m.Dialog));

//            if (allText.Contains("приказ") || allText.Contains("команду") || allText.Contains("выполнить"))
//                return "Командир";

//            if (allText.Contains("докладываю") || allText.Contains("отчет") || allText.Contains("наблюдаю"))
//                return "Наблюдатель";

//            if (allText.Contains("координирую") || allText.Contains("согласую") || allText.Contains("организую"))
//                return "Координатор";

//            return "Исполнитель";
//        }

//        private double CalculateActivityLevel(string area, List<Message> messages)
//        {
//            var areaMessages = messages.Where(m => m.Area.Name == area).ToList();

//            if (!areaMessages.Any())
//                return 0;

//            // Нормализованный уровень активности (0-1)
//            var maxMessages = messages.Max(m => messages.Count);
//            return maxMessages > 0 ? (double)areaMessages.Count / maxMessages : 0;
//        }

//        private async Task<List<PatternSummary>> DetectPatternsForDayAsync(List<Message> messages)
//        {
//            var patterns = new List<PatternSummary>();

//            // Упрощенная логика обнаружения паттернов
//            // В реальном приложении здесь был бы сложный анализ

//            // Паттерн "Утренний брифинг"
//            var morningMessages = messages.Where(m => m.DateTime.Hour >= 6 && m.DateTime.Hour <= 9).ToList();
//            if (morningMessages.Count >= 5)
//            {
//                patterns.Add(new PatternSummary
//                {
//                    PatternType = "Утренний брифинг",
//                    Occurrences = 1,
//                    Confidence = 0.8,
//                    ExampleCallsigns = morningMessages
//                        .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                        .Distinct()
//                        .Take(5)
//                        .ToList(),
//                    Description = "Активная коммуникация в утренние часы"
//                });
//            }

//            // Паттерн "Смена дежурства"
//            var shiftChangeMessages = messages.Where(m => m.DateTime.Hour >= 18 && m.DateTime.Hour <= 20).ToList();
//            if (shiftChangeMessages.Count >= 3)
//            {
//                patterns.Add(new PatternSummary
//                {
//                    PatternType = "Смена дежурства",
//                    Occurrences = 1,
//                    Confidence = 0.7,
//                    ExampleCallsigns = shiftChangeMessages
//                        .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                        .Distinct()
//                        .Take(5)
//                        .ToList(),
//                    Description = "Активность во время смены дежурств"
//                });
//            }

//            return patterns;
//        }

//        private CommunicationMetrics CalculateCommunicationMetrics(List<Message> messages)
//        {
//            var metrics = new CommunicationMetrics();

//            if (!messages.Any())
//                return metrics;

//            // Упрощенные расчеты метрик
//            var uniqueCallsigns = messages
//                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                .Distinct()
//                .Count();

//            // Плотность сети (отношение реальных связей к возможным)
//            var interactions = new HashSet<(string, string)>();
//            foreach (var message in messages)
//            {
//                var callsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
//                for (int i = 0; i < callsigns.Count; i++)
//                {
//                    for (int j = i + 1; j < callsigns.Count; j++)
//                    {
//                        interactions.Add((callsigns[i], callsigns[j]));
//                    }
//                }
//            }

//            var maxPossibleConnections = uniqueCallsigns * (uniqueCallsigns - 1) / 2;
//            metrics.NetworkDensity = maxPossibleConnections > 0
//                ? (double)interactions.Count / maxPossibleConnections
//                : 0;

//            // Другие метрики
//            metrics.ResponseRate = CalculateResponseRate(messages);
//            metrics.AverageReactionTime = CalculateAverageReactionTime(messages);
//            metrics.FlowEfficiency = CalculateFlowEfficiency(messages);
//            metrics.Centralization = CalculateCentralization(messages);

//            return metrics;
//        }

//        private async Task<List<KeyObservation>> ExtractKeyObservationsAsync(List<Message> messages, DateTime date)
//        {
//            var observations = new List<KeyObservation>();

//            // Высокая активность
//            if (messages.Count > 100)
//            {
//                observations.Add(new KeyObservation
//                {
//                    Type = "high_activity",
//                    Description = $"Высокая активность: {messages.Count} сообщений за день",
//                    Impact = "medium",
//                    ObservedAt = date,
//                    RelatedCallsigns = messages
//                        .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                        .Distinct()
//                        .Take(5)
//                        .ToList(),
//                    Recommendations = new List<string>
//                    {
//                        "Проверить причины повышенной активности",
//                        "Усилить мониторинг зон с высокой активностью"
//                    }
//                });
//            }

//            // Новые позывные
//            var todayCallsigns = messages
//                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                .Distinct()
//                .ToList();

//            var yesterday = date.AddDays(-1);
//            var yesterdayMessages = await _context.Messages
//                .Include(m => m.MessageCallsigns)
//                    .ThenInclude(mc => mc.Callsign)
//                .Where(m => m.DateTime >= yesterday.Date && m.DateTime < date.Date)
//                .ToListAsync();

//            var yesterdayCallsigns = yesterdayMessages
//                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                .Distinct()
//                .ToList();

//            var newCallsigns = todayCallsigns.Except(yesterdayCallsigns).ToList();
//            if (newCallsigns.Any())
//            {
//                observations.Add(new KeyObservation
//                {
//                    Type = "new_callsign",
//                    Description = $"Обнаружены новые позывные: {string.Join(", ", newCallsigns)}",
//                    Impact = "low",
//                    ObservedAt = date,
//                    RelatedCallsigns = newCallsigns,
//                    Recommendations = new List<string>
//                    {
//                        "Отслеживать активность новых позывных",
//                        "Проверить связь новых позывных с существующими"
//                    }
//                });
//            }

//            return observations;
//        }

//        private double CalculateResponseRate(List<Message> messages)
//        {
//            // Упрощенный расчет коэффициента ответов
//            var responsePairs = 0;
//            var totalMessages = messages.Count;

//            // Более сложная логика в реальном приложении
//            return totalMessages > 0 ? (double)responsePairs / totalMessages : 0;
//        }

//        private TimeSpan CalculateAverageReactionTime(List<Message> messages)
//        {
//            // Упрощенный расчет среднего времени реакции
//            return TimeSpan.FromMinutes(5); // Заглушка
//        }

//        private double CalculateFlowEfficiency(List<Message> messages)
//        {
//            // Упрощенный расчет эффективности потока
//            return 0.7; // Заглушка
//        }

//        private double CalculateCentralization(List<Message> messages)
//        {
//            // Упрощенный расчет централизации сети
//            return 0.3; // Заглушка
//        }

//        private async Task<List<string>> FindFrequentInterlocutorsAsync(string callsign, List<Message> messages)
//        {
//            var interlocutors = new Dictionary<string, int>();

//            foreach (var message in messages)
//            {
//                var messageCallsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
//                if (messageCallsigns.Contains(callsign))
//                {
//                    foreach (var otherCallsign in messageCallsigns.Where(c => c != callsign))
//                    {
//                        if (!interlocutors.ContainsKey(otherCallsign))
//                            interlocutors[otherCallsign] = 0;
//                        interlocutors[otherCallsign]++;
//                    }
//                }
//            }

//            return interlocutors
//                .OrderByDescending(kv => kv.Value)
//                .Take(10)
//                .Select(kv => kv.Key)
//                .ToList();
//        }

//        private CommunicationStyle AnalyzeCommunicationStyle(string callsign, List<Message> messages)
//        {
//            var style = new CommunicationStyle();
//            var callsignMessages = messages
//                .Where(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign))
//                .ToList();

//            if (!callsignMessages.Any())
//                return style;

//            // Средняя длина сообщения
//            style.AverageMessageLength = callsignMessages.Average(m => m.Dialog.Length);

//            // Анализ типов сообщений
//            var allText = string.Join(" ", callsignMessages.Select(m => m.Dialog.ToLower()));

//            style.QuestionRatio = CountOccurrences(allText, new[] { "?", "почему", "когда", "где", "как" }) / (double)callsignMessages.Count;
//            style.CommandRatio = CountOccurrences(allText, new[] { "приказываю", "выполнить", "сделать", "немедленно" }) / (double)callsignMessages.Count;
//            style.ReportRatio = CountOccurrences(allText, new[] { "докладываю", "отчет", "наблюдаю", "вижу" }) / (double)callsignMessages.Count;

//            // Характерные фразы
//            var words = callsignMessages
//                .SelectMany(m => m.Dialog.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries))
//                .GroupBy(w => w)
//                .Where(g => g.Count() >= 3)
//                .OrderByDescending(g => g.Count())
//                .Take(5)
//                .Select(g => g.Key)
//                .ToList();

//            style.CharacteristicPhrases = words;

//            return style;
//        }

//        private async Task<RoleAnalysis> AnalyzeCallsignRoleAsync(string callsign, List<Message> messages)
//        {
//            var roleAnalysis = new RoleAnalysis();

//            // Упрощенный анализ роли
//            var allText = string.Join(" ", messages.Select(m => m.Dialog.ToLower()));

//            var roleIndicators = new Dictionary<string, List<string>>
//            {
//                ["Командир"] = new List<string> { "приказ", "команду", "выполнить", "атаковать", "отступать" },
//                ["Координатор"] = new List<string> { "координирую", "согласую", "организую", "взаимодействие", "совместно" },
//                ["Наблюдатель"] = new List<string> { "докладываю", "наблюдаю", "вижу", "замечаю", "отчет" },
//                ["Техник"] = new List<string> { "техника", "ремонт", "обслуживание", "неисправность", "работает" }
//            };

//            var scores = new Dictionary<string, double>();
//            foreach (var role in roleIndicators.Keys)
//            {
//                var score = roleIndicators[role].Sum(indicator => CountOccurrences(allText, new[] { indicator }));
//                scores[role] = score;
//                roleAnalysis.RoleProbabilities[role] = score;
//            }

//            var totalScore = scores.Values.Sum();
//            if (totalScore > 0)
//            {
//                foreach (var role in scores.Keys)
//                {
//                    roleAnalysis.RoleProbabilities[role] = scores[role] / totalScore;
//                }

//                roleAnalysis.PrimaryRole = scores.OrderByDescending(kv => kv.Value).First().Key;
//                roleAnalysis.RoleConfidence = scores[roleAnalysis.PrimaryRole] / totalScore;
//            }
//            else
//            {
//                roleAnalysis.PrimaryRole = "Неизвестно";
//                roleAnalysis.RoleConfidence = 0;
//            }

//            // Индикаторы роли
//            roleAnalysis.RoleIndicators = roleIndicators[roleAnalysis.PrimaryRole]
//                .Where(indicator => allText.Contains(indicator))
//                .Take(3)
//                .ToList();

//            return roleAnalysis;
//        }

//        private async Task<List<KeyInteraction>> ExtractKeyInteractionsAsync(string callsign, List<Message> messages)
//        {
//            var interactions = new Dictionary<string, KeyInteraction>();

//            // Анализируем взаимодействия с другими позывными
//            foreach (var message in messages)
//            {
//                var messageCallsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
//                if (messageCallsigns.Contains(callsign))
//                {
//                    foreach (var otherCallsign in messageCallsigns.Where(c => c != callsign))
//                    {
//                        if (!interactions.ContainsKey(otherCallsign))
//                        {
//                            interactions[otherCallsign] = new KeyInteraction
//                            {
//                                WithCallsign = otherCallsign,
//                                InteractionCount = 0,
//                                FirstInteraction = message.DateTime,
//                                LastInteraction = message.DateTime
//                            };
//                        }

//                        var interaction = interactions[otherCallsign];
//                        interaction.InteractionCount++;

//                        if (message.DateTime < interaction.FirstInteraction)
//                            interaction.FirstInteraction = message.DateTime;

//                        if (message.DateTime > interaction.LastInteraction)
//                            interaction.LastInteraction = message.DateTime;
//                    }
//                }
//            }

//            // Определяем паттерн взаимодействия
//            foreach (var interaction in interactions.Values)
//            {
//                var daysDiff = (interaction.LastInteraction - interaction.FirstInteraction).TotalDays;

//                if (interaction.InteractionCount >= 10 && daysDiff <= 7)
//                    interaction.Pattern = "intense";
//                else if (interaction.InteractionCount >= 5)
//                    interaction.Pattern = "frequent";
//                else if (daysDiff <= 3)
//                    interaction.Pattern = "recent";
//                else
//                    interaction.Pattern = "occasional";

//                // Сила взаимодействия (нормированная)
//                interaction.Strength = Math.Min(1.0, interaction.InteractionCount / 20.0);
//            }

//            return interactions.Values
//                .OrderByDescending(i => i.Strength)
//                .Take(10)
//                .ToList();
//        }

//        private async Task<List<PatternParticipation>> AnalyzePatternInvolvementAsync(string callsign, List<Message> messages)
//        {
//            var participations = new List<PatternParticipation>();

//            // Упрощенный анализ участия в паттернах
//            // В реальном приложении здесь был бы сложный анализ

//            // Утренняя активность
//            var morningMessages = messages.Where(m => m.DateTime.Hour >= 6 && m.DateTime.Hour <= 9).ToList();
//            if (morningMessages.Count >= 3)
//            {
//                participations.Add(new PatternParticipation
//                {
//                    PatternType = "Утренняя активность",
//                    ParticipationCount = morningMessages.Count,
//                    RoleInPattern = "Активный участник",
//                    Frequency = (double)morningMessages.Count / messages.Count
//                });
//            }

//            // Вечерняя активность
//            var eveningMessages = messages.Where(m => m.DateTime.Hour >= 18 && m.DateTime.Hour <= 22).ToList();
//            if (eveningMessages.Count >= 3)
//            {
//                participations.Add(new PatternParticipation
//                {
//                    PatternType = "Вечерняя активность",
//                    ParticipationCount = eveningMessages.Count,
//                    RoleInPattern = "Активный участник",
//                    Frequency = (double)eveningMessages.Count / messages.Count
//                });
//            }

//            return participations;
//        }

//        private async Task<List<AlertInvolvement>> GetAlertInvolvementAsync(string callsign, DateTime startDate, DateTime endDate)
//        {
//            var involvement = new List<AlertInvolvement>();

//            // Получаем алерты за период
//            var alerts = await _alertService.GetAlertsAsync(startDate, endDate);

//            // Фильтруем алерты, связанные с позывным
//            var callsignAlerts = alerts
//                .Where(a => a.RelatedCallsigns.Contains(callsign))
//                .ToList();

//            // Группируем по типам алертов
//            var alertGroups = callsignAlerts
//                .GroupBy(a => a.Rule?.Name ?? "Неизвестный тип")
//                .ToList();

//            foreach (var group in alertGroups)
//            {
//                involvement.Add(new AlertInvolvement
//                {
//                    AlertType = group.Key,
//                    Count = group.Count(),
//                    LastInvolvement = group.Max(a => a.DetectedAt),
//                    Severity = group.Max(a => a.Severity).ToString()
//                });
//            }

//            return involvement
//                .OrderByDescending(i => i.Count)
//                .ThenByDescending(i => i.LastInvolvement)
//                .ToList();
//        }

//        private async Task<List<BehavioralChange>> DetectBehavioralChangesAsync(string callsign, List<Message> messages)
//        {
//            var changes = new List<BehavioralChange>();

//            if (messages.Count < 10)
//                return changes; // Недостаточно данных

//            // Разделяем сообщения на две части для сравнения
//            var sortedMessages = messages.OrderBy(m => m.DateTime).ToList();
//            var half = sortedMessages.Count / 2;

//            var firstHalf = sortedMessages.Take(half).ToList();
//            var secondHalf = sortedMessages.Skip(half).ToList();

//            // Изменение активности
//            var firstActivity = firstHalf.Count;
//            var secondActivity = secondHalf.Count;
//            var activityChange = (double)(secondActivity - firstActivity) / firstActivity;

//            if (Math.Abs(activityChange) > 0.5) // Изменение более чем на 50%
//            {
//                changes.Add(new BehavioralChange
//                {
//                    ChangeDate = secondHalf.First().DateTime,
//                    ChangeType = activityChange > 0 ? "activity_increase" : "activity_decrease",
//                    Description = activityChange > 0
//                        ? $"Увеличение активности на {activityChange:P0}"
//                        : $"Снижение активности на {Math.Abs(activityChange):P0}",
//                    Magnitude = Math.Abs(activityChange),
//                    PossibleReasons = new List<string>
//                    {
//                        "Изменение оперативной обстановки",
//                        "Смена роли/задач позывного",
//                        "Технические проблемы"
//                    }
//                });
//            }

//            // Изменение зон активности
//            var firstAreas = firstHalf.Select(m => m.Area.Name).Distinct().ToList();
//            var secondAreas = secondHalf.Select(m => m.Area.Name).Distinct().ToList();

//            var newAreas = secondAreas.Except(firstAreas).ToList();
//            var lostAreas = firstAreas.Except(secondAreas).ToList();

//            if (newAreas.Any() || lostAreas.Any())
//            {
//                changes.Add(new BehavioralChange
//                {
//                    ChangeDate = secondHalf.First().DateTime,
//                    ChangeType = "area_change",
//                    Description = $"Изменение зон активности: новые ({string.Join(", ", newAreas)}), утраченные ({string.Join(", ", lostAreas)})",
//                    Magnitude = (newAreas.Count + lostAreas.Count) / (double)(firstAreas.Count + secondAreas.Count),
//                    PossibleReasons = new List<string>
//                    {
//                        "Передислокация подразделения",
//                        "Изменение зоны ответственности",
//                        "Тактические перемещения"
//                    }
//                });
//            }

//            return changes;
//        }

//        private List<Recommendation> GenerateRecommendations(CallsignDossier dossier)
//        {
//            var recommendations = new List<Recommendation>();

//            // Рекомендации на основе данных досье
//            if (dossier.Alerts.Any(a => a.Severity == "Critical" || a.Severity == "High"))
//            {
//                recommendations.Add(new Recommendation
//                {
//                    Type = "monitoring",
//                    Description = "Усилить мониторинг позывного из-за участия в критических алертах",
//                    Priority = "high",
//                    Actions = new List<string>
//                    {
//                        "Установить повышенный приоритет наблюдения",
//                        "Анализировать все взаимодействия позывного",
//                        "Настроить дополнительные алерты"
//                    }
//                });
//            }

//            if (dossier.BehavioralChanges.Any())
//            {
//                recommendations.Add(new Recommendation
//                {
//                    Type = "investigation",
//                    Description = "Исследовать изменения в поведении позывного",
//                    Priority = "medium",
//                    Actions = new List<string>
//                    {
//                        "Проанализировать причины изменения активности",
//                        "Сравнить с поведением связанных позывных",
//                        "Проверить связь с оперативными событиями"
//                    }
//                });
//            }

//            if (dossier.FrequentInterlocutors.Count >= 5)
//            {
//                recommendations.Add(new Recommendation
//                {
//                    Type = "analysis",
//                    Description = "Провести анализ сети взаимодействий позывного",
//                    Priority = "low",
//                    Actions = new List<string>
//                    {
//                        "Построить граф связей позывного",
//                        "Выявить ключевые точки взаимодействия",
//                        "Проанализировать паттерны коммуникации"
//                    }
//                });
//            }

//            return recommendations;
//        }

//        private int CalculateInteractionCountInArea(string callsign, string area, List<Message> messages)
//        {
//            var interactions = new HashSet<string>();

//            foreach (var message in messages.Where(m => m.Area.Name == area))
//            {
//                var messageCallsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
//                if (messageCallsigns.Contains(callsign))
//                {
//                    foreach (var otherCallsign in messageCallsigns.Where(c => c != callsign))
//                    {
//                        interactions.Add(otherCallsign);
//                    }
//                }
//            }

//            return interactions.Count;
//        }

//        private TimeSpan CalculateAverageResponseTimeInArea(string callsign, string area, List<Message> messages)
//        {
//            var areaMessages = messages
//                .Where(m => m.Area.Name == area && m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign))
//                .OrderBy(m => m.DateTime)
//                .ToList();

//            var responseTimes = new List<TimeSpan>();

//            for (int i = 1; i < areaMessages.Count; i++)
//            {
//                var timeDiff = areaMessages[i].DateTime - areaMessages[i - 1].DateTime;
//                if (timeDiff.TotalHours < 1)
//                {
//                    responseTimes.Add(timeDiff);
//                }
//            }

//            return responseTimes.Any()
//                ? TimeSpan.FromSeconds(responseTimes.Average(ts => ts.TotalSeconds))
//                : TimeSpan.Zero;
//        }

//        private string DetermineCallsignRoleInArea(string callsign, string area, List<Message> messages)
//        {
//            var areaMessages = messages
//                .Where(m => m.Area.Name == area && m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign))
//                .ToList();

//            if (!areaMessages.Any())
//                return "Неизвестно";

//            var allText = string.Join(" ", areaMessages.Select(m => m.Dialog.ToLower()));

//            if (allText.Contains("приказ") || allText.Contains("команду"))
//                return "Командир";

//            if (allText.Contains("докладываю") || allText.Contains("наблюдаю"))
//                return "Наблюдатель";

//            if (allText.Contains("координирую") || allText.Contains("организую"))
//                return "Координатор";

//            return "Исполнитель";
//        }

//        private ActivityTimeline BuildActivityTimeline(List<Message> messages, DateTime startDate, DateTime endDate)
//        {
//            var timeline = new ActivityTimeline();

//            // Почасовые данные
//            for (int hour = 0; hour < 24; hour++)
//            {
//                var hourMessages = messages.Where(m => m.DateTime.Hour == hour).ToList();

//                timeline.HourlyData.Add(new HourlyActivity
//                {
//                    Hour = hour,
//                    MessageCount = hourMessages.Count,
//                    CallsignCount = hourMessages
//                        .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                        .Distinct()
//                        .Count(),
//                    ActivityLevel = hourMessages.Count > 0
//                        ? (double)hourMessages.Count / messages.Count
//                        : 0
//                });
//            }

//            // Пиковое время
//            var peakHour = timeline.HourlyData
//                .OrderByDescending(h => h.MessageCount)
//                .FirstOrDefault();

//            if (peakHour != null)
//            {
//                timeline.PeakTime = startDate.AddHours(peakHour.Hour);
//            }

//            // Средняя активность
//            timeline.AverageActivity = messages.Count > 0
//                ? messages.Count / (endDate - startDate).TotalHours
//                : 0;

//            // Ежедневные данные (если период больше одного дня)
//            if ((endDate - startDate).TotalDays > 1)
//            {
//                var currentDate = startDate.Date;
//                while (currentDate <= endDate.Date)
//                {
//                    var nextDate = currentDate.AddDays(1);
//                    var dayMessages = messages.Where(m => m.DateTime >= currentDate && m.DateTime < nextDate).ToList();

//                    timeline.DailyData.Add(new DailyActivity
//                    {
//                        Date = currentDate,
//                        MessageCount = dayMessages.Count,
//                        CallsignCount = dayMessages
//                            .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                            .Distinct()
//                            .Count(),
//                        Trend = CalculateDailyTrend(dayMessages, messages)
//                    });

//                    currentDate = nextDate;
//                }
//            }

//            return timeline;
//        }

//        private async Task<List<CommunicationPattern>> DetectCommunicationPatternsInAreaAsync(string area, List<Message> messages)
//        {
//            var patterns = new List<CommunicationPattern>();

//            // Упрощенное обнаружение паттернов
//            // В реальном приложении здесь был бы ML анализ

//            // Паттерн "Координация"
//            var coordinationMessages = messages
//                .Where(m => m.Dialog.ToLower().Contains("координирую") ||
//                           m.Dialog.ToLower().Contains("согласую") ||
//                           m.Dialog.ToLower().Contains("взаимодействие"))
//                .ToList();

//            if (coordinationMessages.Count >= 3)
//            {
//                patterns.Add(new CommunicationPattern
//                {
//                    PatternType = "Координация действий",
//                    Description = "Сообщения, связанные с координацией действий",
//                    ExampleFlows = coordinationMessages.Take(3).Select(m => Truncate(m.Dialog, 50)).ToList(),
//                    Frequency = (double)coordinationMessages.Count / messages.Count,
//                    Confidence = 0.7,
//                    CharacteristicCallsigns = coordinationMessages
//                        .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                        .Distinct()
//                        .Take(5)
//                        .ToList(),
//                    TypicalDuration = TimeSpan.FromMinutes(30)
//                });
//            }

//            // Паттерн "Доклады"
//            var reportMessages = messages
//                .Where(m => m.Dialog.ToLower().Contains("докладываю") ||
//                           m.Dialog.ToLower().Contains("отчет") ||
//                           m.Dialog.ToLower().Contains("наблюдаю"))
//                .ToList();

//            if (reportMessages.Count >= 3)
//            {
//                patterns.Add(new CommunicationPattern
//                {
//                    PatternType = "Оперативные доклады",
//                    Description = "Регулярные доклады о ситуации",
//                    ExampleFlows = reportMessages.Take(3).Select(m => Truncate(m.Dialog, 50)).ToList(),
//                    Frequency = (double)reportMessages.Count / messages.Count,
//                    Confidence = 0.8,
//                    CharacteristicCallsigns = reportMessages
//                        .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                        .Distinct()
//                        .Take(5)
//                        .ToList(),
//                    TypicalDuration = TimeSpan.FromMinutes(10)
//                });
//            }

//            return patterns;
//        }

//        private async Task<List<AlertSummary>> GetAreaAlertsAsync(string area, DateTime startDate, DateTime endDate)
//        {
//            var alerts = await _alertService.GetAlertsAsync(startDate, endDate);

//            // Фильтруем алерты, связанные с зоной
//            var areaAlerts = alerts
//                .Where(a => a.RelatedAreas.Contains(area))
//                .ToList();

//            return areaAlerts
//                .GroupBy(a => a.Rule?.Name ?? "Неизвестное правило")
//                .Select(g => new AlertSummary
//                {
//                    RuleName = g.Key,
//                    Count = g.Count(),
//                    HighestSeverity = g.Max(a => a.Severity),
//                    AffectedCallsigns = g.SelectMany(a => a.RelatedCallsigns).Distinct().ToList(),
//                    FirstAlert = g.Min(a => a.DetectedAt),
//                    LastAlert = g.Max(a => a.DetectedAt)
//                })
//                .ToList();
//        }

//        private AreaMetrics CalculateAreaMetrics(string area, List<Message> messages)
//        {
//            var metrics = new AreaMetrics();

//            if (!messages.Any())
//                return metrics;

//            // Плотность активности (сообщения в час)
//            var totalHours = (messages.Max(m => m.DateTime) - messages.Min(m => m.DateTime)).TotalHours;
//            metrics.ActivityDensity = totalHours > 0 ? messages.Count / totalHours : 0;

//            // Оборот позывных (сколько уникальных позывных в единицу времени)
//            var uniqueCallsigns = messages
//                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                .Distinct()
//                .Count();

//            metrics.CallsignTurnover = totalHours > 0 ? uniqueCallsigns / totalHours : 0;

//            // Богатство паттернов (упрощенно)
//            metrics.PatternRichness = CalculatePatternRichness(messages);

//            // Частота алертов
//            metrics.AlertFrequency = CalculateAlertFrequency(area, messages);

//            return metrics;
//        }

//        private async Task<List<KeyObservation>> ExtractAreaObservationsAsync(string area, List<Message> messages)
//        {
//            var observations = new List<KeyObservation>();

//            if (!messages.Any())
//                return observations;

//            // Высокая концентрация активности
//            var peakHour = messages
//                .GroupBy(m => m.DateTime.Hour)
//                .OrderByDescending(g => g.Count())
//                .FirstOrDefault();

//            if (peakHour != null && peakHour.Count() >= 10)
//            {
//                observations.Add(new KeyObservation
//                {
//                    Type = "high_activity",
//                    Description = $"Пик активности в {peakHour.Key}:00 ({peakHour.Count()} сообщений)",
//                    Impact = "medium",
//                    ObservedAt = messages.First().DateTime,
//                    RelatedCallsigns = peakHour
//                        .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                        .Distinct()
//                        .Take(5)
//                        .ToList(),
//                    Recommendations = new List<string>
//                    {
//                        "Усилить мониторинг в пиковые часы",
//                        "Проанализировать причины концентрации активности"
//                    }
//                });
//            }

//            // Разнообразие позывных
//            var uniqueCallsigns = messages
//                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                .Distinct()
//                .Count();

//            if (uniqueCallsigns >= 10)
//            {
//                observations.Add(new KeyObservation
//                {
//                    Type = "diverse_activity",
//                    Description = $"Высокое разнообразие позывных: {uniqueCallsigns} уникальных позывных",
//                    Impact = "low",
//                    ObservedAt = messages.First().DateTime,
//                    RelatedCallsigns = messages
//                        .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
//                        .Distinct()
//                        .Take(5)
//                        .ToList(),
//                    Recommendations = new List<string>
//                    {
//                        "Проанализировать роли различных позывных",
//                        "Выявить ключевых участников коммуникации"
//                    }
//                });
//            }

//            return observations;
//        }

//        private List<Recommendation> GenerateAreaRecommendations(AreaActivityReport report)
//        {
//            var recommendations = new List<Recommendation>();

//            // Рекомендации на основе метрик
//            if (report.Metrics.AlertFrequency > 0.5) // Более 0.5 алертов в час
//            {
//                recommendations.Add(new Recommendation
//                {
//                    Type = "monitoring",
//                    Description = "Усилить мониторинг зоны из-за высокой частоты алертов",
//                    Priority = "high",
//                    Actions = new List<string>
//                    {
//                        "Настроить дополнительные алерты для зоны",
//                        "Увеличить частоту проверок",
//                        "Вести детальный журнал событий"
//                    }
//                });
//            }

//            if (report.AreaAlerts.Any(a => a.HighestSeverity == AlertSeverity.Critical))
//            {
//                recommendations.Add(new Recommendation
//                {
//                    Type = "investigation",
//                    Description = "Срочно исследовать критические алерты в зоне",
//                    Priority = "high",
//                    Actions = new List<string>
//                    {
//                        "Немедленно провести анализ причин",
//                        "Подготовить специальный отчет",
//                        "Уведомить ответственных лиц"
//                    }
//                });
//            }

//            if (report.CommonPatterns.Count >= 3)
//            {
//                recommendations.Add(new Recommendation
//                {
//                    Type = "analysis",
//                    Description = "Провести углубленный анализ паттернов коммуникации",
//                    Priority = "medium",
//                    Actions = new List<string>
//                    {
//                        "Изучить временные закономерности",
//                        "Проанализировать роли участников",
//                        "Выявить аномальные паттерны"
//                    }
//                });
//            }

//            return recommendations;
//        }

//        private double CalculateDailyTrend(List<Message> dayMessages, List<Message> allMessages)
//        {
//            if (!dayMessages.Any() || !allMessages.Any())
//                return 0;

//            // Упрощенный расчет тренда
//            var dayCount = dayMessages.Count;
//            var avgCount = allMessages.Count / (double)allMessages
//                .GroupBy(m => m.DateTime.Date)
//                .Count();

//            return avgCount > 0 ? (dayCount - avgCount) / avgCount : 0;
//        }

//        private double CalculatePatternRichness(List<Message> messages)
//        {
//            // Упрощенный расчет богатства паттернов
//            // В реальном приложении здесь был бы сложный анализ

//            var uniqueCallsignPairs = new HashSet<string>();

//            foreach (var message in messages)
//            {
//                var callsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
//                for (int i = 0; i < callsigns.Count; i++)
//                {
//                    for (int j = i + 1; j < callsigns.Count; j++)
//                    {
//                        var pair = $"{callsigns[i]}-{callsigns[j]}";
//                        uniqueCallsignPairs.Add(pair);
//                    }
//                }
//            }

//            var maxPossiblePairs = messages.Count * 2; // Упрощенная оценка
//            return maxPossiblePairs > 0 ? (double)uniqueCallsignPairs.Count / maxPossiblePairs : 0;
//        }

//        private double CalculateAlertFrequency(string area, List<Message> messages)
//        {
//            // Упрощенный расчет частоты алертов
//            // В реальном приложении здесь был бы запрос к сервису алертов

//            return 0.1; // Заглушка
//        }

//        private int CountOccurrences(string text, string[] keywords)
//        {
//            return keywords.Sum(keyword =>
//                text.Split(new[] { keyword }, StringSplitOptions.None).Length - 1);
//        }

//        private string Truncate(string text, int maxLength)
//        {
//            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
//                return text;

//            return text.Substring(0, maxLength) + "...";
//        }
//    }
//}