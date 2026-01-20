using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Charts;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioIntercepts.WpfApp.Services
{
    public class ChartService : IChartService
    {
        private readonly AppDbContext _context;

        public ChartService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ChartData> GetCallsignActivityByDayOfWeekAsync(string callsign)
        {
            var messages = await _context.MessageCallsigns
                .Include(mc => mc.Message)
                .Where(mc => mc.Callsign.Name == callsign)
                .Select(mc => mc.Message)
                .ToListAsync();

            var data = messages
                .GroupBy(m => m.DateTime.DayOfWeek)
                .Select(g => new
                {
                    DayOfWeek = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.DayOfWeek)
                .ToList();

            var chart = new ChartData
            {
                Title = "Активность по дням недели",
                Labels = new List<string> { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" }
            };

            var values = new List<double>();
            for (int i = 0; i < 7; i++)
            {
                var dayData = data.FirstOrDefault(d => (int)d.DayOfWeek == i);
                values.Add(dayData?.Count ?? 0);
            }

            var columnSeries = new ColumnSeries
            {
                Title = "Количество сообщений",
                Values = new LiveCharts.ChartValues<double>(values)
            };

            chart.SeriesCollection.Add(columnSeries);
            return chart;
        }

        public async Task<ChartData> GetCallsignActivityByHourAsync(string callsign)
        {
            var messages = await _context.MessageCallsigns
                .Include(mc => mc.Message)
                .Where(mc => mc.Callsign.Name == callsign)
                .Select(mc => mc.Message)
                .ToListAsync();

            var data = messages
                .GroupBy(m => m.DateTime.Hour)
                .Select(g => new
                {
                    Hour = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Hour)
                .ToList();

            var chart = new ChartData
            {
                Title = "Активность по времени суток",
                Labels = Enumerable.Range(0, 24).Select(h => $"{h:00}:00").ToList()
            };

            var values = new List<double>();
            for (int hour = 0; hour < 24; hour++)
            {
                var hourData = data.FirstOrDefault(d => d.Hour == hour);
                values.Add(hourData?.Count ?? 0);
            }

            var columnSeries = new ColumnSeries
            {
                Title = "Количество сообщений",
                Values = new LiveCharts.ChartValues<double>(values)
            };

            chart.SeriesCollection.Add(columnSeries);
            return chart;
        }

        public async Task<List<ChartPoint>> GetTopFrequenciesForCallsignAsync(string callsign, int topCount = 10)
        {
            return await _context.MessageCallsigns
                .Include(mc => mc.Message)
                    .ThenInclude(m => m.Frequency)
                .Where(mc => mc.Callsign.Name == callsign)
                .GroupBy(mc => mc.Message.Frequency.Value)
                .Select(g => new ChartPoint
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(x => x.Value)
                .Take(topCount)
                .ToListAsync();
        }

        public async Task<ChartData> GetActivityByDayOfWeekAsync(
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string area = null,
            string frequency = null)
        {
            var query = _context.Messages.AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(m => m.DateTime >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(m => m.DateTime <= dateTo.Value);

            if (!string.IsNullOrEmpty(area))
                query = query.Where(m => m.Area.Name.Contains(area));

            if (!string.IsNullOrEmpty(frequency))
                query = query.Where(m => m.Frequency.Value.Contains(frequency));

            var messages = await query.ToListAsync();

            var data = messages
                .GroupBy(m => m.DateTime.DayOfWeek)
                .Select(g => new
                {
                    DayOfWeek = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.DayOfWeek)
                .ToList();

            var chart = new ChartData
            {
                Title = "Активность перехватов по дням недели",
                Labels = new List<string> { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" }
            };

            var values = new List<double>();
            for (int i = 0; i < 7; i++)
            {
                var dayData = data.FirstOrDefault(d => (int)d.DayOfWeek == i);
                values.Add(dayData?.Count ?? 0);
            }

            var columnSeries = new ColumnSeries
            {
                Title = "Количество сообщений",
                Values = new LiveCharts.ChartValues<double>(values)
            };

            chart.SeriesCollection.Add(columnSeries);
            return chart;
        }

        public async Task<ChartData> GetActivityByHourAsync(
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string area = null,
            string frequency = null)
        {
            var query = _context.Messages.AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(m => m.DateTime >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(m => m.DateTime <= dateTo.Value);

            if (!string.IsNullOrEmpty(area))
                query = query.Where(m => m.Area.Name.Contains(area));

            if (!string.IsNullOrEmpty(frequency))
                query = query.Where(m => m.Frequency.Value.Contains(frequency));

            var messages = await query.ToListAsync();

            var data = messages
                .GroupBy(m => m.DateTime.Hour)
                .Select(g => new
                {
                    Hour = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Hour)
                .ToList();

            var chart = new ChartData
            {
                Title = "Активность перехватов по времени суток",
                Labels = Enumerable.Range(0, 24).Select(h => $"{h:00}:00").ToList()
            };

            var values = new List<double>();
            for (int hour = 0; hour < 24; hour++)
            {
                var hourData = data.FirstOrDefault(d => d.Hour == hour);
                values.Add(hourData?.Count ?? 0);
            }

            var columnSeries = new ColumnSeries
            {
                Title = "Количество сообщений",
                Values = new LiveCharts.ChartValues<double>(values)
            };

            chart.SeriesCollection.Add(columnSeries);
            return chart;
        }

        public async Task<int> GetTotalMessagesCountAsync(
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string area = null,
            string frequency = null)
        {
            var query = _context.Messages.AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(m => m.DateTime >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(m => m.DateTime <= dateTo.Value);

            if (!string.IsNullOrEmpty(area))
                query = query.Where(m => m.Area.Name.Contains(area));

            if (!string.IsNullOrEmpty(frequency))
                query = query.Where(m => m.Frequency.Value.Contains(frequency));

            return await query.CountAsync();
        }
    }
}