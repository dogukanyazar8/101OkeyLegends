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


namespace _101OkeyLegends.Okey101Game.UI.Views
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly GameManager _gameManager;
        public ObservableCollection<Player> Players { get; set; }

        private ObservableCollection<Tile> _player1Tiles;
        public ObservableCollection<Tile> Player1Tiles
        {
            get => _player1Tiles;
            set => SetProperty(ref _player1Tiles, value);
        }

        private ObservableCollection<Tile> _player2Tiles;
        public ObservableCollection<Tile> Player2Tiles
        {
            get => _player2Tiles;
            set => SetProperty(ref _player2Tiles, value);
        }

        private ObservableCollection<Tile> _player3Tiles;
        public ObservableCollection<Tile> Player3Tiles
        {
            get => _player3Tiles;
            set => SetProperty(ref _player3Tiles, value);
        }

        private ObservableCollection<Tile> _player4Tiles;
        public ObservableCollection<Tile> Player4Tiles
        {
            get => _player4Tiles;
            set => SetProperty(ref _player4Tiles, value);
        }
        public RelayCommand StartGameCommand { get; }
        public MainWindowViewModel()
        {
            _gameManager = new GameManager();
            Players = new ObservableCollection<Player>();

            // Komut, oyunu başlatır ve oyuncu listesini günceller.
            StartGameCommand = new RelayCommand(StartGame);
        }
        private void StartGame()
        {
            // Örnek oyuncu isimleri; gerçek uygulamada kullanıcı girişi alınabilir.
            var playerNames = new[] { "Oyuncu_1", "Oyuncu_2", "Oyuncu_3", "Oyuncu_4" };
            _gameManager.InitializeGame(playerNames);

            // UI için oyuncu listesini güncelle
            Players.Clear();
            foreach (var player in _gameManager.Players)
            {
                Players.Add(player);
            }

            // Her oyuncunun elindeki taşları alfabetik olarak sıralı şekilde alıyoruz.
            // Sıralama için DisplayName özelliğini kullanıyoruz.
            if (_gameManager.Players.Count >= 4)
            {
                Player1Tiles = new ObservableCollection<Tile>(
                    _gameManager.Players[0].Hand.OrderBy(t => t.Color.ToString()).ThenBy(t => t.Number));
                Player2Tiles = new ObservableCollection<Tile>(
                    _gameManager.Players[1].Hand.OrderBy(t => t.Color.ToString()).ThenBy(t => t.Number));
                Player3Tiles = new ObservableCollection<Tile>(
                    _gameManager.Players[2].Hand.OrderBy(t => t.Color.ToString()).ThenBy(t => t.Number));
                Player4Tiles = new ObservableCollection<Tile>(
                    _gameManager.Players[3].Hand.OrderBy(t => t.Color.ToString()).ThenBy(t => t.Number));
            }

            // İsteğe bağlı olarak, gösterge ve joker taş bilgilerini de UI'a aktarabilirsiniz.
        }
    }
}