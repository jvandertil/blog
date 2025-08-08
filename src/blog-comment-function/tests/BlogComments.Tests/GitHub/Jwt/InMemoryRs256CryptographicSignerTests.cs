using System;
using BlogComments.GitHub.Helpers;
using BlogComments.GitHub.Jwt;
using Shouldly;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlogComments.Tests
{
    [TestClass]
    public class InMemoryRs256CryptographicSignerTests
    {
        [TestMethod]
        public void CalculateSignature_CalculatesValidSignature()
        {
            // This is a key generated for test purposes and is not used for anything else.
            const string privateKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEAweU2Dn8oPbhRcvHtdOfWkcjFJfNwi4kXBtj+3cYWJ++NS4/Q
EFpzdfCgPP+Kq8oEFN5MUOZKPWAk5eqV+YAwYSgbX8OOE9X593ErZGfW9Wb8c/dc
5tnts/28co0X38Y1tfn1x5i+1YVkmzsYv0lVvhxGdQEl29yXUO+BGKRbcZpP3Umg
/GVfwG5kgaVMA2mERAplkpBDb/63lJXfYUzveUYeeh+lWEBL7fHfY1+u2CciPrYz
vGkWKM4CoRIuQ21CCu/QUYyxkd9+lrLgpSvjSvawNN0geHRck7SkYhhHtn71+lUB
RtK95G8Wv9EBHU6R1BdgILYvb5+i2ZClC+C5sQIDAQABAoIBAGuF52c8np0zdH9w
p8TXuAaaNrHoAPZwLIPQm+1yJuE4l7taYgBfmH3D1ahd8ZF4crD74YhPXMYSZgPW
BhsZOjr2mc+OS2C2nWrZqD2C1BK8bK0GdM9T9NyGjhVcJuwiJ7Dlj6WDD/iqg3MN
35hcW58UYQILg+obtxHb71Qx+L/S4gTUipfgOGCvZ9hxtQXwleonplOGsC0Y0f6P
75XaQfEf6A09Aum6UkfLgdrI6+nwfkMfFAZGOblxEqRU7MI6uaPmhrcdqKGtbYvG
4/m6LTf/TBXE34fxKME8ekJqHSVHkt8ty3VPgQJVDr8MbDxZqBuEaJ9vxCah0Bcs
hUraw5ECgYEA8v42M+jMRHkp0LGSVo246oSvG+Z9U1CgRQI6oXh6LHpN1r2ga+hz
iqXEkbH9xdMj4nDDqSwv5SLM5VkwMY0zOrBzm1tZ7QquBTyMik5gmgfkq0zDG9Gj
W+aNIahwSsDDYtGv/4Xrqi0FvWKzDpNZXdYGKPGxV5GCC0h/zpHIg8UCgYEAzEYz
/ppEO00S22xR8zN6oWf4ycXneoZGVAiFHD2bH2OvnPQdrZKsSga39cK4JRznLz1d
8nW43SyOvYsAQvmAH9y2o7lbTsOhXcmcY60Pk/9hh/62kaN3NTjJLlTz3LPhXzJq
vAKaLredhf5m9qd5Eg9vIddo9Jca9aV5l8TEgP0CgYB+Tba82qe6e8RJbtNi2/2f
IOKoPPEtqj30QMla/vV6QwRMt3o4PLY5/hojpQIEntALNpPtTkOC9cjM+cP8LanQ
OsGMojom5SM5I5TlmwHJborko1zTC+++qCL5uMTNhk7JAbdauTCa3xYZr8DktaCB
Dutawu5sVvzigoe0RsCUBQKBgQCbbC7TYPzZQeM/IEOaD2kWtc0NeI6PIusPtQvS
WO1WDLrpaLPMBPUhvcrqKqWBV2RvBPoeKIPnhKd2f/RLARsDIyOznqxiWWbFvUhI
bryTlpPWrW6rkPx6eiJYJjsFibfIfsvHERPOx9YKxW4B7ZqoqyWbUhKBRxc0IBtL
5mK84QKBgA/8KtTCeMDEHro7e5Ho/0t2dzY/S3TOrqioduT1t1+GYUklyWNDfpT/
vJZcnFq2CZ2CpfhEDOoRbIBs1+l8ktaAZJBr6o2KUPUE2AzkHNfZeHjZ/6AXmKuy
1jLyAX6IFfh9JahO5kEnkyRtwy7qdWqwJA64rja2JiUYy+34IIvv
-----END RSA PRIVATE KEY-----
";

            const string expectedSignature = "IS2ZVZEovipo6NLmLCk0ALb50OSHdVljwFN5CflRZrVW5cCvXjdv8zViq2yWkGvCpgD63llb6ydPyCDoSsXKHmhO2ACpWNJs/9MfBBJ/M3NvP552cmB46xh2z21ufDZYjEMLCCKaTY1qMU9reB3uMtohARJkH4+s+/69XtrVQ1ZOmz7QHnfkM/I99XtCxRvPBNaaU0bojM+cxYzzERtCNHhKUkPuwkCnUYFqniGVsEOaYbQ91o7L/Mr+JlSvHItBTxKIzRXJV2GpS8uMTIwClqMBYZa7nPIVv/RHS60NRGr45U4w5xEBTibFAeFmKZ16CCV2tcYWQs/Tz7sAo/ucxA==";

            var signer = new InMemoryRs256CryptographicSigner(PemPrivateKeyConverter.ExtractRsaPrivateKey(privateKey));

            var signature = signer.CalculateSignature([]);
            var signatureBase64 = Convert.ToBase64String(signature);

            signatureBase64.ShouldBe(expectedSignature);
        }
    }
}
