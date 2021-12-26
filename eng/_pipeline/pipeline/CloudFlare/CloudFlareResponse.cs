namespace Vandertil.Blog.Pipeline.CloudFlare
{
    public class CloudFlareResponse<T>
    {
        public bool Success { get; set; }

        public T Result { get; set; }
    }
}
