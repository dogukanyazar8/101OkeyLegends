using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace _101OkeyLegends.Okey101Game.Domain.Models
{
    public class LobbyPlayer : ObservableObject
    {
        private bool _isReady;
        public string Name { get; set; }
        public bool IsReady
        {
            get => _isReady;
            set => SetProperty(ref _isReady, value);
        }
    }
}
