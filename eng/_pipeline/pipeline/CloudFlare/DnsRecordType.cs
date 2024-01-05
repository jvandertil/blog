using System.Text.Json.Serialization;

namespace Vandertil.Blog.Pipeline.CloudFlare
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DnsRecordType
    {
        A,
        AAAA,
        CNAME,
        HTTPS,
        TXT,
        SRV,
        LOC,
        MX,
        NS,
        SPF,
        CERT,
        DNSKEY,
        DS,
        NAPTR,
        SMIMEA,
        SSHFP,
        SVCB,
        TLSA,
        URI
    }
}
