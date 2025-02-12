using Microsoft.AspNetCore.SignalR;
using _101OkeyLegends.Okey101Game.Core.Services;
using _101OkeyLegends.Okey101Game.Domain.Models;
using System.Collections.Concurrent;

namespace _101OkeyLegends.Server.Hubs
{
    public class GameHub : Hub
    {
        // Bağlı oyuncuları (ConnectionId -> OyuncuAdı) tutan koleksiyon
        private static readonly ConcurrentDictionary<string, string> ConnectedPlayers = new();

        // Oyun mantığını yöneten GameManager örneği (varsa)
        private static GameManager _gameManager = new GameManager();
        public async Task JoinGame(string playerName)
        {
            // Bağlantı ID'si ile oyuncu adını eşleştiriyoruz.
            ConnectedPlayers[Context.ConnectionId] = playerName;
            await Clients.All.SendAsync("UpdatePlayerList", ConnectedPlayers.Values);

            // 4 oyuncuya ulaşıldığında, oyun dağıtım işlemleri yapılabilir...
            if (ConnectedPlayers.Count == 4)
            {
                // Oyuncu adlarını diziye aktararak oyunu başlatabilirsiniz...
                var players = ConnectedPlayers.Values.ToArray();
                _gameManager = new GameManager();
                _gameManager.InitializeGame(players);

                // Dağıtım, gösterge, joker belirleme vb. işlemler GameManager içinde yapılır.
                // Artık JokerTile property’si GameManager içinde güncellenmiş olmalıdır.
                string jokerTileInfo = _gameManager.JokerTile != null ? _gameManager.JokerTile.ToString() : "Okey Belirlenmedi";
                // İstemciler, joker taşının bilgisini UpdateJokerTile mesajı ile alacak.
                await Clients.All.SendAsync("UpdateJokerTile", jokerTileInfo);

                // Her oyuncuya kendi destesini gönderelim.
                for (int i = 0; i < _gameManager.Players.Count; i++)
                {
                    var player = _gameManager.Players[i];
                    var connection = ConnectedPlayers.FirstOrDefault(kvp => kvp.Value == player.Name).Key;
                    if (!string.IsNullOrEmpty(connection))
                    {
                        await Clients.Client(connection).SendAsync("ReceiveDeck", player.Hand);
                    }
                }

                // Dağıtılmayan taş listesini tüm oyunculara gönderelim.
                int usedTileCount = _gameManager.Players.Sum(p => p.Hand.Count);
                var remainingTiles = _gameManager.Tiles.Skip(usedTileCount).ToList();
                await Clients.All.SendAsync("ReceiveRemainingTiles", remainingTiles);
            }
        }

        // İstemciden gelen "oyunu başlat" çağrısını alıp tüm oyunculara bildiriyoruz.
        public async Task StartGameFromClient()
        {
            // İsteğe bağlı: 4 oyuncu bağlı mı kontrol edebilirsiniz.
            if (ConnectedPlayers.Count == 4)
            {
                await Clients.All.SendAsync("GameStarted");
            }
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            ConnectedPlayers.TryRemove(Context.ConnectionId, out _);
            await Clients.All.SendAsync("UpdatePlayerList", ConnectedPlayers.Values);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
