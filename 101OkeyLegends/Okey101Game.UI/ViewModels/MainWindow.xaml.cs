using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using _101OkeyLegends.Okey101Game.Core.Services;
using _101OkeyLegends.Okey101Game.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;

namespace _101OkeyLegends.Okey101Game.UI.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        #region Fields & Properties

        private HubConnection _connection;

        // Oyun alanındaki taşlar
        public ObservableCollection<Tile> MyDeck { get; set; } = new ObservableCollection<Tile>();
        public ObservableCollection<Tile> RemainingTiles { get; set; } = new ObservableCollection<Tile>();

        // Oyuncu listesi
        public ObservableCollection<string> PlayerList { get; set; } = new ObservableCollection<string>();

        private string _playerName;
        public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
        }

        private bool _isGameStarted;
        public bool IsGameStarted
        {
            get => _isGameStarted;
            set => SetProperty(ref _isGameStarted, value);
        }

        // Seri (per) dizme sonuçları
        public ObservableCollection<string> SeriesTiles { get; set; } = new ObservableCollection<string>();
        private int _seriesSum;
        public int SeriesSum
        {
            get => _seriesSum;
            set => SetProperty(ref _seriesSum, value);
        }

        // Çift dizme sonuçları
        public ObservableCollection<string> PairTiles { get; set; } = new ObservableCollection<string>();
        private int _pairSum;
        public int PairSum
        {
            get => _pairSum;
            set => SetProperty(ref _pairSum, value);
        }

        private int _remainingTileCount;
        public int RemainingTileCount
        {
            get => _remainingTileCount;
            set => SetProperty(ref _remainingTileCount, value);
        }

        private string _jokerTileInfo;
        public string JokerTileInfo
        {
            get => _jokerTileInfo;
            set => SetProperty(ref _jokerTileInfo, value);
        }


        // Oyun başlaması için oyuncu sayısının kontrolü (örneğin 4 oyuncu)
        public bool CanStartGame => PlayerList.Count == 4;

        #endregion

        #region Commands

        public RelayCommand ConnectCommand { get; }
        public RelayCommand StartGameCommand { get; }
        public RelayCommand SeriDizCommand { get; }
        public RelayCommand CiftDizCommand { get; }

        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            PlayerName = "Oyuncu1";
            IsGameStarted = false;

            // Komutlar asenkron ya da senkron olarak tanımlanıyor.
            ConnectCommand = new RelayCommand(async () => await ConnectToHub());
            StartGameCommand = new RelayCommand(async () => await ExecuteStartGame(), () => CanStartGame);
            SeriDizCommand = new RelayCommand(ExecuteSeriDiz);
            CiftDizCommand = new RelayCommand(ExecuteCiftDiz);

            // Oyuncu listesi değiştiğinde CanStartGame kontrolünü tetikleyelim.
            PlayerList.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanStartGame));
                StartGameCommand.NotifyCanExecuteChanged();
            };
        }

        #endregion

        #region Connection & Game Start Methods

        private async Task ConnectToHub()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/gamehub")
                .WithAutomaticReconnect()
                .Build();

            _connection.On<string[]>("UpdatePlayerList", (players) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PlayerList.Clear();
                    foreach (var p in players)
                        PlayerList.Add(p);
                });
            });

            _connection.On<Tile[]>("ReceiveDeck", (deck) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MyDeck.Clear();
                    foreach (var tile in deck)
                        MyDeck.Add(tile);
                });
            });

            _connection.On<Tile[]>("ReceiveRemainingTiles", (tiles) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RemainingTiles.Clear();
                    foreach (var tile in tiles)
                        RemainingTiles.Add(tile);
                });
            });

            _connection.On("GameStarted", () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsGameStarted = true;
                });
            });

            _connection.On<Tile[]>("ReceiveRemainingTiles", (tiles) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RemainingTiles.Clear();
                    foreach (var tile in tiles)
                        RemainingTiles.Add(tile);
                    RemainingTileCount = RemainingTiles.Count;
                });
            });

            _connection.On<string>("UpdateJokerTile", (jokerInfo) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    JokerTileInfo = jokerInfo;
                });
            });

            try
            {
                await _connection.StartAsync();
                await _connection.InvokeAsync("JoinGame", PlayerName);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Bağlantı hatası: {ex.Message}");
            }
        }

        private async Task ExecuteStartGame()
        {
            await _connection.InvokeAsync("StartGameFromClient");
        }

        #endregion

        #region Command Execution Methods

        /// <summary>
        /// Elindeki taşlardan Joker global düzeyde yalnızca 1 kullanılarak maksimum seri partition'ı oluşturur.
        /// Sonuç olarak, oluşturulan seriler ve toplam seri değeri hesaplanır.
        /// </summary>
        private void ExecuteSeriDiz()
        {
            var gm = new GameManager();
            var result = gm.GetMaxSeriesPartitionWithJoker(MyDeck.ToList());

            SeriesTiles.Clear();
            if (result.bestPartition.Any())
            {
                foreach (var series in result.bestPartition)
                {
                    // Her bir seriyi string formatında oluşturuyoruz.
                    string seriesDesc = string.Join(" - ", series.Select(t =>
                        t.IsJoker ? $"Okey (as {t.Color} {t.Number})" : $"{t.Color} {t.Number}"));
                    seriesDesc += $" (Toplam: {series.Sum(t => t.Number)})";
                    SeriesTiles.Add(seriesDesc);
                }
                SeriesSum = result.totalSum;
            }
            else
            {
                SeriesSum = 0;
            }
        }

        /// <summary>
        /// Elindeki taşlardan aynı (Color, Number) çiftlerini oluşturur.
        /// Eğer çift oluşturulamıyorsa ve Joker varsa, Joker kullanılarak çift tamamlanır.
        /// Toplam, oluşturulan çift sayısına eşittir.
        /// </summary>
        private void ExecuteCiftDiz()
        {
            var gm = new GameManager();
            var result = gm.GetMaxPairPartitionWithJoker(MyDeck.ToList());

            PairTiles.Clear();
            if (result.bestPairPartition.Any())
            {
                foreach (var pair in result.bestPairPartition)
                {
                    string pairDesc = string.Join(" - ", pair.Select(t =>
                        t.IsJoker ? $"Okey (as {t.Color} {t.Number})" : $"{t.Color} {t.Number}"));
                    pairDesc += " (Çift)";
                    PairTiles.Add(pairDesc);
                }
                // PairSum, oluşturulan çiftlerin toplam adedini temsil ediyor.
                PairSum = result.totalPairCount;
            }
            else
            {
                PairSum = 0;
            }
        }

        #endregion
    }
}
