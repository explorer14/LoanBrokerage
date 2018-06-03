using Common.Abstractions;
using LoanRequestSender.Filter;
using Moq;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LoanRequestSender.UnitTests
{
    public class LoanRequestSenderTests
    {
        [Fact]
        public async void When_A_Valid_LoanQuoteRequest_Is_Sent_To_Registered_Banks_LoanQuoteResponses_Are_Returned()
        {
            var loanRequestSender = new LoanRequestSenderFilter(
                new MockHttpMessageHandlerFactory(),
                MockLogger());
            var quoteResponse = await loanRequestSender
                .GetLoanQuotesFromRegisteredBanks(
                new LoanQuoteRequest
                {
                    BSN = "12345",
                    CreditRating = "Good",
                    LoanAmount = 1000.0m
                });

            Assert.NotNull(quoteResponse);
            // for this exercise, I am going to assume all the banks will
            // always return responses approved or otherwise.
            Assert.True(
                quoteResponse.Quotes.Count == 2,
                $"because number of quotes returned were {quoteResponse.Quotes.Count}");
        }

        private ILogger MockLogger()
        {
            var mockLogger = new Mock<ILogger>();
            mockLogger.Setup(x => x.Information(It.IsAny<string>()));
            mockLogger.Setup(x => x.Warning(It.IsAny<string>()));
            mockLogger.Setup(x => x.Error(It.IsAny<string>()));

            return mockLogger.Object;
        }
    }

    internal class MockHttpMessageHandlerFactory
        : IHttpMessageHandlerFactory
    {
        public Uri BaseUri =>
            throw new NotImplementedException();

        public HttpMessageHandler Create() =>
            new MockHttpMessageHandler();
    }

    internal class MockHttpMessageHandler
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