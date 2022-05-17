using System.Drawing;
using CommandLib.Commands.RemoteCommandItems;
using NUnit.Framework;

namespace Tests.CommandLibTests
{
    public class ScreenMatrixTests
    {
        private int _height, _width;

        [SetUp]
        public void Setup()
        {
            _height = 1280;
            _width = 720;
        }

        [Test]
        public void ByteArrayCommandResult()
        {
            var screen = new ScreenMatrix(_height, _width);
            var bitmap = new Bitmap(_width, _height);
            screen.UpdateScreen(bitmap);
            var bytes = screen.GetUpdatedPixelsBytes();
            var resultPixels = ScreenMatrix.GetPixelsFromBytesOrDefault(bytes);
            
            Assert.AreEqual(_height * _width, resultPixels.Length);
        }
    }
}