using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using _101OkeyLegends.Okey101Game.Core.Services;
using _101OkeyLegends.Okey101Game.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http;

namespace _101OkeyLegends.Okey101Game.UI.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<Tile> MyDeck { get; set; } = new ObservableCollection<Tile>();
        public ObservableCollection<Tile> RemainingTiles { get; set; } = new ObservableCollection<Tile>();
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

        // Seri dizim sonucu için
        public ObservableCollection<string> SeriesTiles { get; set; } = new ObservableCollection<string>();
        private int _seriesSum;
        public int SeriesSum
        {
            get => _seriesSum;
            set => SetProperty(ref _seriesSum, value);
        }

        // Çift dizim sonucu için
        public ObservableCollection<string> PairTiles { get; set; } = new ObservableCollection<string>();
        private int _pairSum;
        public int PairSum
        {
            get => _pairSum;
            set => SetProperty(ref _pairSum, value);
        }
        public bool CanStartGame => PlayerList.Count == 4;

        public RelayCommand ConnectCommand { get; }
        public RelayCommand StartGameCommand { get; }
        public RelayCommand SeriDizCommand { get; }
        public RelayCommand CiftDizCommand { get; }

        private HubConnection _connection;

        public MainWindowViewModel()
        {
            PlayerName = "Oyuncu1";
            IsGameStarted = false;
            ConnectCommand = new RelayCommand(async () => await ConnectToHub());
            StartGameCommand = new RelayCommand(async () => await ExecuteStartGame(), () => CanStartGame);
            SeriDizCommand = new RelayCommand(ExecuteSeriDiz);
            CiftDizCommand = new RelayCommand(ExecuteCiftDiz);



            // PlayerList güncellendiğinde CanStartGame bilgisini de tetikleyelim
            PlayerList.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanStartGame));
                StartGameCommand.NotifyCanExecuteChanged();
            };
        }

        private async Task ExecuteStartGame()
        {
            // Sunucuya oyunu başlat çağrısı yapıyoruz.
            await _connection.InvokeAsync("StartGameFromClient");
        }

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

            try
            {
                await _connection.StartAsync();
                await _connection.InvokeAsync("JoinGame", PlayerName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bağlantı hatası: {ex.Message}");
            }
        }

        //TODO: Bu metodu kontrol et!

        private void StartGame()
        {
            IsGameStarted = true;
            // Ek oyun başlangıç ayarlarını burada yapabilirsiniz.
        }
        private void ExecuteSeriDiz()
        {
            var gm = new GameManager();
            var result = gm.GetMaxSeriesPartitionWithJoker(MyDeck.ToList());

            SeriesTiles.Clear();
            if (result.bestPartition.Any())
            {
                foreach (var series in result.bestPartition)
                {
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
                    // Burada, çift olduğu bilgisini ekleyelim.
                    pairDesc += " (Çift)";
                    PairTiles.Add(pairDesc);
                }
                // Çift toplamı artık çift sayısının adedidir.
                PairSum = result.totalPairCount;
            }
            else
            {
                PairSum = 0;
            }
        }
    }
}