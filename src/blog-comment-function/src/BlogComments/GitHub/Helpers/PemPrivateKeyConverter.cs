using System;

namespace BlogComments.GitHub.Helpers
{
    public static class PemPrivateKeyConverter
    {
        public static ReadOnlyMemory<byte> ExtractRsaPrivateKey(string pemEncodedKey)
        {
            var key = pemEncodedKey
                .Replace("-----END RSA PRIVATE KEY-----", "")
                .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                .Replace("\r\n", "")
                .Replace("\n", "");

            var keyBytes = Convert.FromBase64String(key);

            return keyBytes.AsMemory();
        }
    }
}
