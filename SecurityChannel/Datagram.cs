using System;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SecurityChannel
{
    [Serializable]
    public class Datagram
    {
        private static JsonSerializerOptions _options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        
        [JsonIgnore]
        public Type Type => Type.GetType(TypeName) ?? typeof(object);

        public byte[] Data { get; }
        public byte[] AesKey { get; }
        public string TypeName { get; }
        public bool IsEncrypted { get; }

        [JsonConstructor]
        public Datagram(byte[] data, byte[] aesKey, string typeName, bool isEncrypted)
        {
            Data = data;
            AesKey = aesKey;
            TypeName = typeName;
            IsEncrypted = isEncrypted;
        }

        public Datagram(byte[] data, byte[] aesKey, RSAParameters rsaPublicKey, string typeName, bool isEncrypted)
        {
            if (isEncrypted)
            {
                Data = AesEngine.Encrypt(data, aesKey);
                AesKey = RsaEngine.Encrypt(aesKey, rsaPublicKey);
            }
            else Data = data;
            
            TypeName = typeName;
            IsEncrypted = isEncrypted;
        }

        public byte[] GetData(RSAParameters rsaPrivateKey)
        {
            if (!IsEncrypted)
                return Data;
            
            var aesKey = RsaEngine.Decrypt(AesKey, rsaPrivateKey);
            var data = AesEngine.Decrypt(Data, aesKey);

            return data;
        }

        public byte[] ToBytes() => JsonSerializer.SerializeToUtf8Bytes(this, _options);

        public static Datagram FromBytes(byte[] data) => JsonSerializer.Deserialize<Datagram>(data, _options);
    }
}