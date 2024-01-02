namespace Vandertil.Blog.Pipeline.CloudFlare
{
    public class CloudFlareResponse<T>
    {
        public bool Success { get; set; }

        public required T Result { get; set; }
    }
}
