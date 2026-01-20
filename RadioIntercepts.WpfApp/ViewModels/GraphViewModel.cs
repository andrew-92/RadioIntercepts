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
    public partial class GraphViewModel : ObservableObject
    {
        private readonly IGraphService _graphService;
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
        private Core.Models.InteractionGraph? _graph;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableCollection<string> _keyPlayers = new();

        [ObservableProperty]
        private ObservableCollection<Community> _communities = new();

        public GraphViewModel(IGraphService graphService, AppDbContext context)
        {
            _graphService = graphService;
            _context = context;
            DateTo = DateTime.Now;
            DateFrom = DateTime.Now.AddDays(-30);
            InitializeAsync().ConfigureAwait(false);
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
                MessageBox.Show($"Ошибка загрузки фильтров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task BuildGraphAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                Graph = await _graphService.BuildInteractionGraphAsync(DateFrom, DateTo, SelectedArea, SelectedFrequency);
                await LoadKeyPlayersAsync();
                await LoadCommunitiesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка построения графа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var keyPlayers = await _graphService.FindKeyPlayersAsync(10, DateFrom, DateTo);
                KeyPlayers.Clear();
                foreach (var player in keyPlayers)
                {
                    KeyPlayers.Add(player);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ключевых игроков: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCommunitiesAsync()
        {
            try
            {
                var communities = await _graphService.DetectCommunitiesAsync(DateFrom, DateTo);
                Communities.Clear();
                int i = 1;
                foreach (var community in communities.Where(c => c.Count > 2).OrderByDescending(c => c.Count))
                {
                    Communities.Add(new Community
                    {
                        Name = $"Сообщество {i++}",
                        Callsigns = community,
                        Size = community.Count
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сообществ: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ClearFilters()
        {
            SelectedArea = null;
            SelectedFrequency = null;
            DateTo = DateTime.Now;
            DateFrom = DateTime.Now.AddDays(-30);
            await BuildGraphAsync();
        }
    }

    public class Community
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Callsigns { get; set; } = new();
        public int Size { get; set; }
    }
}