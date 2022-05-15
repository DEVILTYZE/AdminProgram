using System;
using System.Security.Cryptography;

namespace SecurityChannel
{
    public static class RsaEngine
    {
        public static RSAParameters[] GetKeys(int length = 2048)
        {
            var csp = new RSACryptoServiceProvider(length);
            var privateKey = csp.ExportParameters(true);
            var publicKey = csp.ExportParameters(false);

            return new[] { privateKey, publicKey };
        }

        public static byte[] Encrypt(byte[] data, RSAParameters publicKey)
        {
            if (data is null || data.Length == 0)
                throw new Exception("Data exception");
            
            var csp = new RSACryptoServiceProvider();
            csp.ImportParameters(publicKey);

            return csp.Encrypt(data, false);
        }

        public static byte[] Decrypt(byte[] data, RSAParameters privateKey)
        {
            if (data is null || data.Length == 0)
                throw new Exception("Data exception");

            var csp = new RSACryptoServiceProvider();
            csp.ImportParameters(privateKey);

            return csp.Decrypt(data, false);
        }
    }
}