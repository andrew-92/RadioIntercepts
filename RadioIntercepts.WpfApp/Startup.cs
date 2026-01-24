using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RadioIntercepts.Application.Parsers;
using RadioIntercepts.Application.Services;
using RadioIntercepts.Analysis.Interfaces.Services;
using RadioIntercepts.Core.Interfaces;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.Infrastructure.Repositories;
using RadioIntercepts.WpfApp.ViewModels;
using RadioIntercepts.WpfApp.Views;
using System.IO;
using RadioIntercepts.Analysis.Services;
using RadioIntercepts.Analysis.Services.SemanticSearch;
using RadioIntercepts.Analysis.Services.Graphs;
using RadioIntercepts.Analysis.Services.Reports;
using RadioIntercepts.Application.Parsers.Source;

namespace RadioIntercepts.WpfApp
{
    public static class Startup
    {
        public static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Чтение appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // Чтение строки подключения
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString),
                ServiceLifetime.Transient);

            services.AddDbContext<AlertDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("AlertDatabase")));

            // Регистрируем репозитории
            services.AddScoped<IMessageRepository, MessageRepository>();

            // Регистрируем Windows
            services.AddTransient<MainWindow>();
            services.AddTransient<MessageEditWindow>();
            services.AddTransient<MessageParserWindow>();
            services.AddTransient<DashboardWindow>();
            services.AddTransient<ReportsWindow>();
            services.AddTransient<MessagesWindow>();
            services.AddTransient<CallsignStatisticsViewModel>();
            services.AddTransient<CallsignStatisticsWindow>();
            services.AddScoped<IChartService, ChartService>();
            services.AddTransient<CallsignAnalysisWindow>();
            services.AddTransient<CallsignAnalysisViewModel>();
            services.AddTransient<StatisticsWindow>();
            services.AddTransient<StatisticsViewModel>();

            services.AddScoped<IGraphAnalysisService, GraphAnalysisService>();
            services.AddScoped<IAdvancedGraphAnalysisService, AdvancedGraphAnalysisService>();
            services.AddScoped<ICacheService, MemoryCacheService>(); // Реализуйте кэш

            services.AddSingleton<ICacheService, AdvancedMemoryCacheService>();
            services.AddScoped<IGraphAnalysisService, GraphAnalysisService>();
            services.AddScoped<IAdvancedGraphAnalysisService, AdvancedGraphAnalysisService>();
            services.AddScoped<GraphViewModel>();
            services.AddTransient<GraphWindow>();


            // services.AddScoped<ICommunicationFlowService, CommunicationFlowService>();
            //services.AddScoped<IAlertService, AlertService>();
            services.AddScoped<ICodeAnalysisService, CodeAnalysisService>();
            //services.AddScoped<ISemanticSearchService, SemanticSearchService>();
            //services.AddScoped<ITemporalAnalysisService, TemporalAnalysisService>();
            services.AddScoped<IDialogPatternAnalyzer, DialogPatternAnalyzer>();
            services.AddScoped<IGraphAnalysisService, GraphAnalysisService>();
            services.AddTransient<IMessageParser, WhatsappRadioMessageParser>();
            services.AddTransient<IRadioMessageSource, WebServiceRadioMessageSource>();
            services.AddScoped<IMessageProcessingService, MessageProcessingService>();
            services.AddTransient<IRadioMessageSource, FileRadioMessageSource>();
            services.AddTransient<IRadioMessageSource, ManualRadioMessageSource>();

            // Регистрация нового сервиса отчетов
            //services.AddTransient<IReportService, ReportService>();

            // Регистрация ViewModel'ов отчетов
            //services.AddTransient<ReportViewModel>();
            //services.AddTransient<ReportStatisticsViewModel>();
            //services.AddTransient<DailySummaryViewModel>();
            //services.AddTransient<CallsignDossierViewModel>();
            //services.AddTransient<AreaAnalysisViewModel>();
            //services.AddTransient<GenerateReportViewModel>();
            //services.AddHttpClient(); // Для WebServiceRadioMessageSourserce

            // Регистрируем ViewModels, если будут
            // services.AddTransient<MainWindowViewModel>();
            services.AddSingleton(sp => sp);

            return services.BuildServiceProvider();
        }
    }
}
