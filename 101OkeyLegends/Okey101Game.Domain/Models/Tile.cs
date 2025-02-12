using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _101OkeyLegends.Okey101Game.Domain.Models
{
    public enum TileColor
    {
        Kırmızı,
        Mavi,
        Sarı,
        Siyah
    }
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

        // Klonlama metodu: Aynı özelliklere sahip yeni bir Tile oluşturur.
        public Tile Clone()
        {
            return new Tile(this.Color, this.Number) { IsJoker = this.IsJoker };
        }

        public override string ToString()
        {
            return IsJoker ? $"Joker (as {Color} {Number})" : $"{Color} {Number}";
        }

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

        public override int GetHashCode()
        {
            return HashCode.Combine(Color, Number, IsJoker);
        }
    }
}
