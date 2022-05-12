using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SecurityChannel;

namespace Tests.SecurityChannelTests
{
    public class AesTests
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void EncryptInputDataNullTest()
            => Assert.Catch(() => AesEngine.Encrypt(null, null));
        
        [Test]
        public void EncryptInputDataZeroLengthTest()
            => Assert.Catch(() => AesEngine.Encrypt(Array.Empty<byte>(), Array.Empty<byte>()));

        [Test]
        public void DecryptInputDataNullTest()
            => Assert.Catch(() => AesEngine.Decrypt(null, null));
        
        [Test]
        public void DecryptInputDataZeroLengthTest()
            => Assert.Catch(() => AesEngine.Decrypt(Array.Empty<byte>(), Array.Empty<byte>()));

        [Test]
        public void InputDataTest()
        {
            const string str = "EnCrYpT mE!!1!";
            var data = Encoding.Default.GetBytes(str);
            var key = Enumerable.Repeat((byte)12, 32).ToArray();
                //AesEngine.GetKey();
            var encrypted = AesEngine.Encrypt(data, key);
            
            Assert.AreEqual(str, Encoding.Default.GetString(AesEngine.Decrypt(encrypted, key)));
        }
    }
}