using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SecurityChannel
{
    [Serializable]
    public class Datagram
    {
        [JsonIgnore]
        private Type Type => Type.GetType(TypeName) ?? typeof(object);
        
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
        
        public Datagram(byte[] data, byte[] aesKey, RSAParameters rsaPublicKey, string typeName)
        {
            Data = AesEngine.Encrypt(data, aesKey);
            AesKey = RsaEngine.Encrypt(aesKey, rsaPublicKey);
            TypeName = typeName;
        }
        
        public byte[] GetData(RSAParameters rsaPrivateKey)
        {
            var aesKey = RsaEngine.Decrypt(AesKey, rsaPrivateKey);
            var data = AesEngine.Decrypt(Data, aesKey);

            return data;
        }

        public byte[] ToBytes()
        {
            var json = JsonSerializer.Serialize(this);

            return Encoding.Default.GetBytes(json);
        }

        public static Datagram FromBytes(byte[] data)
        {
            var json = Encoding.Default.GetString(data);

            return JsonSerializer.Deserialize<Datagram>(json);
        }
    }
}