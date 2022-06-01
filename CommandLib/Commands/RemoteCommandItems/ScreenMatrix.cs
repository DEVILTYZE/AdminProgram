using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace CommandLib.Commands.RemoteCommandItems
{
    /// <summary>
    /// Вспомогательный класс для работы изображением экрана.
    /// </summary>
    public class ScreenMatrix
    {
        private HashSet<ScreenPixel> _buffer;
        private ScreenPixel[,] _pixels;
        
        private int Height => _pixels.GetLength(0);
        private int Width => _pixels.GetLength(1);

        public ScreenMatrix() => _buffer = new HashSet<ScreenPixel>();

        public byte[] ToBytes(Bitmap nextState)
        {
            BitmapData bitmapData;
            
            if (_pixels is null)
            {
                _pixels = new ScreenPixel[nextState.Height, nextState.Width];
                bitmapData = nextState.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);
                
                try
                {
                    unsafe
                    {
                        for (short i = 0; i < Height; ++i)
                        {
                            var cursor = (byte*)bitmapData.Scan0 + i * bitmapData.Stride;

                            for (short j = 0; j < Width; ++j)
                            {
                                if (i == Height - 1 && j == Width - 1)
                                    break;
                                
                                _pixels[i, j] = new ScreenPixel(j, i, *++cursor, *++cursor, *++cursor);
                            }
                        }
                    }
                }
                finally
                {
                    nextState.UnlockBits(bitmapData);
                }


                return BitmapToBytes(nextState);
            }

            if (_buffer.Count > Datagram.Length - 2000)
            {
                _buffer = new HashSet<ScreenPixel>(Height * Width);
                
                return BitmapToBytes(nextState);
            }

            bitmapData = nextState.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                unsafe
                {
                    for (var i = 0; i < Height; ++i)
                    {
                        var cursor = (byte*)bitmapData.Scan0 + i * bitmapData.Stride;
                        
                        for (var j = 0; j < Width; ++j)
                        {
                            if (i == Height - 1 && j == Width - 1)
                                break;

                            var r = *++cursor;
                            var g = *++cursor;
                            var b = *++cursor;
                            
                            if (_pixels[i, j].Equals(r, g, b))
                                continue;

                            _buffer.Add(_pixels[i, j]);
                            _pixels[i, j].SetPixel(r, g, b);
                        }
                    }
                }
            }
            finally
            {
                nextState.UnlockBits(bitmapData);
            }

            return GetPixelsBytes(_buffer.ToArray());
        }

        public static void FromBytes(byte[] data, ref Bitmap image)
        {
            var length = BitConverter.ToInt32(data.AsSpan()[..4]);

            if (length < 0)
            {
                using var ms = new MemoryStream();
                ms.Write(data, 0, data.Length);
                image = new Bitmap(ms);
                
                return;
            }
            
            var pixels = GetPixelsFromBytesOrDefault(data, length);
            
            foreach (var pixel in pixels)
                image.SetPixel(pixel.X, pixel.Y, Color.FromArgb(pixel.R, pixel.G, pixel.B));
        }

        private static IEnumerable<ScreenPixel> GetPixelsFromBytesOrDefault(byte[] data, int length)
        {
            data = data.Skip(4).ToArray();
            
            if (length < 0)
                return null;
            
            var pixels = new ScreenPixel[length];
            
            for (var i = 0; i < length - 1; ++i)
                pixels[i] = ScreenPixel.FromBytes(
                    data.AsSpan()[(ScreenPixel.OnePixelLength * i)..(ScreenPixel.OnePixelLength * (i + 1))].ToArray());

            return pixels;
        }

        private static byte[] GetPixelsBytes(IReadOnlyCollection<ScreenPixel> array)
        {
            var resultArray = BitConverter.GetBytes(array.Count);
            var result = resultArray.AsEnumerable();
            result = array.Aggregate(result, (current, pixel) => current.Concat(pixel.ToBytes()));

            return result.ToArray();
        }

        private static byte[] BitmapToBytes(Image image)
        {
            using var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Jpeg);

            return ms.ToArray();
        }
    }
}