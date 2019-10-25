using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Uploader.Tests.Stubs
{
    public class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<(HttpRequestMessage, string)> _requests;

        public IReadOnlyList<(HttpRequestMessage request, string body)> PerformedRequests => _requests;

        public StubHttpMessageHandler()
        {
            _requests = new List<(HttpRequestMessage, string)>();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add((request, await request.Content.ReadAsStringAsync()));

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }
}
