using System;
using System.Collections.Generic;
using System.Linq;
using _101OkeyLegends.Okey101Game.Domain.Models;

namespace _101OkeyLegends.Okey101Game.Core.Services
{
    public class GameManager
    {
        public bool TestMode { get; set; } = false;
        #region Constants

        // 4 oyuncu varsayarak dağıtım sabitleri
        private const int FirstPlayerTileCount = 22;
        private const int OtherPlayerTileCount = 21;

        #endregion

        #region Properties

        public List<Tile> Tiles { get; private set; }
        public List<Player> Players { get; private set; }
        public Tile JokerTile { get; private set; }
        public Tile IndicatorTile { get; private set; }
        private readonly Random _random = new Random();

        #endregion

        #region Constructor

        public GameManager()
        {
            Tiles = new List<Tile>();
            Players = new List<Player>();
        }

        #endregion

        #region Game Initialization

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
            // Her renkten 1-13 arası taşlardan 2 adet ekleniyor.
            foreach (TileColor color in Enum.GetValues(typeof(TileColor)))
            {
                for (int i = 1; i <= 13; i++)
                {
                    Tiles.Add(new Tile(color, i));
                    Tiles.Add(new Tile(color, i));
                }
            }
        }

        private void ShuffleTiles()
        {
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
            // Gösterge, destedeki orta konumdaki taş olarak seçilir.
            int indicatorIndex = Tiles.Count / 2;
            IndicatorTile = Tiles[indicatorIndex];

            // Joker: Gösterge taşının sonraki değeri (13 ise 1)
            int jokerNumber = IndicatorTile.Number == 13 ? 1 : IndicatorTile.Number + 1;

            // İlgili renk ve numaraya sahip tüm taşları bul.
            var jokerTiles = Tiles.Where(t => t.Color == IndicatorTile.Color && t.Number == jokerNumber).ToList();

            if (jokerTiles.Any())
            {
                foreach (var tile in jokerTiles)
                {
                    tile.IsJoker = true;
                }
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
            int tileIndex = 0;
            for (int i = 0; i < Players.Count; i++)
            {
                int tilesToDeal = i == 0 ? FirstPlayerTileCount : OtherPlayerTileCount;
                for (int j = 0; j < tilesToDeal; j++)
                {
                    if (tileIndex < Tiles.Count)
                    {
                        Players[i].Hand.Add(Tiles[tileIndex]);
                        tileIndex++;
                    }
                }
            }

            if (TestMode && Players.Count > 0 && JokerTile != null)
            {
                Players[0].Hand.Add(JokerTile.Clone());
            }
        }

        public void InjectIndicatorJokerToFirstPlayer()
        {
            if (Players != null && Players.Count > 0 && JokerTile != null)
            {
                // JokerTile'ı klonlayıp ilk oyuncunun eline ekleyelim.
                Tile clonedJoker = JokerTile.Clone();
                Players[0].Hand.Add(clonedJoker);
            }
        }

        #endregion

        #region Series (Per) Partition Hesaplamaları

        /// <summary>
        /// El (hand) içindeki tüm geçerli serileri (ardışık run'lar ve grup) Joker kullanımını göz önünde bulundurarak bulur.
        /// Her seri en az 3 taş içermelidir. Her seri içinde Joker yalnızca 1 kez kullanılacaktır.
        /// Dönüş: Tuple (List<Tile> series, int sum) şeklinde her seri ve seri toplamı.
        /// </summary>
        public List<(List<Tile> series, int sum)> GetAllValidSeriesWithJoker(List<Tile> hand)
        {
            var allSeries = new List<(List<Tile>, int)>();

            // Joker sayısını tespit et.
            int jokerCount = hand.Count(t => t.IsJoker);
            var nonJokers = hand.Where(t => !t.IsJoker).ToList();

            // Renk bazlı ardışık seriler (run)
            foreach (TileColor color in Enum.GetValues(typeof(TileColor)))
            {
                var colorTiles = nonJokers
                    .Where(t => t.Color == color)
                    .OrderBy(t => t.Number)
                    .ToList();

                for (int i = 0; i < colorTiles.Count; i++)
                {
                    // Her seri için, o seride en fazla 1 Joker kullanılacak.
                    int availableJokers = Math.Min(jokerCount, 1);
                    var currentSeries = new List<Tile>();
                    int currentSum = 0;
                    int expected = colorTiles[i].Number;

                    for (int num = expected; num <= 13; num++)
                    {
                        var tile = colorTiles.FirstOrDefault(t => t.Number == num && !currentSeries.Contains(t));
                        if (tile != null)
                        {
                            currentSeries.Add(tile);
                            currentSum += tile.Number;
                        }
                        else if (availableJokers > 0)
                        {
                            // Joker'i eksik numarayı doldurmak için kullan.
                            Tile fakeTile = new Tile(color, num) { IsJoker = true };
                            currentSeries.Add(fakeTile);
                            currentSum += num;
                            availableJokers--; // Bu seri için Joker kullanıldı.
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (currentSeries.Count >= 3)
                        allSeries.Add((new List<Tile>(currentSeries), currentSum));
                }
            }

            // Aynı sayıdan oluşan gruplar (group) – farklı renklerden
            for (int num = 1; num <= 13; num++)
            {
                var groupTiles = nonJokers
                    .Where(t => t.Number == num)
                    .GroupBy(t => t.Color)
                    .Select(g => g.First())
                    .ToList();

                int possibleGroupSize = Math.Min(4, groupTiles.Count + (jokerCount > 0 ? 1 : 0));
                if (possibleGroupSize >= 3)
                {
                    var groupSeries = new List<Tile>(groupTiles);
                    if (groupSeries.Count < possibleGroupSize && jokerCount > 0)
                    {
                        // Sadece 1 Joker ekle.
                        groupSeries.Add(new Tile(TileColor.Kırmızı, num) { IsJoker = true });
                    }
                    int groupSum = groupSeries.Sum(t => t.Number);
                    allSeries.Add((groupSeries, groupSum));
                }
            }

            return allSeries;
        }

        /// <summary>
        /// Elindeki valid serilerden (Joker'leri de kullanarak) çakışmayan serilerden oluşan,
        /// toplam değeri maksimum olan partition'ı bulur.
        /// **Global düzeyde**, kullanılan Joker sayısı artık elinizdeki okey sayısına eşit olacak şekilde sınırlandırılır.
        /// Dönüş: Tuple (List<List<Tile>> bestPartition, int totalSum)
        /// </summary>
        public (List<List<Tile>> bestPartition, int totalSum) GetMaxSeriesPartitionWithJoker(List<Tile> hand)
        {
            var validSeries = GetAllValidSeriesWithJoker(hand);
            int n = validSeries.Count;
            int bestSum = 0;
            List<List<Tile>> bestPartition = new List<List<Tile>>();

            // Elinizdeki toplam okey sayısı:
            int totalJokers = hand.Count(t => t.IsJoker);

            void DFS(int index, HashSet<Tile> used, List<List<Tile>> currentPartition, int currentSum, int jokerUsed)
            {
                if (currentSum > bestSum)
                {
                    bestSum = currentSum;
                    bestPartition = currentPartition.Select(series => new List<Tile>(series)).ToList();
                }
                for (int i = index; i < n; i++)
                {
                    var series = validSeries[i].series;
                    int seriesJokerCount = series.Count(t => t.IsJoker);
                    // Global olarak, partition içinde kullanılabilecek maksimum okey sayısı,
                    // elinizdeki okey sayısına eşit olsun.
                    if (jokerUsed + seriesJokerCount > totalJokers)
                        continue;
                    if (series.Any(tile => used.Contains(tile)))
                        continue;
                    foreach (var tile in series)
                        used.Add(tile);
                    currentPartition.Add(series);
                    DFS(i + 1, used, currentPartition, currentSum + validSeries[i].sum, jokerUsed + seriesJokerCount);
                    currentPartition.RemoveAt(currentPartition.Count - 1);
                    foreach (var tile in series)
                        used.Remove(tile);
                }
            }

            DFS(0, new HashSet<Tile>(), new List<List<Tile>>(), 0, 0);
            return (bestPartition, bestSum);
        }

        #endregion

        #region Pair (Çift) Partition Hesaplamaları

        /// <summary>
        /// Elindeki taşlardan, aynı (Color, Number) değerine sahip çiftleri (pair) oluşturur.
        /// Bir çift; aynı numaralı iki taş olarak tanımlanır.
        /// Eğer bir grup için tek taş varsa ve Joker mevcutsa, Joker kullanılarak çift tamamlanır.
        /// **Global düzeyinde**, kullanılabilecek Joker sayısı elinizdeki okey sayısına göre ayarlanır.
        /// Dönüş: Tuple (List<List<Tile>> bestPairPartition, int totalPairCount)
        /// Burada "totalPairCount", oluşturulan çiftlerin adedini temsil eder.
        /// </summary>
        public (List<List<Tile>> bestPairPartition, int totalPairCount) GetMaxPairPartitionWithJoker(List<Tile> hand)
        {
            int jokerCount = hand.Count(t => t.IsJoker);
            var nonJokers = hand.Where(t => !t.IsJoker).ToList();

            // Aynı (Color, Number) bazında gruplandırma
            var groups = nonJokers.GroupBy(t => new { t.Color, t.Number })
                .Select(g => new { Tile = g.First(), Count = g.Count() })
                .ToList();

            List<List<Tile>> partition = new List<List<Tile>>();
            int totalPairs = 0;
            // Gerçek çiftler (Count >= 2) ekleniyor.
            foreach (var group in groups)
            {
                if (group.Count >= 2)
                {
                    partition.Add(new List<Tile> { group.Tile, group.Tile });
                    totalPairs += 1;
                }
            }
            // Eğer okey (joker) mevcutsa, Count == 1 olan gruplardan en iyilerini kullanın.
            if (jokerCount > 0)
            {
                var candidates = groups
                    .Where(g => g.Count == 1)
                    .OrderByDescending(g => g.Tile.Number)
                    .Take(jokerCount); // Elinizdeki okey sayısı kadar çift oluşturun.
                foreach (var candidate in candidates)
                {
                    partition.Add(new List<Tile> { candidate.Tile, new Tile(candidate.Tile.Color, candidate.Tile.Number) { IsJoker = true } });
                    totalPairs += 1;
                }
            }
            return (partition, totalPairs);
        }

        #endregion
    }
}
