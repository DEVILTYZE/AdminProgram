using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Compression;
using CommandLib;
using CommandLib.Commands.Helpers;
using NUnit.Framework;
using SecurityChannel;

namespace Tests.CommandLibTests
{
    public class ImageTests
    {
        private Dictionary<byte, Color> _colors = new()
        {
            {0, Color.Aqua},
            {1, Color.Black},
            {2, Color.Blue},
            {3, Color.Chocolate},
            {4, Color.MediumSeaGreen},
            {5, Color.Firebrick},
            {6, Color.MediumSlateBlue},
            {7, Color.SeaGreen},
            {8, Color.SandyBrown},
            {9, Color.Salmon},
            {10, Color.Tan}
        };
        
        private static readonly Size LowQualitySize = new(1400, 950);

        [SetUp]
        public void Setup() { }

        [Test]
        public void CompressDecompress()
        {
            const int length = 120000;
            var random = new Random();
            var bytes = new byte[length];

            for (var i = 0; i < bytes.Length; ++i)
                bytes[i] = (byte)random.Next(byte.MinValue, byte.MaxValue + 1);
            
            var compressedBytes = ByteHelper.ImagesXOrCompress(bytes, null);
            var decompressedBytes = ByteHelper.ImagesXOrDecompress(compressedBytes, null);
            
            Assert.AreEqual(bytes, decompressedBytes);
        }

        [Test]
        public void ImageToByteAndToImage()
        {
            const int length = 11;
            const int width = 1920;
            const int height = 1080;
            var keys = RsaEngine.GetKeys();
            var r = new Random();
            var image = new Bitmap(width, height);
            
            for (var i = 0; i < height; ++i)
                for (var j = 0; j < width; ++j)
                    image.SetPixel(j, i, _colors[(byte)r.Next(0, length)]);

            var lowQualityImage = ReduceQuality(image);
            SavePic(lowQualityImage, 1);
            var bytesImage = ByteHelper.ImageToBytes(lowQualityImage);
            bytesImage = ByteHelper.ImagesXOrCompress((byte[])bytesImage.Clone(), null);
            
            var datagram = new Datagram(bytesImage, typeof(byte[]), keys[1]);
            var bytes = datagram.ToBytes();
            datagram = Datagram.FromBytes(bytes);
            bytesImage = datagram.GetData(keys[0]);

            bytesImage = ByteHelper.ImagesXOrDecompress(bytesImage, null);
            image = ByteHelper.BytesToImage(bytesImage);
            SavePic(image, 2);
            
            Assert.AreEqual(lowQualityImage.Size, image.Size);
        }

        private void SavePic(Image image, int number)
            => image.Save(Environment.CurrentDirectory + $"\\test_images\\img{number}.jpg", ImageFormat.Jpeg);
        
        
        private static Bitmap ReduceQuality(Image image)
        {
            var widthCoef = (float)image.Width / LowQualitySize.Width;
            var heightCoef = (float)image.Height / LowQualitySize.Height;
            var ratio = widthCoef > heightCoef ? widthCoef : heightCoef;
            int width = (int)(image.Width / ratio), height = (int)(image.Height / ratio);
            var rectangle = new Rectangle(0, 0, width, height);
            var newImage = new Bitmap(width, height);
            var g = Graphics.FromImage(newImage);
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.DrawImage(image, rectangle, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);

            return newImage;
        }
    }
}