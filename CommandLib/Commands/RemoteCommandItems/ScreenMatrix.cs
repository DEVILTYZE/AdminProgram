using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CommandLib.Commands.RemoteCommandItems
{
    /// <summary>
    /// Вспомогательный класс для работы изображением экрана.
    /// </summary>
    public class ScreenMatrix
    {
        private ScreenPixel[,] _pixels;
        private int Height => _pixels.GetLength(0);
        private int Width => _pixels.GetLength(1);

        public ScreenMatrix() { }

        public ScreenMatrix(int height, int width) => _pixels = new ScreenPixel[height, width];
        
        /// <summary>
        /// Обновление пикселей в ScreenMatrix и на форме.
        /// </summary>
        /// <param name="pixels">Массив обновленных пикселей.</param>
        /// <param name="screen">Текущее изображение экрана.</param>
        public void UpdateScreen(IEnumerable<ScreenPixel> pixels, Bitmap screen)
        {
            var screenPixels = pixels as ScreenPixel[] ?? pixels.ToArray();
            UpdateScreen(screenPixels);

            foreach (var pixel in screenPixels)
                screen.SetPixel(pixel.X, pixel.Y, Color.FromArgb(pixel.Rgba[3], pixel.Rgba[0], 
                    pixel.Rgba[1], pixel.Rgba[2]));
        }
        
        /// <summary>
        /// Обновление статуса пикселей в ScreenMatrix.
        /// </summary>
        /// <param name="screen"></param>
        public void UpdateScreen(Bitmap screen)
        {
            _pixels ??= new ScreenPixel[screen.Height, screen.Width];
            
            if (_pixels[1, 0].Y == 0)
            {
                SetupAllPixels(screen);
                return;
            }
            
            for (var i = 0; i < Height; ++i)
            for (var j = 0; j < Width; ++j)
            {
                var pixel = screen.GetPixel(j, i);

                if (_pixels[i, j].Equals(pixel)) 
                    continue;
                
                _pixels[i, j].SetPixel(pixel);
                _pixels[i, j].IsUpdated = true;
            }
        }

        /// <summary>
        /// Получение массива байт, представляющего собой массив из обновленных пикселей.
        /// </summary>
        /// <returns>Массив байт.</returns>
        public byte[] GetUpdatedPixelsBytes()
        {
            var array = _pixels.Cast<ScreenPixel>().Where(pixel => pixel.IsUpdated).ToArray();
            UpdateToFalse(array);
            var resultArray = BitConverter.GetBytes(array.Length);
            var result = resultArray.AsEnumerable();
            result = array.Aggregate(result, (current, pixel) => current.Concat(pixel.ToBytes()));

            return result.ToArray();
        }

        public static ScreenPixel[] GetPixelsFromBytesOrDefault(byte[] data)
        {
            var length = BitConverter.ToInt32(data.AsSpan()[0..4]);
            data = data.Skip(4).ToArray();
            
            if (length < 0)
                return null;
            
            var pixels = new ScreenPixel[length];
            
            for (var i = 0; i < length - 1; ++i)
                pixels[i] = ScreenPixel.FromBytes(
                    data.AsSpan()[(ScreenPixel.OnePixelLength * i)..(ScreenPixel.OnePixelLength * (i + 1))].ToArray());

            return pixels;
        }
        
        /// <summary>
        /// Обновление пикселей в ScreenMatrix.
        /// </summary>
        /// <param name="pixels">Массив обновленных пикселей.</param>
        private void UpdateScreen(IEnumerable<ScreenPixel> pixels)
        {
            foreach (var pixel in pixels)
                _pixels[pixel.Y, pixel.X].SetPixel(pixel);
        }

        private void SetupAllPixels(Bitmap screen)
        {
            for (var i = 0; i < Height; ++i)
            for (var j = 0; j < Width; ++j)
            {
                _pixels[i, j] = new ScreenPixel(j, i) { IsUpdated = true };
                _pixels[i, j].SetPixel(screen.GetPixel(j, i));
            }
        }

        private void UpdateToFalse(IEnumerable<ScreenPixel> array)
        {
            foreach (var pixel in array)
                _pixels[pixel.Y, pixel.X].IsUpdated = false;
        }
    }
}