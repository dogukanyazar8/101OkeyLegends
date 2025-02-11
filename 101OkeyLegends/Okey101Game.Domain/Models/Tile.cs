using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _101OkeyLegends.Okey101Game.Domain.Models
{
    public class Tile
    {
        public TileColor Color { get; }
        public int Number { get; }
        public bool IsJoker { get; set; }

        public Tile(TileColor color, int number)
        {
            if (number < 1 || number > 13)
                throw new ArgumentOutOfRangeException(nameof(number), "Sayı 1 ile 13 arasında olmalıdır.");

            Color = color;
            Number = number;
            IsJoker = false;
        }
        public string DisplayName => $"{Color} {Number}{(IsJoker ? " (Okey)" : string.Empty)}";

        public override string ToString()
        {
            return IsJoker ? $"Okey (as {Color} {Number})" : $"{Color} {Number}";
        }

        // Değer bazlı karşılaştırma için Equals metodunu override edelim.
        public override bool Equals(object obj)
        {
            if (obj is Tile other)
            {
                return this.Color == other.Color &&
                       this.Number == other.Number &&
                       this.IsJoker == other.IsJoker;
            }
            return false;
        }

        // Aynı özelliklere göre hash kodunu hesaplayalım.
        public override int GetHashCode()
        {
            return HashCode.Combine(Color, Number, IsJoker);
        }
    }
}
