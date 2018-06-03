using Common.Abstractions;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CreditChecker.Filter
{
    public class MockHttpMessageHandlerFactory
        : IHttpMessageHandlerFactory
    {
        public Uri BaseUri =>
            new Uri("http://localhost:3333");

        public HttpMessageHandler Create() =>
            new MockHttpMessageHandler();
    }

    public class HttpMessageHandlerFactory
        : IHttpMessageHandlerFactory
    {
        // TODO: replace the hard coded URL with a config object that gets
        // injected in @ bootstrap time
        public Uri BaseUri =>
            new Uri("http://localhost:9999");

        public HttpMessageHandler Create() =>
            new HttpClientHandler();
    }

    public class MockHttpMessageHandler
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage httpResponseMessage =
                new HttpResponseMessage();
            httpResponseMessage.Content =
                 MockContentPerRequest(request);
            httpResponseMessage.StatusCode = HttpStatusCode.OK;

            return await Task.FromResult(httpResponseMessage);
        }

        private HttpContent MockContentPerRequest(
            HttpRequestMessage request)
        {
            var reqJson = request.Content.ReadAsStringAsync().Result;
            dynamic loanReq = JsonConvert.DeserializeObject(reqJson);
            var ccr = new CreditCheckResponse();
            ccr.BSN = loanReq.CitizenServiceNumber;
            ccr.CreditRating = 'A';
            ccr.Score = 9;
            ccr.Status = "Awesome Credit History";

            return new StringContent(
                JsonConvert.SerializeObject(ccr),
                Encoding.UTF8,
                "application/json");
        }

        public class CreditCheckResponse
        {
            public string BSN { get; set; }
            public char CreditRating { get; set; }
            public int Score { get; set; }
            public string Status { get; set; }
        }
    }
}