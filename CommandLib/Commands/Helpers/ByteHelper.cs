using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;
using SharpCompress.IO;

namespace CommandLib.Commands.Helpers
{
    public static class ByteHelper
    {
        public static byte[] ImageToBytes(Image image)
        {
            using var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Jpeg);

            return ms.ToArray();
        }

        public static Bitmap BytesToImage(byte[] array)
        {
            using var ms = new NonDisposingStream(new MemoryStream(array));
            
            return Image.FromStream(ms) as Bitmap;
        }

        public static byte[] CompressArray(byte[] array)
        {
            using var msInput = new MemoryStream(array);
            using var msOutput = new MemoryStream();
            using var stream = new ZlibStream(msOutput, CompressionMode.Compress, CompressionLevel.BestCompression) 
                { FlushMode = FlushType.Sync };
            msInput.CopyTo(stream);
            msOutput.Position = 0;
            
            return msOutput.ToArray();
        }

        public static byte[] DecompressArray(byte[] array)
        {
            using var msInput = new MemoryStream(array);
            using var msOutput = new MemoryStream();
            using var stream = new ZlibStream(msInput, CompressionMode.Decompress);
            stream.CopyTo(msOutput);
            msOutput.Position = 0;

            return msOutput.ToArray();
        }
    }
}