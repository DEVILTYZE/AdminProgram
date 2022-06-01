using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;

namespace CommandLib.Commands.Helpers
{
    public static class ByteHelper
    {
        public static byte[] ImagesXOrDecompress(byte[] byteArrayImage, byte[] byteArrayImage2)
        {
            byteArrayImage = DecompressArray(byteArrayImage);

            if (byteArrayImage2 is null)
                return byteArrayImage;
            
            var firstBigger = byteArrayImage.Length > byteArrayImage2.Length;
            var bigArray = firstBigger ? byteArrayImage : byteArrayImage2;
            var smallArray = firstBigger ? byteArrayImage2 : byteArrayImage;
            
            for (var i = 0; i < smallArray.Length; ++i)
                bigArray[i] ^= smallArray[i];

            return bigArray;
        }
        
        public static byte[] ImagesXOrCompress(byte[] byteArrayImage, byte[] byteArrayImage2)
        {
            if (byteArrayImage2 is null)
                return CompressArray(byteArrayImage);

            var firstBigger = byteArrayImage.Length > byteArrayImage2.Length;
            var bigArray = firstBigger ? byteArrayImage : byteArrayImage2;
            var smallArray = firstBigger ? byteArrayImage2 : byteArrayImage;
            
            for (var i = 0; i < smallArray.Length; ++i)
                bigArray[i] ^= smallArray[i];

            return CompressArray(bigArray);
        }

        public static byte[] ImageToBytes(Image image)
        {
            using var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Jpeg);

            return ms.ToArray();
        }

        public static Bitmap BytesToImage(byte[] array)
        {
            using var ms = new MemoryStream(array);

            return new Bitmap(ms);
        }

        private static byte[] CompressArray(byte[] array)
        {
            using var ms = new MemoryStream();
            using var stream = new DeflateStream(ms, CompressionLevel.Optimal);
            stream.Write(array, 0, array.Length);

            return ms.ToArray();
        }

        private static byte[] DecompressArray(byte[] array)
        {
            using var msInput = new MemoryStream(array);
            using var msOutput = new MemoryStream();
            using var stream = new DeflateStream(msInput, CompressionMode.Decompress);
            stream.CopyTo(msOutput);

            return msOutput.ToArray();
        }
    }
}