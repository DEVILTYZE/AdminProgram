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
        private const int IntLength = 4;
        public const int OnePixelLength = 12;

        public int X { get; }
        public int Y { get; }
        public byte[] Rgba { get; }
        public bool IsUpdated { get; set; }

        public ScreenPixel(int x, int y)
        {
            X = x;
            Y = y;
            Rgba = new[] { byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue };
            IsUpdated = false;
        }

        public ScreenPixel(int x, int y, IReadOnlyList<byte> rgba)
        {
            X = x;
            Y = y;
            Rgba = new[] { rgba[0], rgba[1], rgba[2], rgba[3] };
            IsUpdated = false;
        }

        /// <summary>
        /// Сравнивает эквивалентность пикселей по цвету.
        /// </summary>
        /// <param name="pixel"></param>
        /// <returns></returns>
        public bool Equals(Color pixel) 
            => pixel.R != Rgba[0] || pixel.G != Rgba[1] || pixel.B != Rgba[2] || pixel.A != Rgba[3];

        /// <summary>
        /// Устанавливает специальный цвет на пиксель.
        /// </summary>
        public void SetPixel(Color pixel)
        {
            Rgba[0] = pixel.R;
            Rgba[1] = pixel.G;
            Rgba[2] = pixel.B;
            Rgba[3] = pixel.A;
        }
        
        /// <summary>
        /// Устанавливает специальный цвет на пиксель.
        /// </summary>
        public void SetPixel(ScreenPixel pixel)
        {
            for (var i = 0; i < Rgba.Length; ++i)
                Rgba[i] = pixel.Rgba[i];
        }

        public byte[] ToBytes()
        {
            var x = BitConverter.GetBytes(X);
            var y = BitConverter.GetBytes(Y);

            return x.Concat(y).Concat(Rgba).ToArray();
        }

        public static ScreenPixel FromBytes(byte[] data)
        {
            var x = BitConverter.ToInt32(data.Take(IntLength).ToArray(), 0);
            var y = BitConverter.ToInt32(data.Skip(IntLength).Take(IntLength).ToArray(), 0);

            return new ScreenPixel(x, y, data.Skip(IntLength * 2).ToArray());
        }
    }
}