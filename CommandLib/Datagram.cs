using System;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SecurityChannel;

namespace CommandLib
{
    [Serializable]
    public class Datagram
    {
        public const int UdpLength = 65507;
        public const int TcpLength = 1500;
        
        private static JsonSerializerOptions _options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        
        [JsonIgnore]
        public Type Type => Type.GetType(TypeName) ?? typeof(object);

        public byte[] Data { get; }
        public byte[] AesKey { get; }
        public string TypeName { get; }

        [JsonConstructor]
        public Datagram(byte[] data, byte[] aesKey, string typeName)
        {
            Data = data;
            AesKey = aesKey;
            TypeName = typeName;
        }

        public Datagram(byte[] data, Type type, RSAParameters? rsaPublicKey = null)
        {
            if (rsaPublicKey.HasValue)
            {
                var aesKey = AesEngine.GetKey();
                Data = AesEngine.Encrypt(data, aesKey);
                AesKey = RsaEngine.Encrypt(aesKey, rsaPublicKey.Value);
            }
            else Data = data;
            
            TypeName = type.FullName;
        }

        public byte[] GetData(RSAParameters? rsaPrivateKey = null)
        {
            if (!rsaPrivateKey.HasValue || AesKey is null)
                return Data;
            
            var aesKey = RsaEngine.Decrypt(AesKey, rsaPrivateKey.Value);
            var data = AesEngine.Decrypt(Data, aesKey);

            return data;
        }

        public byte[] ToBytes() => JsonSerializer.SerializeToUtf8Bytes(this, _options);

        public static Datagram FromBytes(byte[] data)
            => (Datagram)JsonSerializer.Deserialize(data, typeof(Datagram), _options);
        
        public static Datagram FromBytesOrDefault(byte[] data)
        {
            try
            {
                return FromBytes(data);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}