//using RadioIntercepts.Core.Models;
//using RadioIntercepts.Infrastructure.Data;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace RadioIntercepts.Analysis.Services.Graphs
//{
//    public class GraphService
//    {
//        private readonly AppDbContext _context;

//        public GraphService(AppDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<InteractionGraph> BuildInteractionGraphAsync(DateTime? startDate = null, DateTime? endDate = null, string area = null, string frequency = null)
//        {
//            var query = _context.MessageCallsigns
//                .Include(mc => mc.Message)
//                    .ThenInclude(m => m.Area)
//                .Include(mc => mc.Message)
//                    .ThenInclude(m => m.Frequency)
//                .Include(mc => mc.Callsign)
//                .AsQueryable();

//            if (startDate.HasValue)
//                query = query.Where(mc => mc.Message.DateTime >= startDate.Value);

//            if (endDate.HasValue)
//                query = query.Where(mc => mc.Message.DateTime <= endDate.Value);

//            if (!string.IsNullOrEmpty(area))
//            {
//                var areaEntity = await _context.Areas.FirstOrDefaultAsync(a => a.Name == area);
//                if (areaEntity != null)
//                    query = query.Where(mc => mc.Message.Area.Key == areaEntity.Key);
//            }

//            if (!string.IsNullOrEmpty(frequency))
//                query = query.Where(mc => mc.Message.Frequency.Value == frequency);

//            var messageCallsigns = await query.ToListAsync();

//            // Группируем по сообщениям, чтобы найти взаимодействия
//            var messages = messageCallsigns.GroupBy(mc => mc.MessageId);

//            var nodes = new Dictionary<string, GraphNode>();
//            var edges = new Dictionary<(string, string), GraphEdge>();

//            foreach (var message in messages)
//            {
//                var callsignsInMessage = message.Select(mc => mc.Callsign.Name).Distinct().ToList();

//                // Для каждого сообщения добавляем связи между всеми позывными в нем
//                for (int i = 0; i < callsignsInMessage.Count; i++)
//                {
//                    var callsign1 = callsignsInMessage[i];

//                    if (!nodes.ContainsKey(callsign1))
//                        nodes[callsign1] = new GraphNode { Callsign = callsign1, TotalMessages = 0 };

//                    nodes[callsign1].TotalMessages++;

//                    for (int j = i + 1; j < callsignsInMessage.Count; j++)
//                    {
//                        var callsign2 = callsignsInMessage[j];

//                        if (!nodes.ContainsKey(callsign2))
//                            nodes[callsign2] = new GraphNode { Callsign = callsign2, TotalMessages = 0 };

//                        var key = (callsign1, callsign2);
//                        if (string.Compare(callsign1, callsign2) > 0)
//                            key = (callsign2, callsign1);

//                        if (!edges.ContainsKey(key))
//                        {
//                            edges[key] = new GraphEdge
//                            {
//                                FromCallsign = key.Item1,
//                                ToCallsign = key.Item2,
//                                MessageCount = 0,
//                                FirstInteraction = message.First().Message.DateTime,
//                                LastInteraction = message.First().Message.DateTime
//                            };
//                        }

//                        edges[key].MessageCount++;
//                        if (message.First().Message.DateTime < edges[key].FirstInteraction)
//                            edges[key].FirstInteraction = message.First().Message.DateTime;
//                        if (message.First().Message.DateTime > edges[key].LastInteraction)
//                            edges[key].LastInteraction = message.First().Message.DateTime;
//                    }
//                }
//            }

//            // Вычисляем центральность для каждого узла (простая степень)
//            foreach (var node in nodes.Values)
//            {
//                node.Centrality = edges.Values
//                    .Where(e => e.FromCallsign == node.Callsign || e.ToCallsign == node.Callsign)
//                    .Sum(e => e.MessageCount);
//            }

//            return new InteractionGraph
//            {
//                Nodes = nodes.Values.ToList(),
//                Edges = edges.Values.ToList()
//            };
//        }

//        // Метод для поиска ключевых игроков (топ N по центральности)
//        public async Task<List<string>> FindKeyPlayersAsync(int topN = 10, DateTime? startDate = null, DateTime? endDate = null)
//        {
//            var graph = await BuildInteractionGraphAsync(startDate, endDate);
//            return graph.Nodes
//                .OrderByDescending(n => n.Centrality)
//                .Take(topN)
//                .Select(n => n.Callsign)
//                .ToList();
//        }

//        // Метод для обнаружения сообществ (упрощенный алгоритм)
//        public async Task<List<List<string>>> DetectCommunitiesAsync(DateTime? startDate = null, DateTime? endDate = null)
//        {
//            var graph = await BuildInteractionGraphAsync(startDate, endDate);
//            // Простой алгоритм: каждая связь - сообщество из двух позывных, затем объединение
//            // В реальности нужно использовать более сложные алгоритмы, но для начала сойдет
//            var communities = new List<HashSet<string>>();

//            foreach (var edge in graph.Edges.OrderByDescending(e => e.MessageCount))
//            {
//                var existingCommunities = communities.Where(c => c.Contains(edge.FromCallsign) || c.Contains(edge.ToCallsign)).ToList();

//                if (existingCommunities.Count == 0)
//                {
//                    var newCommunity = new HashSet<string> { edge.FromCallsign, edge.ToCallsign };
//                    communities.Add(newCommunity);
//                }
//                else if (existingCommunities.Count == 1)
//                {
//                    existingCommunities[0].Add(edge.FromCallsign);
//                    existingCommunities[0].Add(edge.ToCallsign);
//                }
//                else
//                {
//                    // Объединяем сообщества
//                    var merged = new HashSet<string>();
//                    foreach (var comm in existingCommunities)
//                    {
//                        foreach (var callsign in comm)
//                            merged.Add(callsign);
//                    }
//                    merged.Add(edge.FromCallsign);
//                    merged.Add(edge.ToCallsign);

//                    // Удаляем старые и добавляем объединенное
//                    foreach (var comm in existingCommunities)
//                        communities.Remove(comm);
//                    communities.Add(merged);
//                }
//            }

//            return communities.Select(c => c.ToList()).ToList();
//        }
//    }
//}