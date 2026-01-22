// Application/Services/GraphAnalysisService.cs
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.Analysis.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Services.Graphs
{
    public class GraphAnalysisService : IGraphAnalysisService
    {
        private readonly AppDbContext _context;

        public GraphAnalysisService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<InteractionGraph> BuildInteractionGraphAsync(
            DateTime? startDate = null, DateTime? endDate = null,
            string? area = null, string? frequency = null)
        {
            var query = _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .Include(m => m.Frequency)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(m => m.DateTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(m => m.DateTime <= endDate.Value);
            if (!string.IsNullOrEmpty(area))
                query = query.Where(m => m.Area.Name == area);
            if (!string.IsNullOrEmpty(frequency))
                query = query.Where(m => m.Frequency.Value == frequency);

            var messages = await query.ToListAsync();
            var graph = new InteractionGraph();
            var edgeDict = new Dictionary<string, GraphEdge>();

            foreach (var message in messages)
            {
                var callsigns = message.MessageCallsigns
                    .Select(mc => mc.Callsign.Name)
                    .Distinct()
                    .ToList();

                // Добавляем узлы
                foreach (var callsign in callsigns)
                {
                    var node = graph.Nodes.FirstOrDefault(n => n.Callsign == callsign);
                    if (node == null)
                    {
                        node = new GraphNode { Callsign = callsign };
                        graph.Nodes.Add(node);
                    }
                    node.TotalMessages++;
                }

                // Добавляем ребра между всеми позывными в сообщении
                for (int i = 0; i < callsigns.Count; i++)
                {
                    for (int j = i + 1; j < callsigns.Count; j++)
                    {
                        var key = $"{callsigns[i]}-{callsigns[j]}";
                        var reverseKey = $"{callsigns[j]}-{callsigns[i]}";

                        if (!edgeDict.ContainsKey(key) && !edgeDict.ContainsKey(reverseKey))
                        {
                            var edge = new GraphEdge
                            {
                                SourceCallsign = callsigns[i],
                                TargetCallsign = callsigns[j],
                                FirstInteraction = message.DateTime,
                                LastInteraction = message.DateTime,
                                Weight = 1
                            };
                            edgeDict[key] = edge;
                            graph.Edges.Add(edge);
                        }
                        else
                        {
                            var existingKey = edgeDict.ContainsKey(key) ? key : reverseKey;
                            var edge = edgeDict[existingKey];
                            edge.Weight++;
                            if (message.DateTime < edge.FirstInteraction)
                                edge.FirstInteraction = message.DateTime;
                            if (message.DateTime > edge.LastInteraction)
                                edge.LastInteraction = message.DateTime;
                        }
                    }
                }
            }

            // Связываем узлы с ребрами
            foreach (var edge in graph.Edges)
            {
                var sourceNode = graph.Nodes.First(n => n.Callsign == edge.SourceCallsign);
                var targetNode = graph.Nodes.First(n => n.Callsign == edge.TargetCallsign);

                sourceNode.ConnectedEdges.Add(edge);
                targetNode.ConnectedEdges.Add(edge);
            }

            // Рассчитываем центральность (степенная)
            CalculateCentrality(graph);

            return graph;
        }

        private void CalculateCentrality(InteractionGraph graph)
        {
            var maxDegree = graph.Nodes.Max(n => n.Degree);

            foreach (var node in graph.Nodes)
            {
                // Нормализованная степень центральности
                node.Centrality = maxDegree > 0 ? (double)node.Degree / maxDegree : 0;

                // Можно добавить другие метрики:
                // - Betweenness centrality
                // - Closeness centrality
                // - Eigenvector centrality
            }
        }

        public async Task<List<string>> FindKeyPlayersAsync(int topN = 10, double minCentrality = 0.1)
        {
            var graph = await BuildInteractionGraphAsync();
            return graph.Nodes
                .Where(n => n.Centrality >= minCentrality)
                .OrderByDescending(n => n.Centrality)
                .ThenByDescending(n => n.TotalMessages)
                .Take(topN)
                .Select(n => n.Callsign)
                .ToList();
        }

        public async Task<List<List<string>>> DetectCommunitiesAsync(int minCommunitySize = 3)
        {
            var graph = await BuildInteractionGraphAsync();
            var communities = new List<List<string>>();
            var visited = new HashSet<string>();

            foreach (var node in graph.Nodes)
            {
                if (!visited.Contains(node.Callsign))
                {
                    var community = new List<string>();
                    DFS(node.Callsign, graph, visited, community);

                    if (community.Count >= minCommunitySize)
                    {
                        communities.Add(community);
                    }
                }
            }

            return communities;
        }

        private void DFS(string callsign, InteractionGraph graph, HashSet<string> visited, List<string> community)
        {
            visited.Add(callsign);
            community.Add(callsign);

            var node = graph.Nodes.First(n => n.Callsign == callsign);
            foreach (var edge in node.ConnectedEdges)
            {
                var neighbor = edge.SourceCallsign == callsign ? edge.TargetCallsign : edge.SourceCallsign;
                if (!visited.Contains(neighbor))
                {
                    DFS(neighbor, graph, visited, community);
                }
            }
        }

        public async Task<Dictionary<string, double>> CalculateCentralityScoresAsync()
        {
            var graph = await BuildInteractionGraphAsync();
            return graph.Nodes.ToDictionary(n => n.Callsign, n => n.Centrality);
        }

        public async Task<List<string>> FindBridgesAsync()
        {
            // Находим позывные, которые соединяют разные сообщества
            var graph = await BuildInteractionGraphAsync();
            var bridges = new List<string>();

            foreach (var node in graph.Nodes)
            {
                if (node.ConnectedEdges.Count >= 2)
                {
                    // Проверяем, соединяет ли этот узел разные кластеры
                    var neighborCommunities = new HashSet<int>();
                    foreach (var edge in node.ConnectedEdges)
                    {
                        var neighbor = edge.SourceCallsign == node.Callsign ? edge.TargetCallsign : edge.SourceCallsign;
                        // Упрощенная проверка - если соседи не связаны между собой
                        var neighborNode = graph.Nodes.First(n => n.Callsign == neighbor);
                        var commonConnections = node.ConnectedEdges
                            .Select(e => e.SourceCallsign == node.Callsign ? e.TargetCallsign : e.SourceCallsign)
                            .Intersect(neighborNode.ConnectedEdges
                                .Select(e => e.SourceCallsign == neighbor ? e.TargetCallsign : e.SourceCallsign))
                            .Count();

                        if (commonConnections == 0)
                        {
                            bridges.Add(node.Callsign);
                            break;
                        }
                    }
                }
            }

            return bridges.Distinct().ToList();
        }
    }
}