// Application/Services/CommunicationFlowService.cs
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.WpfApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioIntercepts.WpfApp.Services
{
    public interface ICommunicationFlowService
    {
        // Sankey-диаграммы потоков
        Task<CommunicationFlow> BuildSankeyDiagramAsync(DateTime startTime, DateTime endTime,
            string? area = null, string? frequency = null, int maxNodes = 50);
        Task<CommunicationFlow> BuildCallsignFlowAsync(string callsign, DateTime startTime, DateTime endTime);
        Task<CommunicationFlow> BuildAreaFlowAsync(string area, DateTime startTime, DateTime endTime);

        // Timeline визуализации
        Task<ParallelTimeline> BuildParallelTimelineAsync(DateTime startTime, DateTime endTime,
            List<string>? callsigns = null, List<string>? areas = null);
        Task<ParallelTimeline> BuildCallsignTimelineAsync(string callsign, DateTime startTime, DateTime endTime);
        Task<ParallelTimeline> BuildConversationTimelineAsync(long startMessageId, int maxMessages = 20);

        // Анализ паттернов
        Task<List<CommunicationPattern>> DetectCommunicationPatternsAsync(DateTime startTime, DateTime endTime);
        Task<List<string>> FindCommonFlowsAsync(int minOccurrences = 3);
        Task<Dictionary<string, double>> CalculateFlowMetricsAsync(DateTime startTime, DateTime endTime);

        // Визуализация групп
        Task<CommunicationFlow> BuildGroupCommunicationFlowAsync(string groupName, DateTime startTime, DateTime endTime);
        Task<List<CommunicationFlow>> CompareTimePeriodsAsync(DateTime period1Start, DateTime period1End,
            DateTime period2Start, DateTime period2End);

        // Экспорт данных для визуализации
        Task<string> ExportFlowDataAsync(CommunicationFlow flow, string format = "json");
        Task<string> ExportTimelineDataAsync(ParallelTimeline timeline, string format = "json");
    }

    public class CommunicationFlowService : ICommunicationFlowService
    {
        private readonly AppDbContext _context;
        private readonly IGraphAnalysisService _graphService;
        private readonly IDialogPatternAnalyzer _dialogAnalyzer;

        // Цвета для визуализации
        private static readonly Dictionary<string, string> _nodeColors = new()
        {
            ["Callsign"] = "#2196F3", // Blue
            ["Area"] = "#4CAF50",     // Green
            ["Frequency"] = "#FF9800", // Orange
            ["TimeSlot"] = "#9C27B0",  // Purple
            ["Cluster"] = "#F44336"    // Red
        };

        // Цвета для типов событий
        private static readonly Dictionary<string, string> _eventColors = new()
        {
            ["Message"] = "#2196F3",
            ["Command"] = "#F44336",
            ["Request"] = "#FF9800",
            ["Report"] = "#4CAF50",
            ["Confirmation"] = "#9C27B0",
            ["Query"] = "#00BCD4",
            ["Technical"] = "#795548",
            ["Greeting"] = "#E91E63",
            ["Farewell"] = "#607D8B"
        };

        public CommunicationFlowService(
            AppDbContext context,
            IGraphAnalysisService graphService,
            IDialogPatternAnalyzer dialogAnalyzer)
        {
            _context = context;
            _graphService = graphService;
            _dialogAnalyzer = dialogAnalyzer;
        }

        public async Task<CommunicationFlow> BuildSankeyDiagramAsync(
            DateTime startTime, DateTime endTime,
            string? area = null, string? frequency = null, int maxNodes = 50)
        {
            var flow = new CommunicationFlow
            {
                StartTime = startTime,
                EndTime = endTime
            };

            // Получаем сообщения за указанный период
            var query = _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .Include(m => m.Frequency)
                .Where(m => m.DateTime >= startTime && m.DateTime <= endTime)
                .OrderBy(m => m.DateTime);

            if (!string.IsNullOrEmpty(area))
                query = query.Where(m => m.Area.Name == area);
            if (!string.IsNullOrEmpty(frequency))
                query = query.Where(m => m.Frequency.Value == frequency);

            var messages = await query.ToListAsync();

            if (!messages.Any())
                return flow;

            // Создаем узлы (позывные)
            var callsignStats = messages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .GroupBy(c => c)
                .Select(g => new { Callsign = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(maxNodes / 2) // Ограничиваем количество позывных
                .ToList();

            // Создаем узлы для зон (если не фильтруем по зоне)
            var areaStats = messages
                .GroupBy(m => m.Area.Name)
                .Select(g => new { Area = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(maxNodes / 4)
                .ToList();

            // Добавляем узлы позывных
            foreach (var stat in callsignStats)
            {
                flow.Nodes.Add(new FlowNode
                {
                    Id = $"callsign_{stat.Callsign}",
                    Label = stat.Callsign,
                    Type = NodeType.Callsign,
                    Size = stat.Count,
                    Color = _nodeColors["Callsign"],
                    Metadata = new Dictionary<string, object>
                    {
                        ["messageCount"] = stat.Count,
                        ["type"] = "callsign"
                    }
                });
            }

            // Добавляем узлы зон
            foreach (var stat in areaStats)
            {
                flow.Nodes.Add(new FlowNode
                {
                    Id = $"area_{stat.Area}",
                    Label = stat.Area,
                    Type = NodeType.Area,
                    Size = stat.Count,
                    Color = _nodeColors["Area"],
                    Metadata = new Dictionary<string, object>
                    {
                        ["messageCount"] = stat.Count,
                        ["type"] = "area"
                    }
                });
            }

            // Создаем связи между позывными (кто с кем общается)
            var callsignLinks = new Dictionary<string, Dictionary<string, int>>();

            foreach (var message in messages)
            {
                var callsigns = message.MessageCallsigns
                    .Select(mc => mc.Callsign.Name)
                    .Where(c => callsignStats.Select(s => s.Callsign).Contains(c))
                    .ToList();

                // Создаем связи между всеми позывными в сообщении
                for (int i = 0; i < callsigns.Count; i++)
                {
                    for (int j = i + 1; j < callsigns.Count; j++)
                    {
                        var source = callsigns[i];
                        var target = callsigns[j];

                        if (!callsignLinks.ContainsKey(source))
                            callsignLinks[source] = new Dictionary<string, int>();

                        if (!callsignLinks[source].ContainsKey(target))
                            callsignLinks[source][target] = 0;

                        callsignLinks[source][target]++;

                        // Также добавляем обратную связь (для неориентированного графа)
                        if (!callsignLinks.ContainsKey(target))
                            callsignLinks[target] = new Dictionary<string, int>();

                        if (!callsignLinks[target].ContainsKey(source))
                            callsignLinks[target][source] = 0;

                        callsignLinks[target][source]++;
                    }
                }

                // Связи между позывными и зонами
                var area = message.Area.Name;
                if (areaStats.Select(a => a.Area).Contains(area))
                {
                    foreach (var callsign in callsigns)
                    {
                        var linkId = $"callsign_{callsign}_area_{area}";
                        var existingLink = flow.Links.FirstOrDefault(l =>
                            l.SourceId == $"callsign_{callsign}" && l.TargetId == $"area_{area}" ||
                            l.SourceId == $"area_{area}" && l.TargetId == $"callsign_{callsign}");

                        if (existingLink != null)
                        {
                            existingLink.Value++;
                            existingLink.InteractionTimes.Add(message.DateTime);
                        }
                        else
                        {
                            flow.Links.Add(new FlowLink
                            {
                                SourceId = $"callsign_{callsign}",
                                TargetId = $"area_{area}",
                                Value = 1,
                                Color = "#FF9800",
                                InteractionTimes = new List<DateTime> { message.DateTime },
                                Strength = 1.0
                            });
                        }
                    }
                }
            }

            // Добавляем связи между позывными в общий список
            foreach (var source in callsignLinks.Keys)
            {
                foreach (var target in callsignLinks[source].Keys)
                {
                    // Чтобы избежать дублирования (A->B и B->A)
                    if (source.CompareTo(target) < 0)
                    {
                        var totalInteractions = callsignLinks[source][target];

                        flow.Links.Add(new FlowLink
                        {
                            SourceId = $"callsign_{source}",
                            TargetId = $"callsign_{target}",
                            Value = totalInteractions,
                            Color = "#2196F3",
                            Strength = CalculateLinkStrength(totalInteractions, messages.Count),
                            Metadata = new Dictionary<string, object>
                            {
                                ["interactionCount"] = totalInteractions
                            }
                        });
                    }
                }
            }

            // Создаем поток сообщений для детального анализа
            await BuildMessageFlowsAsync(flow, messages);

            // Рассчитываем статистику
            flow.Statistics = await CalculateFlowStatisticsAsync(flow, messages);

            return flow;
        }

        public async Task<CommunicationFlow> BuildCallsignFlowAsync(string callsign, DateTime startTime, DateTime endTime)
        {
            var flow = new CommunicationFlow
            {
                StartTime = startTime,
                EndTime = endTime
            };

            // Получаем все сообщения с участием позывного
            var messages = await _context.MessageCallsigns
                .Include(mc => mc.Message)
                    .ThenInclude(m => m.MessageCallsigns)
                        .ThenInclude(mc => mc.Callsign)
                .Include(mc => mc.Message)
                    .ThenInclude(m => m.Area)
                .Include(mc => mc.Message)
                    .ThenInclude(m => m.Frequency)
                .Where(mc => mc.Callsign.Name == callsign &&
                       mc.Message.DateTime >= startTime &&
                       mc.Message.DateTime <= endTime)
                .Select(mc => mc.Message)
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            if (!messages.Any())
                return flow;

            // Основной узел - позывной
            flow.Nodes.Add(new FlowNode
            {
                Id = $"callsign_{callsign}",
                Label = callsign,
                Type = NodeType.Callsign,
                Size = messages.Count,
                Color = "#2196F3",
                Metadata = new Dictionary<string, object>
                {
                    ["messageCount"] = messages.Count,
                    ["type"] = "primary_callsign"
                }
            });

            // Находим всех собеседников
            var interlocutors = messages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .Where(c => c != callsign)
                .Distinct()
                .ToList();

            // Добавляем узлы собеседников
            foreach (var interlocutor in interlocutors)
            {
                var messagesWithInterlocutor = messages
                    .Count(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == interlocutor));

                flow.Nodes.Add(new FlowNode
                {
                    Id = $"callsign_{interlocutor}",
                    Label = interlocutor,
                    Type = NodeType.Callsign,
                    Size = messagesWithInterlocutor,
                    Color = "#4CAF50",
                    Metadata = new Dictionary<string, object>
                    {
                        ["messageCount"] = messagesWithInterlocutor,
                        ["type"] = "interlocutor"
                    }
                });

                // Добавляем связь
                flow.Links.Add(new FlowLink
                {
                    SourceId = $"callsign_{callsign}",
                    TargetId = $"callsign_{interlocutor}",
                    Value = messagesWithInterlocutor,
                    Color = "#2196F3",
                    Strength = CalculateLinkStrength(messagesWithInterlocutor, messages.Count),
                    Metadata = new Dictionary<string, object>
                    {
                        ["interactionCount"] = messagesWithInterlocutor
                    }
                });
            }

            // Добавляем зоны активности
            var areas = messages
                .GroupBy(m => m.Area.Name)
                .Select(g => new { Area = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            foreach (var area in areas)
            {
                flow.Nodes.Add(new FlowNode
                {
                    Id = $"area_{area.Area}",
                    Label = area.Area,
                    Type = NodeType.Area,
                    Size = area.Count,
                    Color = "#FF9800",
                    Metadata = new Dictionary<string, object>
                    {
                        ["messageCount"] = area.Count,
                        ["type"] = "area"
                    }
                });

                // Связь с зоной
                flow.Links.Add(new FlowLink
                {
                    SourceId = $"callsign_{callsign}",
                    TargetId = $"area_{area.Area}",
                    Value = area.Count,
                    Color = "#FF9800",
                    Strength = CalculateLinkStrength(area.Count, messages.Count)
                });
            }

            // Создаем поток сообщений
            await BuildMessageFlowsAsync(flow, messages);

            // Статистика
            flow.Statistics = await CalculateFlowStatisticsAsync(flow, messages);

            return flow;
        }

        public async Task<CommunicationFlow> BuildAreaFlowAsync(string area, DateTime startTime, DateTime endTime)
        {
            var flow = new CommunicationFlow
            {
                StartTime = startTime,
                EndTime = endTime
            };

            // Получаем все сообщения в указанной зоне
            var messages = await _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .Include(m => m.Frequency)
                .Where(m => m.Area.Name == area &&
                       m.DateTime >= startTime &&
                       m.DateTime <= endTime)
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            if (!messages.Any())
                return flow;

            // Узел зоны
            flow.Nodes.Add(new FlowNode
            {
                Id = $"area_{area}",
                Label = area,
                Type = NodeType.Area,
                Size = messages.Count,
                Color = "#4CAF50",
                Metadata = new Dictionary<string, object>
                {
                    ["messageCount"] = messages.Count,
                    ["type"] = "primary_area"
                }
            });

            // Находим всех позывных в этой зоне
            var callsigns = messages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .GroupBy(c => c)
                .Select(g => new { Callsign = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(20) // Ограничиваем количество
                .ToList();

            foreach (var callsign in callsigns)
            {
                flow.Nodes.Add(new FlowNode
                {
                    Id = $"callsign_{callsign.Callsign}",
                    Label = callsign.Callsign,
                    Type = NodeType.Callsign,
                    Size = callsign.Count,
                    Color = "#2196F3",
                    Metadata = new Dictionary<string, object>
                    {
                        ["messageCount"] = callsign.Count,
                        ["type"] = "callsign_in_area"
                    }
                });

                // Связь позывного с зоной
                flow.Links.Add(new FlowLink
                {
                    SourceId = $"area_{area}",
                    TargetId = $"callsign_{callsign.Callsign}",
                    Value = callsign.Count,
                    Color = "#4CAF50",
                    Strength = CalculateLinkStrength(callsign.Count, messages.Count)
                });
            }

            // Создаем внутренние связи между позывными в зоне
            var internalLinks = new Dictionary<string, Dictionary<string, int>>();

            foreach (var message in messages)
            {
                var messageCallsigns = message.MessageCallsigns
                    .Select(mc => mc.Callsign.Name)
                    .Where(c => callsigns.Select(cs => cs.Callsign).Contains(c))
                    .ToList();

                for (int i = 0; i < messageCallsigns.Count; i++)
                {
                    for (int j = i + 1; j < messageCallsigns.Count; j++)
                    {
                        var source = messageCallsigns[i];
                        var target = messageCallsigns[j];

                        if (!internalLinks.ContainsKey(source))
                            internalLinks[source] = new Dictionary<string, int>();

                        if (!internalLinks[source].ContainsKey(target))
                            internalLinks[source][target] = 0;

                        internalLinks[source][target]++;
                    }
                }
            }

            // Добавляем внутренние связи
            foreach (var source in internalLinks.Keys)
            {
                foreach (var target in internalLinks[source].Keys)
                {
                    if (source.CompareTo(target) < 0) // Избегаем дублирования
                    {
                        flow.Links.Add(new FlowLink
                        {
                            SourceId = $"callsign_{source}",
                            TargetId = $"callsign_{target}",
                            Value = internalLinks[source][target],
                            Color = "#2196F3",
                            Strength = CalculateLinkStrength(internalLinks[source][target], messages.Count)
                        });
                    }
                }
            }

            await BuildMessageFlowsAsync(flow, messages);
            flow.Statistics = await CalculateFlowStatisticsAsync(flow, messages);

            return flow;
        }

        public async Task<ParallelTimeline> BuildParallelTimelineAsync(
            DateTime startTime, DateTime endTime,
            List<string>? callsigns = null, List<string>? areas = null)
        {
            var timeline = new ParallelTimeline
            {
                StartTime = startTime,
                EndTime = endTime
            };

            // Получаем сообщения
            var query = _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .Where(m => m.DateTime >= startTime && m.DateTime <= endTime)
                .OrderBy(m => m.DateTime);

            if (callsigns != null && callsigns.Any())
            {
                query = query.Where(m => m.MessageCallsigns.Any(mc => callsigns.Contains(mc.Callsign.Name)));
            }

            if (areas != null && areas.Any())
            {
                query = query.Where(m => areas.Contains(m.Area.Name));
            }

            var messages = await query.ToListAsync();

            if (!messages.Any())
                return timeline;

            // Создаем треки для позывных
            var activeCallsigns = messages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .Distinct()
                .ToList();

            if (callsigns != null && callsigns.Any())
            {
                activeCallsigns = activeCallsigns.Intersect(callsigns).ToList();
            }

            foreach (var callsign in activeCallsigns)
            {
                var callsignMessages = messages
                    .Where(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign))
                    .OrderBy(m => m.DateTime)
                    .ToList();

                var track = new TimelineTrack
                {
                    Id = $"track_callsign_{callsign}",
                    Name = callsign,
                    Type = "Callsign",
                    Color = GetCallsignColor(callsign),
                    ActivityLevel = CalculateActivityLevel(callsignMessages, startTime, endTime)
                };

                // Добавляем события на трек
                foreach (var message in callsignMessages)
                {
                    var messageType = _dialogAnalyzer.ClassifySingleMessage(message.Dialog);
                    var otherCallsigns = message.MessageCallsigns
                        .Select(mc => mc.Callsign.Name)
                        .Where(c => c != callsign)
                        .ToList();

                    track.Events.Add(new TimelineEvent
                    {
                        Time = message.DateTime,
                        Label = $"{messageType}",
                        Description = Truncate(message.Dialog, 50),
                        Type = messageType.ToString(),
                        Callsigns = otherCallsigns,
                        Color = _eventColors.GetValueOrDefault(messageType.ToString(), "#2196F3"),
                        Duration = 0.5, // Предполагаемая длительность в минутах
                        Metadata = new Dictionary<string, object>
                        {
                            ["messageId"] = message.Id,
                            ["area"] = message.Area.Name,
                            ["frequency"] = message.Frequency.Value,
                            ["callsigns"] = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList()
                        }
                    });
                }

                timeline.Tracks.Add(track);
            }

            // Создаем треки для зон
            var activeAreas = messages
                .Select(m => m.Area.Name)
                .Distinct()
                .ToList();

            if (areas != null && areas.Any())
            {
                activeAreas = activeAreas.Intersect(areas).ToList();
            }

            foreach (var area in activeAreas)
            {
                var areaMessages = messages
                    .Where(m => m.Area.Name == area)
                    .OrderBy(m => m.DateTime)
                    .ToList();

                var track = new TimelineTrack
                {
                    Id = $"track_area_{area}",
                    Name = area,
                    Type = "Area",
                    Color = GetAreaColor(area),
                    ActivityLevel = CalculateActivityLevel(areaMessages, startTime, endTime)
                };

                foreach (var message in areaMessages)
                {
                    var messageType = _dialogAnalyzer.ClassifySingleMessage(message.Dialog);
                    var callsignsInMessage = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();

                    track.Events.Add(new TimelineEvent
                    {
                        Time = message.DateTime,
                        Label = $"{string.Join(", ", callsignsInMessage.Take(2))}",
                        Description = $"{messageType}: {Truncate(message.Dialog, 40)}",
                        Type = "AreaMessage",
                        Callsigns = callsignsInMessage,
                        Color = "#4CAF50",
                        Duration = 0.5,
                        Metadata = new Dictionary<string, object>
                        {
                            ["messageId"] = message.Id,
                            ["messageType"] = messageType.ToString(),
                            ["callsignCount"] = callsignsInMessage.Count
                        }
                    });
                }

                timeline.Tracks.Add(track);
            }

            // Рассчитываем статистику для треков
            foreach (var track in timeline.Tracks)
            {
                timeline.TrackStats[track.Id] = CalculateTrackStatistics(track, startTime, endTime);
            }

            return timeline;
        }

        public async Task<ParallelTimeline> BuildCallsignTimelineAsync(string callsign, DateTime startTime, DateTime endTime)
        {
            var timeline = new ParallelTimeline
            {
                StartTime = startTime,
                EndTime = endTime
            };

            // Получаем сообщения позывного
            var messages = await _context.MessageCallsigns
                .Include(mc => mc.Message)
                    .ThenInclude(m => m.MessageCallsigns)
                        .ThenInclude(mc => mc.Callsign)
                .Include(mc => mc.Message)
                    .ThenInclude(m => m.Area)
                .Where(mc => mc.Callsign.Name == callsign &&
                       mc.Message.DateTime >= startTime &&
                       mc.Message.DateTime <= endTime)
                .Select(mc => mc.Message)
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            if (!messages.Any())
                return timeline;

            // Основной трек позывного
            var mainTrack = new TimelineTrack
            {
                Id = $"track_main_{callsign}",
                Name = callsign,
                Type = "PrimaryCallsign",
                Color = "#2196F3",
                ActivityLevel = CalculateActivityLevel(messages, startTime, endTime)
            };

            // События основного трека
            foreach (var message in messages)
            {
                var messageType = _dialogAnalyzer.ClassifySingleMessage(message.Dialog);
                var otherCallsigns = message.MessageCallsigns
                    .Select(mc => mc.Callsign.Name)
                    .Where(c => c != callsign)
                    .ToList();

                mainTrack.Events.Add(new TimelineEvent
                {
                    Time = message.DateTime,
                    Label = $"{messageType}",
                    Description = Truncate(message.Dialog, 60),
                    Type = messageType.ToString(),
                    Callsigns = otherCallsigns,
                    Color = _eventColors.GetValueOrDefault(messageType.ToString(), "#2196F3"),
                    Duration = CalculateMessageDuration(message, messages),
                    Metadata = new Dictionary<string, object>
                    {
                        ["messageId"] = message.Id,
                        ["area"] = message.Area.Name,
                        ["frequency"] = message.Frequency.Value,
                        ["interlocutors"] = otherCallsigns
                    }
                });
            }

            timeline.Tracks.Add(mainTrack);

            // Треки для собеседников
            var interlocutors = messages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .Where(c => c != callsign)
                .Distinct()
                .Take(10) // Ограничиваем количество
                .ToList();

            foreach (var interlocutor in interlocutors)
            {
                var interlocutorMessages = messages
                    .Where(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == interlocutor))
                    .ToList();

                var track = new TimelineTrack
                {
                    Id = $"track_interlocutor_{interlocutor}",
                    Name = interlocutor,
                    Type = "Interlocutor",
                    Color = GetCallsignColor(interlocutor),
                    ActivityLevel = CalculateActivityLevel(interlocutorMessages, startTime, endTime)
                };

                foreach (var message in interlocutorMessages)
                {
                    var messageType = _dialogAnalyzer.ClassifySingleMessage(message.Dialog);

                    track.Events.Add(new TimelineEvent
                    {
                        Time = message.DateTime,
                        Label = $"С {callsign}",
                        Description = Truncate(message.Dialog, 40),
                        Type = "Interaction",
                        Callsigns = new List<string> { callsign, interlocutor },
                        Color = "#4CAF50",
                        Duration = CalculateMessageDuration(message, messages),
                        Metadata = new Dictionary<string, object>
                        {
                            ["messageId"] = message.Id,
                            ["area"] = message.Area.Name
                        }
                    });
                }

                timeline.Tracks.Add(track);
            }

            // Трек активности по зонам
            var areas = messages
                .GroupBy(m => m.Area.Name)
                .Select(g => new { Area = g.Key, Messages = g.ToList() })
                .OrderByDescending(x => x.Messages.Count)
                .Take(5)
                .ToList();

            foreach (var area in areas)
            {
                var track = new TimelineTrack
                {
                    Id = $"track_area_{area.Area}",
                    Name = area.Area,
                    Type = "Area",
                    Color = GetAreaColor(area.Area),
                    ActivityLevel = CalculateActivityLevel(area.Messages, startTime, endTime)
                };

                foreach (var message in area.Messages)
                {
                    track.Events.Add(new TimelineEvent
                    {
                        Time = message.DateTime,
                        Label = "В зоне",
                        Description = Truncate(message.Dialog, 40),
                        Type = "AreaActivity",
                        Callsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList(),
                        Color = "#FF9800",
                        Duration = 0.5
                    });
                }

                timeline.Tracks.Add(track);
            }

            // Статистика
            foreach (var track in timeline.Tracks)
            {
                timeline.TrackStats[track.Id] = CalculateTrackStatistics(track, startTime, endTime);
            }

            return timeline;
        }

        public async Task<ParallelTimeline> BuildConversationTimelineAsync(long startMessageId, int maxMessages = 20)
        {
            var timeline = new ParallelTimeline();

            // Находим начальное сообщение
            var startMessage = await _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .FirstOrDefaultAsync(m => m.Id == startMessageId);

            if (startMessage == null)
                return timeline;

            timeline.StartTime = startMessage.DateTime.AddMinutes(-5);

            // Находим связанные сообщения (по времени и позывным)
            var callsigns = startMessage.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
            var area = startMessage.Area.Name;

            // Ищем сообщения в том же временном окне с теми же позывными
            var relatedMessages = await _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .Where(m => m.DateTime >= startMessage.DateTime.AddMinutes(-10) &&
                       m.DateTime <= startMessage.DateTime.AddMinutes(30) &&
                       m.MessageCallsigns.Any(mc => callsigns.Contains(mc.Callsign.Name)) &&
                       m.Area.Name == area)
                .OrderBy(m => m.DateTime)
                .Take(maxMessages)
                .ToListAsync();

            if (!relatedMessages.Any())
                return timeline;

            timeline.StartTime = relatedMessages.Min(m => m.DateTime).AddMinutes(-2);
            timeline.EndTime = relatedMessages.Max(m => m.DateTime).AddMinutes(2);

            // Создаем треки для каждого позывного в беседе
            var conversationCallsigns = relatedMessages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .Distinct()
                .ToList();

            foreach (var callsign in conversationCallsigns)
            {
                var callsignMessages = relatedMessages
                    .Where(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign))
                    .OrderBy(m => m.DateTime)
                    .ToList();

                var track = new TimelineTrack
                {
                    Id = $"track_conversation_{callsign}",
                    Name = callsign,
                    Type = "ConversationParticipant",
                    Color = GetCallsignColor(callsign),
                    ActivityLevel = CalculateActivityLevel(callsignMessages, timeline.StartTime, timeline.EndTime)
                };

                // Определяем порядок в разговоре
                int messageOrder = 0;
                foreach (var message in callsignMessages)
                {
                    messageOrder++;
                    var messageType = _dialogAnalyzer.ClassifySingleMessage(message.Dialog);
                    var otherCallsigns = message.MessageCallsigns
                        .Select(mc => mc.Callsign.Name)
                        .Where(c => c != callsign)
                        .ToList();

                    // Рассчитываем длительность до следующего сообщения
                    double duration = 2.0; // Базовая длительность
                    var nextMessage = callsignMessages
                        .Where(m => m.DateTime > message.DateTime)
                        .OrderBy(m => m.DateTime)
                        .FirstOrDefault();

                    if (nextMessage != null)
                    {
                        duration = (nextMessage.DateTime - message.DateTime).TotalMinutes;
                        duration = Math.Min(duration, 10); // Ограничиваем максимальную длительность
                    }

                    track.Events.Add(new TimelineEvent
                    {
                        Time = message.DateTime,
                        Label = $"{messageOrder}. {messageType}",
                        Description = Truncate(message.Dialog, 80),
                        Type = messageType.ToString(),
                        Callsigns = otherCallsigns,
                        Color = _eventColors.GetValueOrDefault(messageType.ToString(), "#2196F3"),
                        Duration = duration,
                        Metadata = new Dictionary<string, object>
                        {
                            ["messageId"] = message.Id,
                            ["order"] = messageOrder,
                            ["responseTo"] = FindResponseTo(message, callsignMessages, callsign)
                        }
                    });
                }

                timeline.Tracks.Add(track);
            }

            // Добавляем трек для всей беседы
            var conversationTrack = new TimelineTrack
            {
                Id = "track_conversation_summary",
                Name = "Вся беседа",
                Type = "ConversationSummary",
                Color = "#9C27B0",
                ActivityLevel = 1.0
            };

            int globalOrder = 0;
            foreach (var message in relatedMessages.OrderBy(m => m.DateTime))
            {
                globalOrder++;
                var callsignsInMessage = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
                var messageType = _dialogAnalyzer.ClassifySingleMessage(message.Dialog);

                conversationTrack.Events.Add(new TimelineEvent
                {
                    Time = message.DateTime,
                    Label = $"{globalOrder}. {string.Join(", ", callsignsInMessage)}",
                    Description = $"{messageType}: {Truncate(message.Dialog, 60)}",
                    Type = "ConversationStep",
                    Callsigns = callsignsInMessage,
                    Color = "#9C27B0",
                    Duration = 1.0,
                    Metadata = new Dictionary<string, object>
                    {
                        ["messageId"] = message.Id,
                        ["step"] = globalOrder,
                        ["participants"] = callsignsInMessage.Count
                    }
                });
            }

            timeline.Tracks.Add(conversationTrack);

            // Статистика
            foreach (var track in timeline.Tracks)
            {
                timeline.TrackStats[track.Id] = CalculateTrackStatistics(track, timeline.StartTime, timeline.EndTime);
            }

            return timeline;
        }

        public async Task<List<CommunicationPattern>> DetectCommunicationPatternsAsync(DateTime startTime, DateTime endTime)
        {
            var patterns = new List<CommunicationPattern>();

            // Получаем сообщения за период
            var messages = await _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Where(m => m.DateTime >= startTime && m.DateTime <= endTime)
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            if (!messages.Any())
                return patterns;

            // 1. Паттерн "Вопрос-Ответ"
            var questionAnswerPattern = await DetectQuestionAnswerPatternAsync(messages);
            if (questionAnswerPattern != null)
                patterns.Add(questionAnswerPattern);

            // 2. Паттерн "Команда-Подтверждение"
            var commandConfirmationPattern = await DetectCommandConfirmationPatternAsync(messages);
            if (commandConfirmationPattern != null)
                patterns.Add(commandConfirmationPattern);

            // 3. Паттерн "Отчет-Принятие"
            var reportAcceptancePattern = await DetectReportAcceptancePatternAsync(messages);
            if (reportAcceptancePattern != null)
                patterns.Add(reportAcceptancePattern);

            // 4. Паттерн "Координация группы"
            var groupCoordinationPattern = await DetectGroupCoordinationPatternAsync(messages);
            if (groupCoordinationPattern != null)
                patterns.Add(groupCoordinationPattern);

            // 5. Паттерн "Эстафета" (сообщение передается от одного позывного к другому)
            var relayPattern = await DetectRelayPatternAsync(messages);
            if (relayPattern != null)
                patterns.Add(relayPattern);

            return patterns
                .OrderByDescending(p => p.Frequency)
                .ThenByDescending(p => p.Confidence)
                .ToList();
        }

        public async Task<List<string>> FindCommonFlowsAsync(int minOccurrences = 3)
        {
            var commonFlows = new List<string>();

            // Анализируем последовательности сообщений
            var messages = await _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .OrderBy(m => m.DateTime)
                .Take(1000) // Ограничиваем для производительности
                .ToListAsync();

            // Разбиваем на сессии (разрыв более 5 минут)
            var sessions = new List<List<Message>>();
            var currentSession = new List<Message>();
            DateTime? lastTime = null;

            foreach (var message in messages)
            {
                if (lastTime.HasValue && (message.DateTime - lastTime.Value).TotalMinutes > 5)
                {
                    if (currentSession.Count >= 2)
                    {
                        sessions.Add(new List<Message>(currentSession));
                    }
                    currentSession.Clear();
                }

                currentSession.Add(message);
                lastTime = message.DateTime;
            }

            if (currentSession.Count >= 2)
            {
                sessions.Add(currentSession);
            }

            // Ищем общие паттерны в сессиях
            var flowPatterns = new Dictionary<string, int>();

            foreach (var session in sessions.Where(s => s.Count >= 2 && s.Count <= 10))
            {
                // Извлекаем паттерн типов сообщений
                var pattern = string.Join("->", session
                    .Select(m => _dialogAnalyzer.ClassifySingleMessage(m.Dialog))
                    .Select(t => t.ToString()));

                if (!flowPatterns.ContainsKey(pattern))
                    flowPatterns[pattern] = 0;

                flowPatterns[pattern]++;

                // Извлекаем паттерн позывных
                var callsignPattern = string.Join("->", session
                    .Select(m => string.Join("+", m.MessageCallsigns
                        .Select(mc => mc.Callsign.Name)
                        .OrderBy(c => c))));

                if (!string.IsNullOrEmpty(callsignPattern))
                {
                    var key = $"Callsigns: {callsignPattern}";
                    if (!flowPatterns.ContainsKey(key))
                        flowPatterns[key] = 0;

                    flowPatterns[key]++;
                }
            }

            // Отбираем часто встречающиеся паттерны
            foreach (var pattern in flowPatterns.Where(p => p.Value >= minOccurrences))
            {
                commonFlows.Add($"{pattern.Key} (встречается {pattern.Value} раз)");
            }

            return commonFlows
                .OrderByDescending(f => flowPatterns[f.Split(" (встречается")[0]])
                .ToList();
        }

        public async Task<Dictionary<string, double>> CalculateFlowMetricsAsync(DateTime startTime, DateTime endTime)
        {
            var metrics = new Dictionary<string, double>();

            var messages = await _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Where(m => m.DateTime >= startTime && m.DateTime <= endTime)
                .ToListAsync();

            if (!messages.Any())
                return metrics;

            // 1. Интенсивность потока (сообщений в минуту)
            var duration = (endTime - startTime).TotalMinutes;
            metrics["MessageIntensity"] = messages.Count / duration;

            // 2. Среднее количество позывных в сообщении
            var avgCallsignsPerMessage = messages.Average(m => m.MessageCallsigns.Count);
            metrics["AvgCallsignsPerMessage"] = avgCallsignsPerMessage;

            // 3. Коэффициент ответов
            var responseRate = await CalculateResponseRateAsync(messages);
            metrics["ResponseRate"] = responseRate;

            // 4. Время реакции (среднее время между сообщениями в диалоге)
            var reactionTime = await CalculateAverageReactionTimeAsync(messages);
            metrics["AvgReactionTimeSeconds"] = reactionTime.TotalSeconds;

            // 5. Сетевая плотность
            var networkDensity = await CalculateNetworkDensityAsync(messages);
            metrics["NetworkDensity"] = networkDensity;

            // 6. Коэффициент централизации
            var centralization = await CalculateCentralizationAsync(messages);
            metrics["Centralization"] = centralization;

            // 7. Эффективность потока (отношение уникальных связей к возможным)
            var flowEfficiency = await CalculateFlowEfficiencyAsync(messages);
            metrics["FlowEfficiency"] = flowEfficiency;

            return metrics;
        }

        public async Task<CommunicationFlow> BuildGroupCommunicationFlowAsync(string groupName, DateTime startTime, DateTime endTime)
        {
            // Эта функция строит поток для группы позывных
            // Группа может быть определена по кластеризации или вручную

            var flow = new CommunicationFlow
            {
                StartTime = startTime,
                EndTime = endTime
            };

            // Получаем позывные группы (здесь нужно будет определить, как получать группу)
            // Пока что используем пример
            var groupCallsigns = new List<string>(); // Заполнить из какого-то источника

            if (!groupCallsigns.Any())
                return flow;

            // Получаем сообщения группы
            var messages = await _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .Where(m => m.DateTime >= startTime && m.DateTime <= endTime &&
                       m.MessageCallsigns.Any(mc => groupCallsigns.Contains(mc.Callsign.Name)))
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            if (!messages.Any())
                return flow;

            // Узел группы
            flow.Nodes.Add(new FlowNode
            {
                Id = $"group_{groupName}",
                Label = groupName,
                Type = NodeType.Cluster,
                Size = messages.Count,
                Color = "#F44336",
                Metadata = new Dictionary<string, object>
                {
                    ["messageCount"] = messages.Count,
                    ["callsignCount"] = groupCallsigns.Count
                }
            });

            // Узлы позывных группы
            foreach (var callsign in groupCallsigns)
            {
                var callsignMessages = messages
                    .Count(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign));

                flow.Nodes.Add(new FlowNode
                {
                    Id = $"callsign_{callsign}",
                    Label = callsign,
                    Type = NodeType.Callsign,
                    Size = callsignMessages,
                    Color = "#2196F3",
                    Groups = new List<string> { $"group_{groupName}" }
                });

                // Связь с группой
                flow.Links.Add(new FlowLink
                {
                    SourceId = $"group_{groupName}",
                    TargetId = $"callsign_{callsign}",
                    Value = callsignMessages,
                    Color = "#F44336",
                    Strength = CalculateLinkStrength(callsignMessages, messages.Count)
                });
            }

            // Внутренние связи в группе
            var internalLinks = new Dictionary<string, Dictionary<string, int>>();

            foreach (var message in messages)
            {
                var messageCallsigns = message.MessageCallsigns
                    .Select(mc => mc.Callsign.Name)
                    .Where(c => groupCallsigns.Contains(c))
                    .ToList();

                for (int i = 0; i < messageCallsigns.Count; i++)
                {
                    for (int j = i + 1; j < messageCallsigns.Count; j++)
                    {
                        var source = messageCallsigns[i];
                        var target = messageCallsigns[j];

                        if (!internalLinks.ContainsKey(source))
                            internalLinks[source] = new Dictionary<string, int>();

                        if (!internalLinks[source].ContainsKey(target))
                            internalLinks[source][target] = 0;

                        internalLinks[source][target]++;
                    }
                }
            }

            // Добавляем внутренние связи
            foreach (var source in internalLinks.Keys)
            {
                foreach (var target in internalLinks[source].Keys)
                {
                    if (source.CompareTo(target) < 0)
                    {
                        flow.Links.Add(new FlowLink
                        {
                            SourceId = $"callsign_{source}",
                            TargetId = $"callsign_{target}",
                            Value = internalLinks[source][target],
                            Color = "#2196F3",
                            Strength = CalculateLinkStrength(internalLinks[source][target], messages.Count)
                        });
                    }
                }
            }

            // Внешние связи (с позывными вне группы)
            var externalCallsigns = messages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .Where(c => !groupCallsigns.Contains(c))
                .Distinct()
                .Take(10)
                .ToList();

            foreach (var externalCallsign in externalCallsigns)
            {
                var interactionCount = messages
                    .Count(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == externalCallsign));

                // Добавляем узел внешнего позывного
                flow.Nodes.Add(new FlowNode
                {
                    Id = $"callsign_{externalCallsign}",
                    Label = externalCallsign,
                    Type = NodeType.Callsign,
                    Size = interactionCount,
                    Color = "#9C27B0",
                    Metadata = new Dictionary<string, object>
                    {
                        ["type"] = "external_callsign"
                    }
                });

                // Находим, с кем из группы общается
                foreach (var groupCallsign in groupCallsigns)
                {
                    var interactions = messages
                        .Count(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == groupCallsign) &&
                               m.MessageCallsigns.Any(mc => mc.Callsign.Name == externalCallsign));

                    if (interactions > 0)
                    {
                        flow.Links.Add(new FlowLink
                        {
                            SourceId = $"callsign_{groupCallsign}",
                            TargetId = $"callsign_{externalCallsign}",
                            Value = interactions,
                            Color = "#9C27B0",
                            Strength = CalculateLinkStrength(interactions, messages.Count)
                        });
                    }
                }
            }

            await BuildMessageFlowsAsync(flow, messages);
            flow.Statistics = await CalculateFlowStatisticsAsync(flow, messages);

            return flow;
        }

        public async Task<List<CommunicationFlow>> CompareTimePeriodsAsync(
            DateTime period1Start, DateTime period1End,
            DateTime period2Start, DateTime period2End)
        {
            var flows = new List<CommunicationFlow>();

            // Строим потоки для каждого периода
            var flow1 = await BuildSankeyDiagramAsync(period1Start, period1End, maxNodes: 30);
            var flow2 = await BuildSankeyDiagramAsync(period2Start, period2End, maxNodes: 30);

            flows.Add(flow1);
            flows.Add(flow2);

            // Можно добавить третий поток с различиями
            var diffFlow = new CommunicationFlow
            {
                StartTime = period1Start,
                EndTime = period2End
            };

            // Здесь можно реализовать вычисление различий между потоками
            // (например, какие связи появились/исчезли, изменилась интенсивность)

            return flows;
        }

        public async Task<string> ExportFlowDataAsync(CommunicationFlow flow, string format = "json")
        {
            if (format.ToLower() == "csv")
            {
                // Экспорт в CSV
                var lines = new List<string>
                {
                    "Source,Target,Value,Strength,Color"
                };

                foreach (var link in flow.Links)
                {
                    lines.Add($"{link.SourceId},{link.TargetId},{link.Value},{link.Strength:F3},{link.Color}");
                }

                return string.Join(Environment.NewLine, lines);
            }
            else
            {
                // Экспорт в JSON
                var exportData = new
                {
                    nodes = flow.Nodes.Select(n => new
                    {
                        id = n.Id,
                        label = n.Label,
                        size = n.Size,
                        color = n.Color,
                        type = n.Type.ToString()
                    }),
                    links = flow.Links.Select(l => new
                    {
                        source = l.SourceId,
                        target = l.TargetId,
                        value = l.Value,
                        strength = l.Strength,
                        color = l.Color
                    }),
                    statistics = flow.Statistics,
                    timeframe = new
                    {
                        start = flow.StartTime,
                        end = flow.EndTime
                    }
                };

                return System.Text.Json.JsonSerializer.Serialize(exportData,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            }
        }

        public async Task<string> ExportTimelineDataAsync(ParallelTimeline timeline, string format = "json")
        {
            if (format.ToLower() == "csv")
            {
                var lines = new List<string>
                {
                    "Track,Time,Label,Description,Type,Duration,Color"
                };

                foreach (var track in timeline.Tracks)
                {
                    foreach (var ev in track.Events)
                    {
                        lines.Add($"{track.Name},{ev.Time:HH:mm:ss},{ev.Label},{ev.Description},{ev.Type},{ev.Duration},{ev.Color}");
                    }
                }

                return string.Join(Environment.NewLine, lines);
            }
            else
            {
                var exportData = new
                {
                    tracks = timeline.Tracks.Select(t => new
                    {
                        id = t.Id,
                        name = t.Name,
                        type = t.Type,
                        color = t.Color,
                        events = t.Events.Select(e => new
                        {
                            time = e.Time,
                            label = e.Label,
                            description = e.Description,
                            type = e.Type,
                            duration = e.Duration,
                            color = e.Color,
                            callsigns = e.Callsigns
                        })
                    }),
                    timeframe = new
                    {
                        start = timeline.StartTime,
                        end = timeline.EndTime
                    },
                    statistics = timeline.TrackStats
                };

                return System.Text.Json.JsonSerializer.Serialize(exportData,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            }
        }

        // Вспомогательные методы

        private async Task BuildMessageFlowsAsync(CommunicationFlow flow, List<Message> messages)
        {
            // Группируем сообщения по временным интервалам для создания потоков
            var timeSlots = messages
                .GroupBy(m => m.DateTime.ToString("yyyy-MM-dd HH:mm"))
                .OrderBy(g => g.Key)
                .Take(20) // Ограничиваем количество временных слотов
                .ToList();

            int slotIndex = 0;
            foreach (var slot in timeSlots)
            {
                var slotNode = new FlowNode
                {
                    Id = $"timeslot_{slotIndex}",
                    Label = slot.Key,
                    Type = NodeType.TimeSlot,
                    Size = slot.Count(),
                    Color = "#9C27B0"
                };

                flow.Nodes.Add(slotNode);

                // Связываем сообщения с временным слотом
                foreach (var message in slot)
                {
                    var messageType = _dialogAnalyzer.ClassifySingleMessage(message.Dialog);

                    flow.MessageFlows.Add(new MessageFlow
                    {
                        MessageId = message.Id,
                        Timestamp = message.DateTime,
                        Path = new List<string> { $"timeslot_{slotIndex}" },
                        Duration = 0,
                        MessageType = messageType.ToString(),
                        Content = Truncate(message.Dialog, 100),
                        Callsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList()
                    });
                }

                slotIndex++;
            }
        }

        private async Task<Dictionary<string, FlowStatistics>> CalculateFlowStatisticsAsync(CommunicationFlow flow, List<Message> messages)
        {
            var stats = new FlowStatistics
            {
                TotalMessages = messages.Count,
                UniqueCallsigns = messages
                    .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                    .Distinct()
                    .Count(),
                UniqueLinks = flow.Links.Count,
                AverageMessagesPerMinute = messages.Count > 0
                    ? messages.Count / (flow.EndTime - flow.StartTime).TotalMinutes
                    : 0
            };

            // Расчет среднего времени ответа
            var responseTimes = new List<TimeSpan>();
            var sortedMessages = messages.OrderBy(m => m.DateTime).ToList();

            for (int i = 0; i < sortedMessages.Count - 1; i++)
            {
                var current = sortedMessages[i];
                var next = sortedMessages[i + 1];

                // Проверяем, есть ли общие позывные
                var currentCallsigns = current.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
                var nextCallsigns = next.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();

                if (currentCallsigns.Intersect(nextCallsigns).Any())
                {
                    responseTimes.Add(next.DateTime - current.DateTime);
                }
            }

            if (responseTimes.Any())
            {
                stats.AverageResponseTime = TimeSpan.FromSeconds(responseTimes.Average(ts => ts.TotalSeconds));
            }

            // Плотность сети (отношение фактических связей к возможным)
            var callsignCount = stats.UniqueCallsigns;
            if (callsignCount > 1)
            {
                var maxPossibleLinks = callsignCount * (callsignCount - 1) / 2;
                stats.NetworkDensity = (double)stats.UniqueLinks / maxPossibleLinks;
            }

            // Топ позывных
            stats.TopCallsigns = messages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            // Критические связи (наиболее интенсивные)
            stats.CriticalLinks = flow.Links
                .OrderByDescending(l => l.Value)
                .Take(5)
                .Select(l => $"{l.SourceId} -> {l.TargetId}: {l.Value}")
                .ToList();

            // Метрики центральности
            var centrality = await CalculateCentralityMetricsAsync(flow, messages);
            stats.CentralityMetrics = centrality;

            return new Dictionary<string, FlowStatistics> { ["overall"] = stats };
        }

        private async Task<Dictionary<string, double>> CalculateCentralityMetricsAsync(CommunicationFlow flow, List<Message> messages)
        {
            var metrics = new Dictionary<string, double>();

            // Рассчитываем центральность для каждого позывного
            var callsignDegrees = new Dictionary<string, int>();

            foreach (var link in flow.Links)
            {
                // Извлекаем позывные из ID узлов
                if (link.SourceId.StartsWith("callsign_"))
                {
                    var callsign = link.SourceId.Substring("callsign_".Length);
                    if (!callsignDegrees.ContainsKey(callsign))
                        callsignDegrees[callsign] = 0;
                    callsignDegrees[callsign]++;
                }

                if (link.TargetId.StartsWith("callsign_"))
                {
                    var callsign = link.TargetId.Substring("callsign_".Length);
                    if (!callsignDegrees.ContainsKey(callsign))
                        callsignDegrees[callsign] = 0;
                    callsignDegrees[callsign]++;
                }
            }

            if (callsignDegrees.Any())
            {
                var maxDegree = callsignDegrees.Values.Max();

                foreach (var kvp in callsignDegrees.OrderByDescending(kv => kv.Value).Take(10))
                {
                    metrics[$"centrality_{kvp.Key}"] = (double)kvp.Value / maxDegree;
                }
            }

            return metrics;
        }

        private double CalculateLinkStrength(int linkValue, int totalMessages)
        {
            if (totalMessages == 0)
                return 0;

            // Нормализуем значение связи от 0 до 1
            var normalized = (double)linkValue / totalMessages;
            return Math.Min(1.0, normalized * 10); // Усиливаем для лучшей визуализации
        }

        private string GetCallsignColor(string callsign)
        {
            // Генерируем цвет на основе хеша позывного
            int hash = callsign.GetHashCode();
            var colors = new[]
            {
                "#2196F3", "#4CAF50", "#FF9800", "#F44336", "#9C27B0",
                "#00BCD4", "#8BC34A", "#FFC107", "#E91E63", "#3F51B5"
            };

            return colors[Math.Abs(hash) % colors.Length];
        }

        private string GetAreaColor(string area)
        {
            int hash = area.GetHashCode();
            var colors = new[]
            {
                "#4CAF50", "#8BC34A", "#CDDC39", "#FFEB3B", "#FFC107",
                "#FF9800", "#FF5722", "#795548", "#9E9E9E", "#607D8B"
            };

            return colors[Math.Abs(hash) % colors.Length];
        }

        private double CalculateActivityLevel(List<Message> messages, DateTime startTime, DateTime endTime)
        {
            if (!messages.Any())
                return 0;

            var totalDuration = (endTime - startTime).TotalMinutes;
            if (totalDuration == 0)
                return 0;

            // Рассчитываем долю времени, когда были сообщения
            var activeMinutes = messages.Count * 0.5; // Предполагаем 30 секунд на сообщение
            return Math.Min(1.0, activeMinutes / totalDuration);
        }

        private double CalculateMessageDuration(Message message, List<Message> allMessages)
        {
            // Находим следующее сообщение с участием тех же позывных
            var callsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();

            var nextMessage = allMessages
                .Where(m => m.DateTime > message.DateTime &&
                       m.MessageCallsigns.Any(mc => callsigns.Contains(mc.Callsign.Name)))
                .OrderBy(m => m.DateTime)
                .FirstOrDefault();

            if (nextMessage != null)
            {
                var duration = (nextMessage.DateTime - message.DateTime).TotalMinutes;
                return Math.Min(duration, 5); // Ограничиваем максимальную длительность
            }

            return 1.0; // Базовая длительность
        }

        private TimelineStatistics CalculateTrackStatistics(TimelineTrack track, DateTime startTime, DateTime endTime)
        {
            var stats = new TimelineStatistics
            {
                EventCount = track.Events.Count
            };

            if (track.Events.Any())
            {
                var totalDuration = track.Events.Sum(e => e.Duration);
                stats.TotalDuration = TimeSpan.FromMinutes(totalDuration);
                stats.AverageEventDuration = TimeSpan.FromMinutes(totalDuration / track.Events.Count);

                // Плотность событий (событий в час)
                var timeframeHours = (endTime - startTime).TotalHours;
                stats.Density = timeframeHours > 0 ? track.Events.Count / timeframeHours : 0;

                // Пиковые времена (часы с наибольшим количеством событий)
                var hours = track.Events
                    .GroupBy(e => e.Time.Hour)
                    .Select(g => new { Hour = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(3)
                    .Select(x => new DateTime(startTime.Year, startTime.Month, startTime.Day, x.Hour, 0, 0))
                    .ToList();

                stats.PeakTimes = hours;
            }

            return stats;
        }

        private async Task<double> CalculateResponseRateAsync(List<Message> messages)
        {
            if (messages.Count < 2)
                return 0;

            int responses = 0;
            var sortedMessages = messages.OrderBy(m => m.DateTime).ToList();

            for (int i = 0; i < sortedMessages.Count - 1; i++)
            {
                var current = sortedMessages[i];
                var next = sortedMessages[i + 1];

                // Проверяем, есть ли общие позывные и разница во времени менее 5 минут
                var currentCallsigns = current.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
                var nextCallsigns = next.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();

                if (currentCallsigns.Intersect(nextCallsigns).Any() &&
                    (next.DateTime - current.DateTime).TotalMinutes < 5)
                {
                    responses++;
                }
            }

            return (double)responses / (sortedMessages.Count - 1);
        }

        private async Task<TimeSpan> CalculateAverageReactionTimeAsync(List<Message> messages)
        {
            var reactionTimes = new List<TimeSpan>();
            var sortedMessages = messages.OrderBy(m => m.DateTime).ToList();

            for (int i = 0; i < sortedMessages.Count - 1; i++)
            {
                var current = sortedMessages[i];
                var next = sortedMessages[i + 1];

                var currentCallsigns = current.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
                var nextCallsigns = next.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();

                if (currentCallsigns.Intersect(nextCallsigns).Any())
                {
                    reactionTimes.Add(next.DateTime - current.DateTime);
                }
            }

            if (reactionTimes.Any())
            {
                return TimeSpan.FromSeconds(reactionTimes.Average(ts => ts.TotalSeconds));
            }

            return TimeSpan.Zero;
        }

        private async Task<double> CalculateNetworkDensityAsync(List<Message> messages)
        {
            var allCallsigns = messages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .Distinct()
                .ToList();

            if (allCallsigns.Count < 2)
                return 0;

            // Считаем уникальные связи
            var uniqueLinks = new HashSet<string>();

            foreach (var message in messages)
            {
                var callsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();

                for (int i = 0; i < callsigns.Count; i++)
                {
                    for (int j = i + 1; j < callsigns.Count; j++)
                    {
                        var link = callsigns[i].CompareTo(callsigns[j]) < 0
                            ? $"{callsigns[i]}-{callsigns[j]}"
                            : $"{callsigns[j]}-{callsigns[i]}";

                        uniqueLinks.Add(link);
                    }
                }
            }

            var maxPossibleLinks = allCallsigns.Count * (allCallsigns.Count - 1) / 2;
            return (double)uniqueLinks.Count / maxPossibleLinks;
        }

        private async Task<double> CalculateCentralizationAsync(List<Message> messages)
        {
            // Рассчитываем степень центральности сети
            var callsignDegrees = new Dictionary<string, int>();

            foreach (var message in messages)
            {
                var callsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();

                foreach (var callsign in callsigns)
                {
                    if (!callsignDegrees.ContainsKey(callsign))
                        callsignDegrees[callsign] = 0;

                    callsignDegrees[callsign] += callsigns.Count - 1; // Связи с другими позывными в сообщении
                }
            }

            if (!callsignDegrees.Any())
                return 0;

            var maxDegree = callsignDegrees.Values.Max();
            var sumDifferences = callsignDegrees.Values.Sum(degree => maxDegree - degree);
            var n = callsignDegrees.Count;

            if (n < 2)
                return 0;

            return (double)sumDifferences / ((n - 1) * (n - 2));
        }

        private async Task<double> CalculateFlowEfficiencyAsync(List<Message> messages)
        {
            var allCallsigns = messages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .Distinct()
                .ToList();

            if (allCallsigns.Count < 2)
                return 0;

            var messageFlows = new Dictionary<string, int>();

            foreach (var message in messages)
            {
                var callsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();

                for (int i = 0; i < callsigns.Count - 1; i++)
                {
                    var flowKey = $"{callsigns[i]}-{callsigns[i + 1]}";
                    if (!messageFlows.ContainsKey(flowKey))
                        messageFlows[flowKey] = 0;

                    messageFlows[flowKey]++;
                }
            }

            var totalFlows = messageFlows.Values.Sum();
            var uniqueFlows = messageFlows.Count;

            return totalFlows > 0 ? (double)uniqueFlows / totalFlows : 0;
        }

        private async Task<CommunicationPattern> DetectQuestionAnswerPatternAsync(List<Message> messages)
        {
            var pattern = new CommunicationPattern
            {
                PatternType = "Вопрос-Ответ",
                Description = "Паттерн, когда один позывной задает вопрос, а другой отвечает"
            };

            int occurrences = 0;
            var examples = new List<string>();
            var characteristicCallsigns = new HashSet<string>();

            for (int i = 0; i < messages.Count - 1; i++)
            {
                var current = messages[i];
                var next = messages[i + 1];

                var currentType = _dialogAnalyzer.ClassifySingleMessage(current.Dialog);
                var nextType = _dialogAnalyzer.ClassifySingleMessage(next.Dialog);

                // Проверяем, является ли текущее сообщение вопросом, а следующее - ответом
                if ((currentType == MessageType.Query || current.Dialog.Contains("?")) &&
                    (nextType == MessageType.Confirmation || nextType == MessageType.Report) &&
                    (next.DateTime - current.DateTime).TotalMinutes < 2)
                {
                    // Проверяем, есть ли общие позывные
                    var currentCallsigns = current.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
                    var nextCallsigns = next.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();

                    if (currentCallsigns.Intersect(nextCallsigns).Any())
                    {
                        occurrences++;

                        if (examples.Count < 3)
                        {
                            examples.Add($"{current.DateTime:HH:mm}: {Truncate(current.Dialog, 50)} -> {next.DateTime:HH:mm}: {Truncate(next.Dialog, 50)}");
                        }

                        foreach (var callsign in currentCallsigns.Concat(nextCallsigns))
                        {
                            characteristicCallsigns.Add(callsign);
                        }
                    }
                }
            }

            if (occurrences > 0)
            {
                pattern.Frequency = (double)occurrences / messages.Count * 100;
                pattern.Confidence = Math.Min(1.0, occurrences / 10.0);
                pattern.ExampleFlows = examples;
                pattern.CharacteristicCallsigns = characteristicCallsigns.ToList();
                pattern.TypicalDuration = TimeSpan.FromMinutes(1);

                return pattern;
            }

            return null;
        }

        private async Task<CommunicationPattern> DetectCommandConfirmationPatternAsync(List<Message> messages)
        {
            var pattern = new CommunicationPattern
            {
                PatternType = "Команда-Подтверждение",
                Description = "Паттерн, когда один позывной отдает команду, а другой подтверждает выполнение"
            };

            // Реализация аналогична DetectQuestionAnswerPatternAsync
            // ... (опущено для краткости)

            return null;
        }

        private async Task<CommunicationPattern> DetectReportAcceptancePatternAsync(List<Message> messages)
        {
            // Реализация аналогична DetectQuestionAnswerPatternAsync
            return null;
        }

        private async Task<CommunicationPattern> DetectGroupCoordinationPatternAsync(List<Message> messages)
        {
            // Реализация аналогична DetectQuestionAnswerPatternAsync
            return null;
        }

        private async Task<CommunicationPattern> DetectRelayPatternAsync(List<Message> messages)
        {
            // Реализация аналогична DetectQuestionAnswerPatternAsync
            return null;
        }

        private string FindResponseTo(Message message, List<Message> conversation, string currentCallsign)
        {
            // Ищем, на какое сообщение может быть ответом текущее
            var previousMessages = conversation
                .Where(m => m.DateTime < message.DateTime)
                .OrderByDescending(m => m.DateTime)
                .Take(5);

            foreach (var prevMessage in previousMessages)
            {
                var prevCallsigns = prevMessage.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();
                var currentCallsigns = message.MessageCallsigns.Select(mc => mc.Callsign.Name).ToList();

                // Если есть общие позывные и разница во времени небольшая
                if (prevCallsigns.Intersect(currentCallsigns).Any() &&
                    (message.DateTime - prevMessage.DateTime).TotalMinutes < 5)
                {
                    return $"Ответ на сообщение {prevMessage.DateTime:HH:mm:ss}";
                }
            }

            return "Новое сообщение";
        }

        private string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }
    }
}