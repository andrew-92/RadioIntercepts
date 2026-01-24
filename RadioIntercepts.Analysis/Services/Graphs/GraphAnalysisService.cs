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
        private readonly ICacheService _cacheService;

        public GraphAnalysisService(AppDbContext context, ICacheService cacheService = null)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<InteractionGraph> BuildInteractionGraphAsync(
            DateTime? startDate = null, DateTime? endDate = null,
            string? area = null, string? frequency = null)
        {
            var cacheKey = $"graph_{startDate}_{endDate}_{area}_{frequency}";

            if (_cacheService != null && _cacheService.TryGet(cacheKey, out InteractionGraph cachedGraph))
                return cachedGraph;

            var query = _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .Include(m => m.Frequency)
                .AsNoTracking()
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
            var graph = new InteractionGraph { Nodes = new List<GraphNode>(), Edges = new List<GraphEdge>() };

            if (!messages.Any())
                return graph;

            var nodeDict = new Dictionary<string, GraphNode>();
            var edgeDict = new Dictionary<string, GraphEdge>();

            foreach (var message in messages)
            {
                var callsigns = message.MessageCallsigns
                    .Select(mc => mc.Callsign.Name)
                    .Distinct()
                    .ToList();

                if (callsigns.Count < 2)
                    continue;

                // Добавляем/обновляем узлы
                foreach (var callsign in callsigns)
                {
                    if (!nodeDict.ContainsKey(callsign))
                    {
                        var node = new GraphNode
                        {
                            Callsign = callsign,
                            ConnectedEdges = new List<GraphEdge>(),
                            CommunityId = -1
                        };
                        nodeDict[callsign] = node;
                        graph.Nodes.Add(node);
                    }
                    nodeDict[callsign].TotalMessages++;
                }

                // Добавляем/обновляем ребра между всеми позывными в сообщении
                for (int i = 0; i < callsigns.Count; i++)
                {
                    for (int j = i + 1; j < callsigns.Count; j++)
                    {
                        var sortedCallsigns = new[] { callsigns[i], callsigns[j] }.OrderBy(c => c).ToList();
                        var key = $"{sortedCallsigns[0]}-{sortedCallsigns[1]}";

                        if (!edgeDict.ContainsKey(key))
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

                            // Связываем узлы с ребром
                            nodeDict[callsigns[i]].ConnectedEdges.Add(edge);
                            nodeDict[callsigns[j]].ConnectedEdges.Add(edge);
                        }
                        else
                        {
                            var edge = edgeDict[key];
                            edge.Weight++;
                            if (message.DateTime < edge.FirstInteraction)
                                edge.FirstInteraction = message.DateTime;
                            if (message.DateTime > edge.LastInteraction)
                                edge.LastInteraction = message.DateTime;
                        }
                    }
                }
            }

            // Рассчитываем метрики
            CalculateCentrality(graph);
            await DetectCommunitiesLouvainAsync(graph);

            if (_cacheService != null)
                _cacheService.Set(cacheKey, graph, TimeSpan.FromMinutes(30));

            return graph;
        }

        private void CalculateCentrality(InteractionGraph graph)
        {
            if (!graph.Nodes.Any()) return;

            // Степенная центральность
            var maxDegree = graph.Nodes.Max(n => n.Degree);
            foreach (var node in graph.Nodes)
            {
                node.DegreeCentrality = maxDegree > 0 ? (double)node.Degree / maxDegree : 0;
            }

            // Приближенная посредническая центральность (междуness centrality)
            CalculateBetweennessCentrality(graph);

            // Близостная центральность (closeness centrality)
            CalculateClosenessCentrality(graph);
        }

        private void CalculateBetweennessCentrality(InteractionGraph graph, int sampleSize = 100)
        {
            var nodes = graph.Nodes;
            var betweenness = new Dictionary<string, double>();

            foreach (var node in nodes)
                betweenness[node.Callsign] = 0;

            // Выборочный расчет для больших графов
            var random = new Random();
            var sampleNodes = nodes.Count > sampleSize
                ? nodes.OrderBy(x => random.Next()).Take(sampleSize).ToList()
                : nodes;

            foreach (var startNode in sampleNodes)
            {
                var stack = new Stack<string>();
                var pred = new Dictionary<string, List<string>>();
                var dist = new Dictionary<string, int>();
                var sigma = new Dictionary<string, double>();
                var queue = new Queue<string>();

                foreach (var node in nodes)
                {
                    pred[node.Callsign] = new List<string>();
                    dist[node.Callsign] = -1;
                    sigma[node.Callsign] = 0;
                }

                dist[startNode.Callsign] = 0;
                sigma[startNode.Callsign] = 1;
                queue.Enqueue(startNode.Callsign);

                while (queue.Count > 0)
                {
                    var v = queue.Dequeue();
                    stack.Push(v);

                    var vNode = nodes.First(n => n.Callsign == v);
                    foreach (var edge in vNode.ConnectedEdges)
                    {
                        var w = edge.SourceCallsign == v ? edge.TargetCallsign : edge.SourceCallsign;

                        if (dist[w] < 0)
                        {
                            dist[w] = dist[v] + 1;
                            queue.Enqueue(w);
                        }

                        if (dist[w] == dist[v] + 1)
                        {
                            sigma[w] += sigma[v];
                            pred[w].Add(v);
                        }
                    }
                }

                var delta = new Dictionary<string, double>();
                foreach (var node in nodes)
                    delta[node.Callsign] = 0;

                while (stack.Count > 0)
                {
                    var w = stack.Pop();
                    foreach (var v in pred[w])
                    {
                        delta[v] += (sigma[v] / sigma[w]) * (1 + delta[w]);
                    }
                    if (w != startNode.Callsign)
                    {
                        betweenness[w] += delta[w];
                    }
                }
            }

            // Нормализация
            var maxBetweenness = betweenness.Values.Max();
            foreach (var node in nodes)
            {
                node.BetweennessCentrality = maxBetweenness > 0
                    ? betweenness[node.Callsign] / maxBetweenness
                    : 0;
                node.Centrality = (node.DegreeCentrality + node.BetweennessCentrality) / 2;
            }
        }

        private void CalculateClosenessCentrality(InteractionGraph graph)
        {
            var nodes = graph.Nodes;

            foreach (var node in nodes)
            {
                var distances = new Dictionary<string, int>();
                var visited = new HashSet<string>();
                var queue = new Queue<(string, int)>();

                queue.Enqueue((node.Callsign, 0));
                visited.Add(node.Callsign);

                while (queue.Count > 0)
                {
                    var (current, distance) = queue.Dequeue();
                    distances[current] = distance;

                    var currentNode = nodes.First(n => n.Callsign == current);
                    foreach (var edge in currentNode.ConnectedEdges)
                    {
                        var neighbor = edge.SourceCallsign == current
                            ? edge.TargetCallsign
                            : edge.SourceCallsign;

                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue((neighbor, distance + 1));
                        }
                    }
                }

                var totalDistance = distances.Values.Sum();
                node.ClosenessCentrality = distances.Count > 1
                    ? (distances.Count - 1) / (double)totalDistance
                    : 0;
            }

            // Нормализация
            var maxCloseness = nodes.Max(n => n.ClosenessCentrality);
            if (maxCloseness > 0)
            {
                foreach (var node in nodes)
                {
                    node.ClosenessCentrality /= maxCloseness;
                }
            }
        }

        public async Task<List<string>> FindKeyPlayersAsync(int topN = 10, double minCentrality = 0.1)
        {
            var graph = await BuildInteractionGraphAsync();
            return graph.Nodes
                .Where(n => n.Centrality >= minCentrality)
                .OrderByDescending(n => n.Centrality)
                .ThenByDescending(n => n.BetweennessCentrality)
                .ThenByDescending(n => n.TotalMessages)
                .Take(topN)
                .Select(n => n.Callsign)
                .ToList();
        }

        public async Task<List<List<string>>> DetectCommunitiesAsync(int minCommunitySize = 3)
        {
            var graph = await BuildInteractionGraphAsync();
            return DetectConnectedComponents(graph, minCommunitySize);
        }

        private List<List<string>> DetectConnectedComponents(InteractionGraph graph, int minCommunitySize)
        {
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

        private async Task DetectCommunitiesLouvainAsync(InteractionGraph graph)
        {
            if (graph.Nodes.Count < 10)
            {
                // Для маленьких графов используем простой алгоритм
                var components = DetectConnectedComponents(graph, 1);
                for (int i = 0; i < components.Count; i++)
                {
                    foreach (var callsign in components[i])
                    {
                        var node = graph.Nodes.First(n => n.Callsign == callsign);
                        node.CommunityId = i;
                    }
                }
                return;
            }

            // Реализация алгоритма Лувена (упрощенная)
            var communities = new Dictionary<string, int>();
            var nodeToCommunity = new Dictionary<string, int>();
            var communityNodes = new Dictionary<int, List<string>>();

            // Инициализация: каждый узел в своем сообществе
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                var node = graph.Nodes[i];
                node.CommunityId = i;
                nodeToCommunity[node.Callsign] = i;
                communityNodes[i] = new List<string> { node.Callsign };
            }

            bool improved;
            int iteration = 0;
            do
            {
                improved = false;
                var randomOrder = graph.Nodes.OrderBy(x => Guid.NewGuid()).ToList();

                foreach (var node in randomOrder)
                {
                    var bestCommunity = node.CommunityId;
                    var bestModularityGain = 0.0;

                    var neighborCommunities = GetNeighborCommunities(node, graph, nodeToCommunity);

                    foreach (var community in neighborCommunities.Distinct())
                    {
                        if (community == node.CommunityId) continue;

                        var modularityGain = CalculateModularityGain(node, community, graph, nodeToCommunity, communityNodes);

                        if (modularityGain > bestModularityGain)
                        {
                            bestModularityGain = modularityGain;
                            bestCommunity = community;
                        }
                    }

                    if (bestCommunity != node.CommunityId && bestModularityGain > 0)
                    {
                        // Перемещаем узел в лучшее сообщество
                        var oldCommunity = node.CommunityId;
                        communityNodes[oldCommunity].Remove(node.Callsign);

                        node.CommunityId = bestCommunity;
                        nodeToCommunity[node.Callsign] = bestCommunity;
                        communityNodes[bestCommunity].Add(node.Callsign);

                        improved = true;
                    }
                }

                iteration++;
            } while (improved && iteration < 10); // Ограничиваем количество итераций
        }

        private HashSet<int> GetNeighborCommunities(GraphNode node, InteractionGraph graph, Dictionary<string, int> nodeToCommunity)
        {
            var communities = new HashSet<int>();

            foreach (var edge in node.ConnectedEdges)
            {
                var neighbor = edge.SourceCallsign == node.Callsign
                    ? edge.TargetCallsign
                    : edge.SourceCallsign;

                if (nodeToCommunity.TryGetValue(neighbor, out int community))
                {
                    communities.Add(community);
                }
            }

            return communities;
        }

        private double CalculateModularityGain(GraphNode node, int newCommunity,
            InteractionGraph graph, Dictionary<string, int> nodeToCommunity,
            Dictionary<int, List<string>> communityNodes)
        {
            // Упрощенный расчет прироста модулярности
            var ki = node.Degree;
            var ki_in = 0; // Вес связей с узлами в целевом сообществе

            foreach (var edge in node.ConnectedEdges)
            {
                var neighbor = edge.SourceCallsign == node.Callsign
                    ? edge.TargetCallsign
                    : edge.SourceCallsign;

                if (nodeToCommunity.TryGetValue(neighbor, out int neighborCommunity) &&
                    neighborCommunity == newCommunity)
                {
                    ki_in += edge.Weight;
                }
            }

            var sum_tot = communityNodes[newCommunity]
                .SelectMany(c => graph.Nodes.First(n => n.Callsign == c).ConnectedEdges)
                .Sum(e => e.Weight) / 2.0;

            var m = graph.Edges.Sum(e => e.Weight) / 2.0;

            if (m == 0) return 0;

            return (ki_in - sum_tot * ki / m) / m;
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
            return graph.Nodes.ToDictionary(
                n => n.Callsign,
                n => new
                {
                    n.Centrality,
                    n.DegreeCentrality,
                    n.BetweennessCentrality,
                    n.ClosenessCentrality
                }.Centrality);
        }

        public async Task<List<string>> FindBridgesAsync()
        {
            var graph = await BuildInteractionGraphAsync();
            return FindArticulationPoints(graph);
        }

        private List<string> FindArticulationPoints(InteractionGraph graph)
        {
            var articulationPoints = new HashSet<string>();
            var visited = new HashSet<string>();
            var discoveryTime = new Dictionary<string, int>();
            var lowTime = new Dictionary<string, int>();
            var parent = new Dictionary<string, string>();
            var time = 0;

            foreach (var node in graph.Nodes)
            {
                if (!visited.Contains(node.Callsign))
                {
                    DFSArticulation(node.Callsign, graph, visited, discoveryTime, lowTime, parent, ref time, articulationPoints);
                }
            }

            return articulationPoints.ToList();
        }

        private void DFSArticulation(string u, InteractionGraph graph, HashSet<string> visited,
            Dictionary<string, int> discoveryTime, Dictionary<string, int> lowTime,
            Dictionary<string, string> parent, ref int time, HashSet<string> articulationPoints)
        {
            visited.Add(u);
            discoveryTime[u] = lowTime[u] = ++time;
            var children = 0;

            var uNode = graph.Nodes.First(n => n.Callsign == u);

            foreach (var edge in uNode.ConnectedEdges)
            {
                var v = edge.SourceCallsign == u ? edge.TargetCallsign : edge.SourceCallsign;

                if (!visited.Contains(v))
                {
                    children++;
                    parent[v] = u;
                    DFSArticulation(v, graph, visited, discoveryTime, lowTime, parent, ref time, articulationPoints);

                    lowTime[u] = Math.Min(lowTime[u], lowTime[v]);

                    // u - точка сочленения если:
                    // 1. u - корень и имеет >= 2 детей
                    if (!parent.ContainsKey(u) && children > 1)
                        articulationPoints.Add(u);

                    // 2. u - не корень и lowTime[v] >= discoveryTime[u]
                    if (parent.ContainsKey(u) && lowTime[v] >= discoveryTime[u])
                        articulationPoints.Add(u);
                }
                else if (v != (parent.ContainsKey(u) ? parent[u] : null))
                {
                    lowTime[u] = Math.Min(lowTime[u], discoveryTime[v]);
                }
            }
        }

        // Новые методы для расширенного анализа
        public async Task<NetworkMetrics> CalculateNetworkMetricsAsync()
        {
            var graph = await BuildInteractionGraphAsync();
            var metrics = new NetworkMetrics();

            if (!graph.Nodes.Any())
                return metrics;

            // Основные метрики сети
            metrics.NodeCount = graph.Nodes.Count;
            metrics.EdgeCount = graph.Edges.Count;
            metrics.AverageDegree = graph.Nodes.Average(n => n.Degree);
            metrics.Density = (2.0 * metrics.EdgeCount) / (metrics.NodeCount * (metrics.NodeCount - 1));

            // Диаметр и средняя длина пути (приближенно)
            metrics.AveragePathLength = CalculateAveragePathLength(graph);

            // Коэффициент кластеризации
            metrics.ClusteringCoefficient = CalculateClusteringCoefficient(graph);

            // Модулярность (качество разбиения на сообщества)
            metrics.Modularity = CalculateModularity(graph);

            return metrics;
        }

        private double CalculateAveragePathLength(InteractionGraph graph)
        {
            if (graph.Nodes.Count < 2) return 0;

            var totalDistance = 0;
            var pairs = 0;

            // Выборочный расчет для больших графов
            var sampleSize = Math.Min(100, graph.Nodes.Count);
            var random = new Random();
            var sampleNodes = graph.Nodes.OrderBy(x => random.Next()).Take(sampleSize).ToList();

            foreach (var startNode in sampleNodes)
            {
                var distances = CalculateDistancesFromNode(startNode.Callsign, graph);
                totalDistance += distances.Values.Sum();
                pairs += distances.Count - 1; // Исключаем расстояние до себя
            }

            return pairs > 0 ? (double)totalDistance / pairs : 0;
        }

        private Dictionary<string, int> CalculateDistancesFromNode(string startCallsign, InteractionGraph graph)
        {
            var distances = new Dictionary<string, int>();
            var visited = new HashSet<string>();
            var queue = new Queue<(string, int)>();

            queue.Enqueue((startCallsign, 0));
            visited.Add(startCallsign);

            while (queue.Count > 0)
            {
                var (current, distance) = queue.Dequeue();
                distances[current] = distance;

                var currentNode = graph.Nodes.First(n => n.Callsign == current);
                foreach (var edge in currentNode.ConnectedEdges)
                {
                    var neighbor = edge.SourceCallsign == current
                        ? edge.TargetCallsign
                        : edge.SourceCallsign;

                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, distance + 1));
                    }
                }
            }

            return distances;
        }

        private double CalculateClusteringCoefficient(InteractionGraph graph)
        {
            double totalCoefficient = 0;
            int nodesWithNeighbors = 0;

            foreach (var node in graph.Nodes)
            {
                var neighbors = GetNeighbors(node.Callsign, graph);
                var neighborCount = neighbors.Count;

                if (neighborCount < 2)
                {
                    totalCoefficient += 0;
                    continue;
                }

                // Считаем связи между соседями
                int connectionsBetweenNeighbors = 0;
                for (int i = 0; i < neighborCount; i++)
                {
                    for (int j = i + 1; j < neighborCount; j++)
                    {
                        if (AreConnected(neighbors[i], neighbors[j], graph))
                        {
                            connectionsBetweenNeighbors++;
                        }
                    }
                }

                var possibleConnections = neighborCount * (neighborCount - 1) / 2;
                var coefficient = possibleConnections > 0
                    ? (double)connectionsBetweenNeighbors / possibleConnections
                    : 0;

                totalCoefficient += coefficient;
                nodesWithNeighbors++;
            }

            return nodesWithNeighbors > 0 ? totalCoefficient / nodesWithNeighbors : 0;
        }

        private List<string> GetNeighbors(string callsign, InteractionGraph graph)
        {
            var node = graph.Nodes.First(n => n.Callsign == callsign);
            return node.ConnectedEdges
                .Select(e => e.SourceCallsign == callsign ? e.TargetCallsign : e.SourceCallsign)
                .Distinct()
                .ToList();
        }

        private bool AreConnected(string callsign1, string callsign2, InteractionGraph graph)
        {
            var node1 = graph.Nodes.First(n => n.Callsign == callsign1);
            return node1.ConnectedEdges.Any(e =>
                (e.SourceCallsign == callsign1 && e.TargetCallsign == callsign2) ||
                (e.SourceCallsign == callsign2 && e.TargetCallsign == callsign1));
        }

        private double CalculateModularity(InteractionGraph graph)
        {
            if (graph.Nodes.All(n => n.CommunityId == -1))
                return 0;

            var m = graph.Edges.Sum(e => e.Weight) / 2.0;
            if (m == 0) return 0;

            var communities = graph.Nodes
                .GroupBy(n => n.CommunityId)
                .Where(g => g.Key != -1)
                .ToList();

            double modularity = 0;

            foreach (var community in communities)
            {
                var communityNodes = community.Select(n => n.Callsign).ToHashSet();
                double l_c = 0; // Сумма весов ребер внутри сообщества
                double d_c = 0; // Сумма степеней узлов сообщества

                foreach (var node in community)
                {
                    d_c += node.Degree;

                    foreach (var edge in node.ConnectedEdges)
                    {
                        var neighbor = edge.SourceCallsign == node.Callsign
                            ? edge.TargetCallsign
                            : edge.SourceCallsign;

                        if (communityNodes.Contains(neighbor))
                        {
                            l_c += edge.Weight;
                        }
                    }
                }

                l_c /= 2; // Каждое ребро посчитано дважды
                modularity += (l_c / m) - Math.Pow(d_c / (2 * m), 2);
            }

            return modularity;
        }
    }

    public class NetworkMetrics
    {
        public int NodeCount { get; set; }
        public int EdgeCount { get; set; }
        public double AverageDegree { get; set; }
        public double Density { get; set; }
        public double AveragePathLength { get; set; }
        public double ClusteringCoefficient { get; set; }
        public double Modularity { get; set; }
    }
}