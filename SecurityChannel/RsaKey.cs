using System;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace SecurityChannel
{
    [Serializable]
    public class RsaKey
    {
        public byte[] D { get; set; }
        public byte[] Exponent { get; set; }
        public byte[] Modulus { get; set; }
        public byte[] P { get; set; }
        public byte[] Q { get; set; }
        public byte[] Dp { get; set; }
        public byte[] Dq { get; set; }
        public byte[] InverseQ { get; set; }

        [JsonConstructor]
        public RsaKey() { }

        public RsaKey(RSAParameters key)
        {
            D = key.D;
            Exponent = key.Exponent;
            Modulus = key.Modulus;
            P = key.P;
            Q = key.Q;
            Dp = key.DP;
            Dq = key.DQ;
            InverseQ = key.InverseQ;
        }

        public RSAParameters GetKey()
        {
            var returnKey = new RSAParameters
            {
                D = D,
                Exponent = Exponent,
                Modulus = Modulus,
                P = P,
                Q = Q,
                DP = Dp,
                DQ = Dq,
                InverseQ = InverseQ
            };

            return returnKey;
        }
    }
}