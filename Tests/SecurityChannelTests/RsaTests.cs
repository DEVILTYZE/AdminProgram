using System;
using System.Security.Cryptography;
using NUnit.Framework;
using SecurityChannel;

namespace Tests.SecurityChannelTests
{
    public class RsaTests
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void EncryptInputDataNullTest()
            => Assert.Catch(() => RsaEngine.Encrypt(null, new RSAParameters()));
        
        [Test]
        public void EncryptInputDataZeroLengthTest()
            => Assert.Catch(() => RsaEngine.Encrypt(Array.Empty<byte>(), new RSAParameters()));
        
        [Test]
        public void DecryptInputDataNullTest()
            => Assert.Catch(() => RsaEngine.Decrypt(null, new RSAParameters()));
        
        [Test]
        public void DecryptInputDataZeroLengthTest()
            => Assert.Catch(() => RsaEngine.Decrypt(Array.Empty<byte>(), new RSAParameters()));

        [Test]
        public void InputDataTest()
        {
            var aesKey = AesEngine.GetKey();
            var keys = RsaEngine.GetKeys();
            var encrypted = RsaEngine.Encrypt(aesKey, keys[1]);
            
            Assert.AreEqual(aesKey, RsaEngine.Decrypt(encrypted, keys[0]));
        }
    }
}