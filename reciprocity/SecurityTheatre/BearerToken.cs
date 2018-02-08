using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace reciprocity.SecurityTheatre
{
    public class BearerToken
    {
        private readonly string _token;

        private BearerToken(string token)
        {
            _token = token;
        }

        public bool TimingSafeEquals(string other)
        {
            if (_token.Length != other.Length)
            {
                return false;
            }
            uint acc = 0;
            for (int i = 0; i < _token.Length; i++)
            {
                acc |= (uint)(_token[i] ^ other[i]);
            }
            return acc == 0;
        }

        public byte[] ToBytes()
        {
            return Base64UrlEncoder.DecodeBytes(_token);
        }

        public override string ToString()
        {
            return _token;
        }

        public static BearerToken CreateRandom()
        {
            byte[] bytes = new byte[40];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetNonZeroBytes(bytes);
            }
            return new BearerToken(Base64UrlEncoder.Encode(bytes));
        }

        public static BearerToken FromBytes(byte[] bytes)
        {
            if (bytes.Length != 40)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), "bytes.Length must be 40");
            }

            return new BearerToken(Base64UrlEncoder.Encode(bytes));
        }
    }
}
