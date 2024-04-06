using System;
using System.Security.Cryptography;
using System.Text;
using ThrowException.CSharpLibs.BytesUtilLib;

namespace Speculatores
{
    public static class Util
    {
        public static Guid CreateId(string text, params object[] args)
        {
            using (var hash = SHA256.Create())
            {
                return new Guid(hash.ComputeHash(Encoding.UTF8.GetBytes(string.Format(text, args))).Part(0, 16));
            }
        }
    }
}
