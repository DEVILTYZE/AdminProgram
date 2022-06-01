using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CommandLib.Commands.RemoteCommandItems
{
    /// <summary>
    /// Вспомогательная структура для работы с пикселями.
    /// </summary>
    public struct ScreenPixel
    {
        private const int ShortLength = 2;
        public const int OnePixelLength = 7;

        public short X { get; }
        public short Y { get; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }


        public ScreenPixel(short x, short y, byte r, byte g, byte b)
        {
            X = x;
            Y = y;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Сравнивает эквивалентность пикселей по цвету.
        /// </summary>
        /// <param name="pixel"></param>
        /// <returns></returns>
        public bool Equals(Color pixel) => pixel.R != R || pixel.G != G || pixel.B != B;
        
        public bool Equals(byte r, byte g, byte b) => r != R || g != G || b != B;

        /// <summary>
        /// Устанавливает специальный цвет на пиксель.
        /// </summary>
        public void SetPixel(Color pixel)
        {
            R = pixel.R;
            G = pixel.G;
            B = pixel.B;
        }

        public void SetPixel(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public IEnumerable<byte> ToBytes()
        {
            var x = BitConverter.GetBytes(X);
            var y = BitConverter.GetBytes(Y);

            return x.Concat(y).Concat(new[] { R, G, B }).ToArray();
        }

        public static ScreenPixel FromBytes(byte[] data)
        {
            var x = BitConverter.ToInt16(data.Take(ShortLength).ToArray(), 0);
            var y = BitConverter.ToInt16(data.Skip(ShortLength).Take(ShortLength).ToArray(), 0);
            var rgb = data.Skip(ShortLength * 2).ToArray();
            
            return new ScreenPixel(x, y, rgb[0], rgb[1], rgb[2]);
        }
    }
}