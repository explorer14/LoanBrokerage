using Common.Abstractions;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoanRequestSender.Filter
{
    public class MockHttpMessageHandlerFactory
        : IHttpMessageHandlerFactory
    {
        public Uri BaseUri =>
            new Uri("http://localhost:7777");

        public HttpMessageHandler Create() =>
            new MockHttpMessageHandler();
    }

    public class MockHttpMessageHandler
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var loanQuote = new LoanQuote();
            loanQuote.ApprovableAmount = 100;
            loanQuote.Bank = "Test";

            HttpResponseMessage httpResponseMessage =
                new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            httpResponseMessage.Content = new StringContent(
                JsonConvert.SerializeObject(loanQuote),
                Encoding.UTF8,
                "application/json");

            return await Task.FromResult(httpResponseMessage);
        }
    }
}