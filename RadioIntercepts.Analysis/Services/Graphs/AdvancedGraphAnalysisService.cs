// Application/Services/AdvancedGraphAnalysisService.cs
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
    public class AdvancedGraphAnalysisService : GraphAnalysisService, IAdvancedGraphAnalysisService
    {
        public AdvancedGraphAnalysisService(AppDbContext context, ICacheService cacheService = null)
            : base(context, cacheService)
        {
        }

        public async Task<List<CommunityAnalysis>> AnalyzeCommunitiesAsync(int minCommunitySize = 3)
        {
            var graph = await BuildInteractionGraphAsync();
            var communities = new List<CommunityAnalysis>();

            var communityGroups = graph.Nodes
                .Where(n => n.CommunityId != -1)
                .GroupBy(n => n.CommunityId)
                .Where(g => g.Count() >= minCommunitySize);

            foreach (var group in communityGroups)
            {
                var communityNodes = group.ToList();
                var callsigns = communityNodes.Select(n => n.Callsign).ToList();

                // Рассчитываем внутреннюю плотность сообщества
                var internalEdges = graph.Edges.Count(e =>
                    callsigns.Contains(e.SourceCallsign) &&
                    callsigns.Contains(e.TargetCallsign));

                var possibleInternalEdges = callsigns.Count * (callsigns.Count - 1) / 2;
                var internalDensity = possibleInternalEdges > 0
                    ? (double)internalEdges / possibleInternalEdges
                    : 0;

                var analysis = new CommunityAnalysis
                {
                    Id = group.Key,
                    Callsigns = callsigns,
                    InternalDensity = internalDensity,
                    AverageDegree = communityNodes.Average(n => n.Degree),
                    KeyPlayers = communityNodes
                        .OrderByDescending(n => n.Centrality)
                        .Take(Math.Max(3, callsigns.Count / 10))
                        .Select(n => n.Callsign)
                        .ToList()
                };

                communities.Add(analysis);
            }

            return communities.OrderByDescending(c => c.Size).ToList();
        }

        public async Task<Dictionary<string, double>> CalculateEigenvectorCentralityAsync()
        {
            var graph = await BuildInteractionGraphAsync();
            var adjacencyMatrix = BuildAdjacencyMatrix(graph);
            var n = graph.Nodes.Count;

            if (n == 0)
                return new Dictionary<string, double>();

            // Инициализация вектора центральности
            var centrality = new double[n];
            for (int i = 0; i < n; i++)
                centrality[i] = 1.0 / n;

            // Итерации степенного метода
            int maxIterations = 100;
            double tolerance = 1e-8;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                var newCentrality = new double[n];
                double norm = 0;

                for (int i = 0; i < n; i++)
                {
                    newCentrality[i] = 0;
                    for (int j = 0; j < n; j++)
                    {
                        newCentrality[i] += adjacencyMatrix[i, j] * centrality[j];
                    }
                    norm += newCentrality[i] * newCentrality[i];
                }

                norm = Math.Sqrt(norm);
                if (norm == 0) norm = 1;

                // Нормализация
                for (int i = 0; i < n; i++)
                {
                    newCentrality[i] /= norm;
                }

                // Проверка сходимости
                double diff = 0;
                for (int i = 0; i < n; i++)
                {
                    diff += Math.Abs(newCentrality[i] - centrality[i]);
                }

                centrality = newCentrality;

                if (diff < tolerance)
                    break;
            }

            // Преобразование в словарь
            var result = new Dictionary<string, double>();
            for (int i = 0; i < n; i++)
            {
                result[graph.Nodes[i].Callsign] = centrality[i];
            }

            return result;
        }

        private double[,] BuildAdjacencyMatrix(InteractionGraph graph)
        {
            var n = graph.Nodes.Count;
            var matrix = new double[n, n];
            var indexMap = graph.Nodes
                .Select((node, index) => new { node.Callsign, index })
                .ToDictionary(x => x.Callsign, x => x.index);

            // Заполняем матрицу смежности с весами
            foreach (var edge in graph.Edges)
            {
                var i = indexMap[edge.SourceCallsign];
                var j = indexMap[edge.TargetCallsign];
                matrix[i, j] = edge.Weight;
                matrix[j, i] = edge.Weight;
            }

            return matrix;
        }

        public async Task<List<KeyPlayerAnalysis>> FindKeyPlayersDetailedAsync(int topN = 10)
        {
            var graph = await BuildInteractionGraphAsync();
            var eigenvectorCentrality = await CalculateEigenvectorCentralityAsync();

            return graph.Nodes
                .Select(node => new KeyPlayerAnalysis
                {
                    Callsign = node.Callsign,
                    Centrality = node.Centrality,
                    DegreeCentrality = node.DegreeCentrality,
                    BetweennessCentrality = node.BetweennessCentrality,
                    ClosenessCentrality = node.ClosenessCentrality,
                    TotalMessages = node.TotalMessages,
                    Degree = node.Degree,
                    CommunityId = node.CommunityId
                })
                .OrderByDescending(kp => kp.Centrality)
                .ThenByDescending(kp => eigenvectorCentrality.GetValueOrDefault(kp.Callsign, 0))
                .Take(topN)
                .ToList();
        }
    }
}