using Common.Abstractions;
using CreditChecker.Filter;
using Moq;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CreditChecker.UnitTests
{
    public class CreditCheckerTests
    {
        [Fact]
        public async void Given_One_or_More_Loan_Requests_Each_Is_Enriched_With_Credit_Score_Info()
        {
            IReadOnlyCollection<LoanRequest> mockLoanRequests = MockData();
            ICreditCheckFilter creditCheckFilter = new CreditCheckFilter(
                new MockHttpMessageHandlerFactory(),
                MockLogger());
            List<EnrichedLoanRequest> loanRequestsWithCreditReport =
                new List<EnrichedLoanRequest>();

            foreach (var mockLoanRequest in mockLoanRequests)
            {
                loanRequestsWithCreditReport.Add(await creditCheckFilter
                .PerformCreditCheck(mockLoanRequest));
            }

            Assert.True(loanRequestsWithCreditReport.Any());
            Assert.True(
                loanRequestsWithCreditReport.Count(x => x.CreditCheckReport != null) ==
                loanRequestsWithCreditReport.Count);
        }

        [Fact]
        public async void Given_Credit_Check_API_Returns_Error_Code_Its_Logged_And_There_Is_No_Entry_For_Failing_Record()
        {
            var cannedResponses = MockDataWithCannedResponses();
            ICreditCheckFilter creditCheckFilter = new CreditCheckFilter(
                new MockHttpMessageHandlerFactory2(cannedResponses.Item1),
                MockLogger());

            List<EnrichedLoanRequest> loanRequestsWithCreditReport =
                new List<EnrichedLoanRequest>();

            foreach (var item in cannedResponses.Item2)
            {
                var creditCheckedLoanRequest = await creditCheckFilter
                    .PerformCreditCheck(item);

                if (creditCheckedLoanRequest != null)
                    loanRequestsWithCreditReport.Add(
                        creditCheckedLoanRequest);
            }

            Assert.True(
                loanRequestsWithCreditReport.Any(),
                "Because there are no enriched loan requests!");
            Assert.True(
                loanRequestsWithCreditReport.Count == 1,
                "Because there are more than one enriched loan requests " +
                "even though there should be 2 out of 3 loan requests are invalid!");
        }

        private IReadOnlyCollection<LoanRequest> MockData()
        {
            return new[]
            {
                new LoanRequest
                {
                    CitizenServiceNumber = "12345",
                    RequestedLoanAmount = 1000.0m
                },
                new LoanRequest
                {
                    CitizenServiceNumber = "12346",
                    RequestedLoanAmount = 2000.0m
                }
            };
        }

        private (IDictionary<string, HttpResponseMessage>,
            IReadOnlyCollection<LoanRequest>) MockDataWithCannedResponses()
        {
            var dict = new Dictionary<string, HttpResponseMessage>();
            var key1 = new LoanRequest
            {
                CitizenServiceNumber = "12345",
                RequestedLoanAmount = 1000.0m
            };
            var value1 = new HttpResponseMessage(HttpStatusCode.OK);
            value1.Content = new StringContent("{\"BSN\":\"12345\", \"CreditRating\":\"A\", " +
                "\"Score\":\"9\", \"Status\":\"Good credit\"}",
                Encoding.UTF8,
                "application/json");

            var key2 = new LoanRequest
            {
                CitizenServiceNumber = "12346",
                RequestedLoanAmount = 1001.0m
            };
            var value2 = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            value2.Content = null;

            var key3 = new LoanRequest
            {
                CitizenServiceNumber = "12347",
                RequestedLoanAmount = 1002.0m
            };
            var value3 = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            value3.Content = null;

            dict.Add(key1.CitizenServiceNumber, value1);
            dict.Add(key2.CitizenServiceNumber, value2);
            dict.Add(key3.CitizenServiceNumber, value3);

            List<LoanRequest> loanRequests = new List<LoanRequest>();
            loanRequests.Add(key1);
            loanRequests.Add(key2);
            loanRequests.Add(key3);

            return (dict, loanRequests);
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
                 MockContent(request.Content);
            httpResponseMessage.StatusCode = HttpStatusCode.OK;

            return await Task.FromResult(httpResponseMessage);
        }

        private HttpContent MockContent(HttpContent content)
        {
            string mockJsonContent = "{\"BSN\":\"12345\", \"CreditRating\":\"A\", " +
                "\"Score\":\"9\", \"Status\":\"Good credit\"}";

            return new StringContent(
                mockJsonContent,
                Encoding.UTF8,
                "application/json");
        }
    }

    public class MockHttpMessageHandler2
        : HttpMessageHandler
    {
        private readonly IDictionary<string, HttpResponseMessage> cannedResponses;

        public MockHttpMessageHandler2(
            IDictionary<string, HttpResponseMessage> cannedResponses)
        {
            this.cannedResponses = cannedResponses;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var item = JsonConvert.DeserializeObject<LoanRequest>(
                await request.Content.ReadAsStringAsync());

            return await Task.FromResult(cannedResponses[item.CitizenServiceNumber]);
        }

        private HttpContent MockContent(HttpContent content)
        {
            string mockJsonContent = "{\"BSN\":\"12345\", \"CreditRating\":\"A\", " +
                "\"Score\":\"9\", \"Status\":\"Good credit\"}";

            return new StringContent(
                mockJsonContent,
                Encoding.UTF8,
                "application/json");
        }
    }

    public class MockHttpMessageHandlerFactory
        : IHttpMessageHandlerFactory
    {
        public Uri BaseUri =>
            new Uri("http://localhost:1111");

        public HttpMessageHandler Create() =>
            new MockHttpMessageHandler();
    }

    public class MockHttpMessageHandlerFactory2
        : IHttpMessageHandlerFactory
    {
        private readonly IDictionary<string, HttpResponseMessage> cannedResponses;

        public MockHttpMessageHandlerFactory2(
            IDictionary<string, HttpResponseMessage> cannedResponses)
        {
            this.cannedResponses = cannedResponses;
        }

        public Uri BaseUri =>
            new Uri("http://localhost:1111");

        public HttpMessageHandler Create() =>
            new MockHttpMessageHandler2(cannedResponses);
    }
}