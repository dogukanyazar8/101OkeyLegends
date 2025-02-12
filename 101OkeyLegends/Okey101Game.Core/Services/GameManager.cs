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

        /// <summary>
        /// Elindeki valid serilerden (Joker'leri de kullanarak) çakışmayan serilerden oluşan,
        /// toplam değeri maksimum olan partition'ı bulur.
        /// PARTİSYON düzeyinde kullanılan Joker sayısını 1 ile sınırlandırır.
        /// (Bu fonksiyon seri perler için; GetMaxPairPartitionWithJoker, çift dizme işlemi için kullanılacaktır.)
        /// Dönüş: Tuple (List<List<Tile>> bestPartition, int totalSum)
        /// </summary>
        public (List<List<Tile>> bestPartition, int totalSum) GetMaxSeriesPartitionWithJoker(List<Tile> hand)
        {
            var validSeries = GetAllValidSeriesWithJoker(hand);
            int n = validSeries.Count;
            int bestSum = 0;
            List<List<Tile>> bestPartition = new List<List<Tile>>();

            // DFS, global joker kullanımını takip eden parametre eklenmiştir.
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
                    if (jokerUsed + seriesJokerCount > 1)
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

        /// <summary>
        /// Elindeki taşlardan, aynı (Color, Number) değerine sahip çiftleri (pair) oluşturur.
        /// Bir çift; aynı numaralı iki taş olarak tanımlanır.
        /// Eğer bir grup için tek taş varsa ve Joker mevcutsa, Joker kullanılarak çift tamamlanır.
        /// PARTİSYON düzeyinde global olarak Joker kullanımı 1 ile sınırlandırılmıştır.
        /// Dönüş: Tuple (List<List<Tile>> bestPairPartition, int totalPairCount)
        /// Burada "totalPairCount" çiftlerin adedini temsil eder.
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
            // Gerçek çiftler (Count >= 2) oluşturuluyor.
            foreach (var group in groups)
            {
                if (group.Count >= 2)
                {
                    partition.Add(new List<Tile> { group.Tile, group.Tile });
                    totalPairs += 1;
                }
            }
            // Eğer Joker kullanılabiliyorsa, Count == 1 olan gruplardan en iyisini seçiyoruz.
            if (jokerCount > 0)
            {
                var candidate = groups
                    .Where(g => g.Count == 1)
                    .OrderByDescending(g => g.Tile.Number)
                    .FirstOrDefault();
                if (candidate != null)
                {
                    partition.Add(new List<Tile> { candidate.Tile, new Tile(candidate.Tile.Color, candidate.Tile.Number) { IsJoker = true } });
                    totalPairs += 1;
                }
            }
            return (partition, totalPairs);
        }

        /// <summary>
        /// El (hand) içindeki tüm geçerli serileri (ardışık run'lar ve grup) Joker kullanımını da göz önünde bulundurarak bulur.
        /// Her seri en az 3 taş içermelidir. Her seri içinde Joker yalnızca bir kere kullanılacaktır.
        /// Dönüş: Tuple (List<Tile> series, int sum) şeklinde her seri ve seri toplamı.
        /// </summary>
        public List<(List<Tile> series, int sum)> GetAllValidSeriesWithJoker(List<Tile> hand)
        {
            var allSeries = new List<(List<Tile>, int)>();

            int jokerCount = hand.Count(t => t.IsJoker);
            var nonJokers = hand.Where(t => !t.IsJoker).ToList();

            // 1. Renk bazlı ardışık seriler (run)
            foreach (TileColor color in Enum.GetValues(typeof(TileColor)))
            {
                var colorTiles = nonJokers
                    .Where(t => t.Color == color)
                    .OrderBy(t => t.Number)
                    .ToList();

                for (int i = 0; i < colorTiles.Count; i++)
                {
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
                            Tile fakeTile = new Tile(color, num) { IsJoker = true };
                            currentSeries.Add(fakeTile);
                            currentSum += num;
                            availableJokers--;
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

            // 2. Aynı sayıdan oluşan gruplar (group) – farklı renklerden
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
                        groupSeries.Add(new Tile(TileColor.Kırmızı, num) { IsJoker = true });
                    }
                    int groupSum = groupSeries.Sum(t => t.Number);
                    allSeries.Add((groupSeries, groupSum));
                }
            }

            return allSeries;
        }
    }

    // İlerleyen aşamalarda; oyuncu hamleleri, taş ekleme/çıkarma, set kontrolleri gibi metotlar eklenebilir.
}