// WpfApp/ViewModels/InteractionGraphViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.WpfApp.Services;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RadioIntercepts.WpfApp.ViewModels
{
    public partial class InteractionGraphViewModel : ObservableObject
    {
        private readonly IGraphAnalysisService _graphService;
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<Area> _areas = new();

        [ObservableProperty]
        private ObservableCollection<string> _frequencies = new();

        [ObservableProperty]
        private DateTime? _dateFrom;

        [ObservableProperty]
        private DateTime? _dateTo;

        [ObservableProperty]
        private string? _selectedArea;

        [ObservableProperty]
        private string? _selectedFrequency;

        [ObservableProperty]
        private InteractionGraph? _graph;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableCollection<KeyPlayer> _keyPlayers = new();

        [ObservableProperty]
        private ObservableCollection<Community> _communities = new();

        [ObservableProperty]
        private ObservableCollection<BridgeNode> _bridgeNodes = new();

        [ObservableProperty]
        private string _graphInfo = string.Empty;

        public InteractionGraphViewModel(
            IGraphAnalysisService graphService,
            AppDbContext context)
        {
            _graphService = graphService;
            _context = context;
            DateTo = DateTime.Today;
            DateFrom = DateTime.Today.AddDays(-30);
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadFilterDataAsync();
            await BuildGraphAsync();
        }

        private async Task LoadFilterDataAsync()
        {
            try
            {
                var areasList = await _context.Areas
                    .AsNoTracking()
                    .OrderBy(a => a.Name)
                    .ToListAsync();

                Areas.Clear();
                foreach (var area in areasList)
                {
                    Areas.Add(area);
                }

                var frequenciesList = await _context.Frequencies
                    .AsNoTracking()
                    .OrderBy(f => f.Value)
                    .Select(f => f.Value)
                    .ToListAsync();

                Frequencies.Clear();
                foreach (var frequency in frequenciesList)
                {
                    Frequencies.Add(frequency);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки фильтров: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task BuildGraphAsync()
        {
            IsLoading = true;
            try
            {
                Graph = await _graphService.BuildInteractionGraphAsync(
                    DateFrom, DateTo, SelectedArea, SelectedFrequency);

                await LoadKeyPlayersAsync();
                await LoadCommunitiesAsync();
                await LoadBridgeNodesAsync();
                UpdateGraphInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка построения графа: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadKeyPlayersAsync()
        {
            try
            {
                var keyPlayersList = await _graphService.FindKeyPlayersAsync(15, 0.05);
                KeyPlayers.Clear();
                foreach (var player in keyPlayersList)
                {
                    var node = Graph?.Nodes.FirstOrDefault(n => n.Callsign == player);
                    KeyPlayers.Add(new KeyPlayer
                    {
                        Callsign = player,
                        Centrality = node?.Centrality ?? 0,
                        Degree = node?.Degree ?? 0,
                        MessageCount = node?.TotalMessages ?? 0
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ключевых игроков: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCommunitiesAsync()
        {
            try
            {
                var communitiesList = await _graphService.DetectCommunitiesAsync(3);
                Communities.Clear();

                int communityId = 1;
                foreach (var community in communitiesList.OrderByDescending(c => c.Count))
                {
                    Communities.Add(new Community
                    {
                        Id = communityId++,
                        Name = $"Группа {communityId - 1}",
                        Callsigns = string.Join(", ", community),
                        Size = community.Count,
                        CallsignList = community
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сообществ: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadBridgeNodesAsync()
        {
            try
            {
                var bridges = await _graphService.FindBridgesAsync();
                BridgeNodes.Clear();

                foreach (var bridge in bridges)
                {
                    var node = Graph?.Nodes.FirstOrDefault(n => n.Callsign == bridge);
                    BridgeNodes.Add(new BridgeNode
                    {
                        Callsign = bridge,
                        Degree = node?.Degree ?? 0,
                        ConnectedCommunities = EstimateConnectedCommunities(bridge)
                    });
                }
            }
            catch (Exception ex)
            {
                // Не критичная ошибка
                Console.WriteLine($"Ошибка загрузки bridge-узлов: {ex.Message}");
            }
        }

        private int EstimateConnectedCommunities(string callsign)
        {
            if (Graph == null) return 0;

            var node = Graph.Nodes.FirstOrDefault(n => n.Callsign == callsign);
            if (node == null) return 0;

            // Простая оценка: количество уникальных компонент связности среди соседей
            var neighbors = node.ConnectedEdges
                .Select(e => e.SourceCallsign == callsign ? e.TargetCallsign : e.SourceCallsign)
                .ToList();

            return neighbors.Distinct().Count();
        }

        private void UpdateGraphInfo()
        {
            if (Graph == null)
            {
                GraphInfo = "Граф не построен";
                return;
            }

            GraphInfo = $"Узлы: {Graph.Nodes.Count} | Ребра: {Graph.Edges.Count} | " +
                       $"Плотность: {CalculateDensity():F2} | " +
                       $"Средняя степень: {Graph.Nodes.Average(n => n.Degree):F1}";
        }

        private double CalculateDensity()
        {
            if (Graph == null || Graph.Nodes.Count < 2)
                return 0;

            int possibleEdges = Graph.Nodes.Count * (Graph.Nodes.Count - 1) / 2;
            return possibleEdges > 0 ? (double)Graph.Edges.Count / possibleEdges : 0;
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SelectedArea = null;
            SelectedFrequency = null;
            DateTo = DateTime.Today;
            DateFrom = DateTime.Today.AddDays(-30);
            _ = BuildGraphAsync();
        }

        [RelayCommand]
        private void ExportGraph(string format = "json")
        {
            if (Graph == null)
            {
                MessageBox.Show("Нет данных для экспорта", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string content = format.ToLower() switch
                {
                    "csv" => ExportToCsv(),
                    "json" => ExportToJson(),
                    "graphml" => ExportToGraphML(),
                    _ => ExportToJson()
                };

                // Сохранение в файл (упрощенно)
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = format.ToLower() switch
                    {
                        "csv" => "CSV файлы (*.csv)|*.csv",
                        "json" => "JSON файлы (*.json)|*.json",
                        "graphml" => "GraphML файлы (*.graphml)|*.graphml",
                        _ => "Все файлы (*.*)|*.*"
                    }
                };

                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialog.FileName, content);
                    MessageBox.Show($"Граф экспортирован в {dialog.FileName}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ExportToCsv()
        {
            var lines = new List<string> { "Source,Target,Weight,FirstInteraction,LastInteraction" };
            lines.AddRange(Graph!.Edges.Select(e =>
                $"{e.SourceCallsign},{e.TargetCallsign},{e.Weight},{e.FirstInteraction:yyyy-MM-dd HH:mm},{e.LastInteraction:yyyy-MM-dd HH:mm}"));
            return string.Join(Environment.NewLine, lines);
        }

        private string ExportToJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(Graph,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        private string ExportToGraphML()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns\">");
            sb.AppendLine("  <key id=\"weight\" for=\"edge\" attr.name=\"weight\" attr.type=\"int\"/>");
            sb.AppendLine("  <key id=\"centrality\" for=\"node\" attr.name=\"centrality\" attr.type=\"double\"/>");
            sb.AppendLine("  <key id=\"messages\" for=\"node\" attr.name=\"messages\" attr.type=\"int\"/>");
            sb.AppendLine("  <graph id=\"G\" edgedefault=\"undirected\">");

            foreach (var node in Graph!.Nodes)
            {
                sb.AppendLine($"    <node id=\"{node.Callsign}\">");
                sb.AppendLine($"      <data key=\"centrality\">{node.Centrality}</data>");
                sb.AppendLine($"      <data key=\"messages\">{node.TotalMessages}</data>");
                sb.AppendLine("    </node>");
            }

            foreach (var edge in Graph.Edges)
            {
                sb.AppendLine($"    <edge source=\"{edge.SourceCallsign}\" target=\"{edge.TargetCallsign}\">");
                sb.AppendLine($"      <data key=\"weight\">{edge.Weight}</data>");
                sb.AppendLine("    </edge>");
            }

            sb.AppendLine("  </graph>");
            sb.AppendLine("</graphml>");
            return sb.ToString();
        }
    }

    public class KeyPlayer
    {
        public string Callsign { get; set; } = null!;
        public double Centrality { get; set; }
        public int Degree { get; set; }
        public int MessageCount { get; set; }
        public string CentralityFormatted => $"{Centrality:P1}";
        public string Info => $"{Callsign} (степень: {Degree}, сообщений: {MessageCount})";
    }

    public class Community
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Callsigns { get; set; } = null!;
        public List<string> CallsignList { get; set; } = new();
        public int Size { get; set; }
    }

    public class BridgeNode
    {
        public string Callsign { get; set; } = null!;
        public int Degree { get; set; }
        public int ConnectedCommunities { get; set; }
        public string Info => $"{Callsign} (степень: {Degree}, соединяет {ConnectedCommunities} групп)";
    }
}