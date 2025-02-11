using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _101OkeyLegends.Okey101Game.Domain.Models;

namespace _101OkeyLegends.Okey101Game.Core.Services
{
    public class GameManager
    {
        public List<Tile> Tiles { get; private set; }
        public List<Player> Players { get; private set; }
        public Tile JokerTile { get; private set; }
        public Tile IndicatorTile { get; private set; }
        private readonly Random _random = new Random();

        public GameManager()
        {
            Tiles = new List<Tile>();
            Players = new List<Player>();
        }

        /// <summary>
        /// Oyunu başlatır: taşları oluşturur, karıştırır, gösterge ve joker taşını belirler ve oyunculara dağıtır.
        /// </summary>
        /// <param name="playerNames">Oyuncu isimleri listesi</param>
        public void InitializeGame(IEnumerable<string> playerNames)
        {
            CreateTiles();
            ShuffleTiles();
            DetermineIndicatorAndJoker();
            CreatePlayers(playerNames);
            DistributeTiles();
        }

        private void CreateTiles()
        {
            Tiles.Clear();
            // Her renkten 1-13 arası 2 adet taş ekleniyor.
            foreach (TileColor color in Enum.GetValues(typeof(TileColor)))
            {
                for (int i = 1; i <= 13; i++)
                {
                    Tiles.Add(new Tile(color, i));
                    Tiles.Add(new Tile(color, i));
                }
            }
            // İhtiyaca göre ekstra taş (sahte okey gibi) eklenebilir.
        }

        private void ShuffleTiles()
        {
            // Basit bir karıştırma algoritması (Fisher-Yates)
            int n = Tiles.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                (Tiles[k], Tiles[n]) = (Tiles[n], Tiles[k]);
            }
        }

        private void DetermineIndicatorAndJoker()
        {
            // Göstergesi, destedeki orta konumdaki taş olarak belirleniyor.
            int indicatorIndex = Tiles.Count / 2;
            IndicatorTile = Tiles[indicatorIndex];

            // Joker taş: Göstergeden sonraki sayıyı alır (13 ise 1 olarak döner)
            int jokerNumber = IndicatorTile.Number == 13 ? 1 : IndicatorTile.Number + 1;

            // İlgili renk ve numaraya sahip tüm taşları buluyoruz.
            var jokerTiles = Tiles.Where(t => t.Color == IndicatorTile.Color && t.Number == jokerNumber).ToList();

            if (jokerTiles.Any())
            {
                // Bulunan tüm taşlar Joker olarak işaretleniyor.
                foreach (var tile in jokerTiles)
                {
                    tile.IsJoker = true;
                }

                // İhtiyaç duyuluyorsa JokerTile propertysine ilk bulunan joker taş atanabilir.
                JokerTile = jokerTiles.First();
            }
        }

        private void CreatePlayers(IEnumerable<string> playerNames)
        {
            Players.Clear();
            foreach (var name in playerNames)
            {
                Players.Add(new Player(name));
            }
        }

        private void DistributeTiles()
        {
            // Örnek dağıtım: 4 oyuncu varsa, 1 oyuncuya 22, diğerlerine 21 taş dağıtılır.
            int firstPlayerTiles = 22;
            int otherPlayerTiles = 21;

            // Oyuncuları sırayla dolaşıp ellerine taş ekliyoruz.
            int tileIndex = 0;
            for (int i = 0; i < Players.Count; i++)
            {
                int tilesToDeal = i == 0 ? firstPlayerTiles : otherPlayerTiles;
                for (int j = 0; j < tilesToDeal; j++)
                {
                    if (tileIndex < Tiles.Count)
                    {
                        Players[i].Hand.Add(Tiles[tileIndex]);
                        tileIndex++;
                    }
                }
            }
            // Kalan taşlar oynanabilir veya oyun sonrasında kullanılabilir.
        }

        // İlerleyen aşamalarda; oyuncu hamleleri, taş ekleme/çıkarma, set kontrolleri gibi metotlar eklenebilir.
    }
}
