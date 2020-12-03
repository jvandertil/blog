using System;
using System.IO;
using System.Security.Cryptography;
using BlogComments.GitHub.Helpers;

namespace BlogComments.GitHub.Jwt
{
    public sealed class FileRs256CryptographicSigner : ICryptographicSigner, IDisposable
    {
        public string Algorithm => "RS256";

        private readonly RSA _rsa;
        private bool _disposed;

        public FileRs256CryptographicSigner(string path)
        {
            var keyBytes = PemPrivateKeyConverter.ExtractRsaPrivateKey(File.ReadAllText(path));

            _rsa = RSA.Create();
            _rsa.ImportRSAPrivateKey(keyBytes.Span, out _);
        }

        public byte[] CalculateSignature(byte[] data)
        {
            return _rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _rsa.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
