using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _101OkeyLegends.Okey101Game.Domain.Models
{
    public class Player
    {
        public string Name { get; }
        public List<Tile> Hand { get; }

        public Player(string name)
        {
            Name = name;
            Hand = new List<Tile>();
        }

        // Oyuncunun elindeki taşları ekle veya düzenleyecek metotlar eklenebilir.
    }
}
