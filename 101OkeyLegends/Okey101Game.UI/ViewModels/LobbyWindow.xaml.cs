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
using _101OkeyLegends.Okey101Game.UI.Views;

namespace _101OkeyLegends.Okey101Game.UI.ViewModels
{
    public partial class LobbyViewModel : ObservableObject
    {
        private HubConnection _connection;

        [ObservableProperty]
        private string playerName = "Oyuncu"; // Varsayılan oyuncu adı

        public ObservableCollection<string> Players { get; set; } = new ObservableCollection<string>();

        public RelayCommand ConnectCommand { get; }
        public RelayCommand StartGameCommand { get; }

        public LobbyViewModel()
        {
            ConnectCommand = new RelayCommand(async () => await ConnectToHub());
            StartGameCommand = new RelayCommand(StartGame, () => Players.Count == 4);
        }

        private async Task ConnectToHub()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/gamehub")
                .WithAutomaticReconnect()
                .Build();

            _connection.On<string[]>("UpdatePlayerList", (playerList) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Players.Clear();
                    foreach (var p in playerList)
                        Players.Add(p);

                    // Oyuncu sayısı değiştiğinde butonu güncelle
                    StartGameCommand.NotifyCanExecuteChanged();
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

        private void StartGame()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Yeni ana pencereyi oluştur ve göster.
                var mainWindow = new MainWindow();
                mainWindow.Show();

                // Açık pencereler arasında Lobby ekranını arayalım.
                var lobbyWindow = Application.Current.Windows
                                     .OfType<Window>()
                                     .FirstOrDefault(w => w.Title.Contains("Lobi"));  // veya sizin lobi pencerenizin belirgin bir özelliğini kullanın

                // Eğer lobby penceresi bulunduysa kapatalım.
                lobbyWindow?.Close();

                // Yeni ana pencereyi Application.Current.MainWindow olarak ayarlayabiliriz.
                Application.Current.MainWindow = mainWindow;
            });
        }
    }
}