using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Analysis.Interfaces.Services;
using RadioIntercepts.Analysis.Services.Graphs;
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
    public partial class GraphViewModel : ObservableObject
    {
        private readonly IAdvancedGraphAnalysisService _graphService;
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
        private ObservableCollection<KeyPlayerAnalysis> _keyPlayers = new();

        [ObservableProperty]
        private ObservableCollection<CommunityAnalysis> _communities = new();

        [ObservableProperty]
        private NetworkMetrics? _networkMetrics;

        [ObservableProperty]
        private ObservableCollection<string> _bridges = new();

        [ObservableProperty]
        private GraphNode? _selectedNode;

        [ObservableProperty]
        private CommunityAnalysis? _selectedCommunity;

        [ObservableProperty]
        private bool _showCommunities = true;

        [ObservableProperty]
        private bool _showCentrality = true;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private int _minCommunitySize = 3;

        [ObservableProperty]
        private int _topKeyPlayers = 10;

        [ObservableProperty]
        private double _minCentrality = 0.1;

        [ObservableProperty]
        private bool _useLouvainAlgorithm = true;

        // Добавляем ObservableCollection для отображения узлов
        private ObservableCollection<GraphNode> _displayNodes = new();
        public ObservableCollection<GraphNode> DisplayNodes => _displayNodes;

        public int TotalNodes => DisplayNodes.Count;
        public int TotalEdges => Graph?.Edges?.Count ?? 0;

        public GraphViewModel(IAdvancedGraphAnalysisService graphService, AppDbContext context)
        {
            _graphService = graphService;
            _context = context;
            DateTo = DateTime.Now;
            DateFrom = DateTime.Now.AddDays(-30);

            // Инициализируем пустой граф
            Graph = new InteractionGraph
            {
                Nodes = new List<GraphNode>(),
                Edges = new List<GraphEdge>()
            };

            // Загружаем только фильтры, граф не строим
            Task.Run(async () =>
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadFilterDataAsync();
                });
            });
        }

        // Этот метод уже есть в оригинальном коде - оставляем как есть
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
                StatusMessage = $"Ошибка загрузки фильтров: {ex.Message}";
                MessageBox.Show(StatusMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task BuildGraphAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            StatusMessage = "Построение графа...";

            try
            {
                // Построение графа с фильтрами
                Graph = await _graphService.BuildInteractionGraphAsync(
                    DateFrom, DateTo, SelectedArea, SelectedFrequency);

                // Обновление коллекции для отображения
                UpdateDisplayNodes();

                // Загрузка аналитики
                await LoadKeyPlayersAsync();
                await LoadCommunitiesAsync();
                await LoadNetworkMetricsAsync();
                await LoadBridgesAsync();

                StatusMessage = $"Граф построен: {Graph.Nodes.Count} узлов, {Graph.Edges.Count} связей";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка построения графа: {ex.Message}";
                MessageBox.Show(StatusMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Новый метод: обновляет DisplayNodes из Graph.Nodes
        private void UpdateDisplayNodes()
        {
            if (Graph == null) return;

            _displayNodes.Clear();

            // Копируем все узлы из Graph.Nodes (List) в DisplayNodes (ObservableCollection)
            foreach (var node in Graph.Nodes)
            {
                _displayNodes.Add(node);
            }

            // Уведомляем UI об изменениях
            OnPropertyChanged(nameof(DisplayNodes));
            OnPropertyChanged(nameof(TotalNodes));
            OnPropertyChanged(nameof(TotalEdges));
        }

        // Остальные методы оставляем без изменений
        private async Task LoadKeyPlayersAsync()
        {
            try
            {
                var keyPlayers = await _graphService.FindKeyPlayersDetailedAsync(TopKeyPlayers);

                KeyPlayers.Clear();
                foreach (var player in keyPlayers.Where(kp => kp.Centrality >= MinCentrality))
                {
                    KeyPlayers.Add(player);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки ключевых игроков: {ex.Message}";
                MessageBox.Show(StatusMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCommunitiesAsync()
        {
            try
            {
                var communities = await _graphService.AnalyzeCommunitiesAsync(MinCommunitySize);

                Communities.Clear();
                int i = 1;
                foreach (var community in communities.OrderByDescending(c => c.Size))
                {
                    Communities.Add(new CommunityAnalysis
                    {
                        Id = community.Id,
                        Callsigns = community.Callsigns,
                        InternalDensity = community.InternalDensity,
                        AverageDegree = community.AverageDegree,
                        KeyPlayers = community.KeyPlayers
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки сообществ: {ex.Message}";
                MessageBox.Show(StatusMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadNetworkMetricsAsync()
        {
            try
            {
                NetworkMetrics = await _graphService.CalculateNetworkMetricsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки метрик сети: {ex.Message}";
                MessageBox.Show(StatusMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadBridgesAsync()
        {
            try
            {
                var bridges = await _graphService.FindBridgesAsync();

                Bridges.Clear();
                foreach (var bridge in bridges)
                {
                    Bridges.Add(bridge);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки мостов: {ex.Message}";
                MessageBox.Show(StatusMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ClearFilters()
        {
            SelectedArea = null;
            SelectedFrequency = null;
            DateTo = DateTime.Now;
            DateFrom = DateTime.Now.AddDays(-30);
            MinCommunitySize = 3;
            TopKeyPlayers = 10;
            MinCentrality = 0.1;

            await BuildGraphAsync();
        }

        [RelayCommand]
        private async Task RefreshAnalysis()
        {
            await BuildGraphAsync();
        }

        [RelayCommand]
        private void ExportGraphData()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON файлы (*.json)|*.json|CSV файлы (*.csv)|*.csv",
                    DefaultExt = ".json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var graphData = new
                    {
                        Nodes = Graph?.Nodes,
                        Edges = Graph?.Edges,
                        KeyPlayers = KeyPlayers,
                        Communities = Communities,
                        Metrics = NetworkMetrics,
                        Bridges = Bridges,
                        Filters = new
                        {
                            DateFrom,
                            DateTo,
                            SelectedArea,
                            SelectedFrequency
                        }
                    };

                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(graphData,
                        Newtonsoft.Json.Formatting.Indented);

                    System.IO.File.WriteAllText(dialog.FileName, json);

                    StatusMessage = $"Данные экспортированы в {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка экспорта данных: {ex.Message}";
                MessageBox.Show(StatusMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ShowNodeDetails(GraphNode? node)
        {
            if (node == null) return;

            SelectedNode = node;

            // Поиск связей узла
            var connectedEdges = Graph?.Edges?
                .Where(e => e.SourceCallsign == node.Callsign || e.TargetCallsign == node.Callsign)
                .ToList() ?? new List<GraphEdge>();

            StatusMessage = $"Узел: {node.Callsign}. Связей: {connectedEdges.Count}. Центральность: {node.Centrality:F2}";
        }

        [RelayCommand]
        private void ShowCommunityDetails(CommunityAnalysis? community)
        {
            if (community == null) return;

            SelectedCommunity = community;
            StatusMessage = $"Сообщество {community.Id}: {community.Size} узлов, плотность: {community.InternalDensity:F2}";
        }

        [RelayCommand]
        private void ToggleCommunitiesVisibility()
        {
            ShowCommunities = !ShowCommunities;
        }

        [RelayCommand]
        private void ToggleCentralityVisibility()
        {
            ShowCentrality = !ShowCentrality;
        }
    }
}