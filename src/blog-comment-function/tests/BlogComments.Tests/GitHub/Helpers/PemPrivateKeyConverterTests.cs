using System;
using BlogComments.GitHub.Helpers;
using Shouldly;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlogComments.Tests
{
    [TestClass]
    public class PemPrivateKeyConverterTests
    {
        [TestMethod]
        public void ExtractRsaPrivateKey_StripsPemFormat()
        {
            const string input = @"-----BEGIN RSA PRIVATE KEY-----
Zm9v
YmFy
-----END RSA PRIVATE KEY-----
";
            var expected = "foobar"u8;

            ReadOnlyMemory<byte> result = PemPrivateKeyConverter.ExtractRsaPrivateKey(input);

            result.Span.ToArray().ShouldBe(expected.ToArray());
        }
    }
}
