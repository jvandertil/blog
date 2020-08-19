using System;
using System.Security.Cryptography;

namespace BlogComments.GitHub
{
    public interface ICryptographicSigner
    {
        string Algorithm { get; }

        byte[] CalculateSignature(byte[] data);
    }
}
