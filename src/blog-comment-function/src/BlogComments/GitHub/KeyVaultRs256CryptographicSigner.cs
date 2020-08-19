using Azure.Security.KeyVault.Keys.Cryptography;

namespace BlogComments.GitHub
{
    public sealed class KeyVaultRs256CryptographicSigner : ICryptographicSigner
    {
        private readonly CryptographyClient _keyVault;

        public string Algorithm => "RS256";

        public KeyVaultRs256CryptographicSigner(CryptographyClient client)
        {
            _keyVault = client;
        }

        public byte[] CalculateSignature(byte[] data)
        {
            var result = _keyVault.SignData(SignatureAlgorithm.RS256, data);

            return result.Signature;
        }
    }
}
