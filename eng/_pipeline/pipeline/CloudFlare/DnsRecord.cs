namespace Vandertil.Blog.Pipeline.CloudFlare
{
    public class DnsRecord
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public DnsRecordType Type { get; set; }

        public string? Content { get; set; }

        public bool Proxied { get; set; }

        public int Ttl { get; set; }
    }
}
