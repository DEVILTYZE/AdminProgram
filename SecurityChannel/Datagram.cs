using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SecurityChannel
{
    [Serializable]
    public class Datagram
    {
        private byte[] _data;
        private byte[] _aesKey;

        public Datagram(byte[] data, byte[] aesKey, RSAParameters rsaPublicKey)
        {
            _data = AesEngine.Encrypt(data, aesKey);
            _aesKey = RsaEngine.Encrypt(aesKey, rsaPublicKey);
        }

        public byte[] GetData(RSAParameters rsaPrivateKey)
        {
            var aesKey = RsaEngine.Decrypt(_aesKey, rsaPrivateKey);
            var data = AesEngine.Decrypt(_data, aesKey);

            return data;
        }

        public byte[] ToBytes()
        {
            var json = JsonSerializer.Serialize(this);

            return Encoding.UTF8.GetBytes(json);
        }

        public static Datagram FromBytes(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);

            return JsonSerializer.Deserialize<Datagram>(json);
        }
    }
}