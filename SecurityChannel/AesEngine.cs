using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SecurityChannel
{
    public static class AesEngine
    {
        private static readonly byte[] Iv =
        {
            byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
            byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
            byte.MaxValue, byte.MaxValue
        };
        
        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv = null)
        {
            iv ??= Iv;
            
            if (data is null || data.Length == 0)
                throw new Exception("Data exception");
            
            var aes = GetAes(key, iv);
            var encryptor = aes.CreateEncryptor();

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
            }
            
            return ms.ToArray();
        }

        public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv = null)
        {
            iv ??= Iv;
            
            if (data is null || data.Length == 0)
                throw new Exception("Data exception");
            
            var aes = GetAes(key, iv);
            var decryptor = aes.CreateDecryptor();

            using var ms = new MemoryStream(data);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            var decrypted = new byte[data.Length];
            var bytesRead = cs.Read(decrypted, 0, data.Length);
            decrypted = decrypted.Take(bytesRead).ToArray();
            //cs.FlushFinalBlock();

            return decrypted;
        }

        public static byte[] GetKey(int length = 32)
        {
            var key = new byte[length];
            var random = new Random();

            for (var i = 0; i < key.Length; ++i)
                key[i] = (byte)random.Next(0, 256);

            return key;
        }

        private static Aes GetAes(byte[] key, byte[] iv)
        {
            if (key is null || key.Length == 0)
                throw new Exception("Key exception");
            
            if (iv is null || iv.Length == 0)
                throw new Exception("IV exception");

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.Zeros;
            aes.Key = key;
            aes.IV = iv;

            return aes;
        }
    }
}