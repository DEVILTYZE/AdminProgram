using System;
using System.IO;
using System.Security.Cryptography;

namespace SecurityChannel
{
    public static class AesEngine
    {
        private static readonly byte[] Iv = { 1, 21, 13, 32, 10, 45, 39, 67, 12, 10, 112, 222, 99, 198, 23, 250 };
        
        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv = null)
        {
            iv ??= Iv;
            
            if (data is null || data.Length == 0)
                throw new Exception("Data exception");

            var aes = GetAes(key, iv);
            
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
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
            
            var decrypted = new byte[data.Length];
            var aes = GetAes(key, iv);
            
            using var ms = new MemoryStream(data);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(key, iv), CryptoStreamMode.Read);
            var length = cs.Read(decrypted, 0, decrypted.Length);

            var returnArray = new byte[length];
            Array.Copy(decrypted, returnArray, length);

            return returnArray;
        }

        public static byte[] GetKey(int length = 32)
        {
            var key = new byte[length];
            var random = new Random();

            for (var i = 0; i < key.Length; ++i)
                key[i] = (byte)random.Next(0, 256);

            return key;
        }

        private static AesCryptoServiceProvider GetAes(byte[] key, byte[] iv)
        {
            iv ??= Iv;
            
            if (key is null || key.Length == 0)
                throw new Exception("Key exception");
            
            if (iv is null || iv.Length == 0)
                throw new Exception("IV exception");

            using var aes = new AesCryptoServiceProvider();
            aes.Mode = CipherMode.CBC;
            aes.KeySize = key.Length * 8;
            aes.BlockSize = iv.Length * 8;
            aes.Padding = PaddingMode.ISO10126;

            return aes;
        }
    }
}