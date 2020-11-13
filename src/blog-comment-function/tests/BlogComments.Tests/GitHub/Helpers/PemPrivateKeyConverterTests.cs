using System;
using BlogComments.GitHub.Helpers;
using Xunit;

namespace BlogComments.Tests
{
    public class PemPrivateKeyConverterTests
    {
        [Fact]
        public void ExtractRsaPrivateKey_StripsPemFormat()
        {
            const string input = @"-----BEGIN RSA PRIVATE KEY-----
Zm9v
YmFy
-----END RSA PRIVATE KEY-----
";
            var expected = new byte[]
            {
                (byte)'f',
                (byte)'o',
                (byte)'o',
                (byte)'b',
                (byte)'a',
                (byte)'r',
            };

            ReadOnlyMemory<byte> result = PemPrivateKeyConverter.ExtractRsaPrivateKey(input);

            Assert.Equal(expected, result.ToArray());
        }
    }
}
