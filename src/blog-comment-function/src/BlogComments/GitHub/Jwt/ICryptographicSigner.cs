namespace BlogComments.GitHub.Jwt
{
    public interface ICryptographicSigner
    {
        string Algorithm { get; }

        byte[] CalculateSignature(byte[] data);
    }
}
