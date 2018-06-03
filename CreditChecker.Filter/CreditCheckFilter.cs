using Common.Abstractions;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CreditChecker.Filter
{
    public class CreditCheckFilter : ICreditCheckFilter
    {
        private readonly IHttpMessageHandlerFactory httpMessageHandlerFactory;
        private readonly ILogger logger;

        public CreditCheckFilter(
            IHttpMessageHandlerFactory httpMessageHandlerFactory,
            ILogger logger)
        {
            this.httpMessageHandlerFactory = httpMessageHandlerFactory;
            this.logger = logger;
        }

        public async Task<EnrichedLoanRequest> PerformCreditCheck(
            LoanRequest loanRequest)
        {
            if (loanRequest.RequestedLoanAmount == 0 || string.IsNullOrWhiteSpace(loanRequest.CitizenServiceNumber))
                throw new ArgumentException("Requested loan amount must be > 0 and BSN must be provided");

            EnrichedLoanRequest enrichedLoanRequest =
                default(EnrichedLoanRequest);

            using (var httpClient = new HttpClient(
                httpMessageHandlerFactory.Create(),
                true))
            {
                httpClient.BaseAddress = httpMessageHandlerFactory.BaseUri;

                // TODO: this is a very chatty approach to credit check.
                // Perhaps a single call with all the loan requests in
                // an array is a better solution as long as the response
                // comes back within the HTTP timeout of 2 minutes.
                // Alternatively, batching can be used to send "x" requests
                // per call, will be helpful if the number of loan requests
                // is really high. At the other end, having a queue behind the HTTP API
                // will also help with load levelling.
                var stringContent = new StringContent(
                    JsonConvert.SerializeObject(loanRequest),
                    Encoding.UTF8,
                    "application/json");
                httpClient.DefaultRequestHeaders.Add(
                    "Request-Id",
                    loanRequest.CitizenServiceNumber);
                logger.Information("Contacting credit check agency...");
                var response = await httpClient.PutAsync("api/creditcheck",
                    stringContent);

                if (response.IsSuccessStatusCode)
                {
                    // TODO: at the moment, the API call returns the result
                    // of the credit check. In the future it will simply return
                    // a 201 Accepted i.e. queued for processing so our application
                    // doesn't have to sit and wait for the response. We could
                    // expose another queue fronted by an API to which the Credit
                    // Agency can write the results of credit check. We will be listening
                    // on that queue and can start processing as soon as there are messages
                    // on that queue.
                    var content = await response
                        .Content
                        .ReadAsStringAsync();

                    dynamic obj = JsonConvert.DeserializeObject(content);

                    enrichedLoanRequest = new EnrichedLoanRequest();
                    enrichedLoanRequest.OriginalLoanRequest = loanRequest;
                    enrichedLoanRequest.CreditCheckReport = new CreditCheckReport
                    {
                        CitizenServiceNumber = loanRequest.CitizenServiceNumber,
                        CreditRating = obj.CreditRating,
                        CreditScore = obj.Score,
                        Description = obj.Status
                    };
                    logger.Information(
                        "Credit check service returned {code} {response} for {@request}",
                        response.StatusCode,
                        response.ReasonPhrase,
                        loanRequest);
                }
                else
                {
                    logger.Error(
                        "Credit check service returned {code} {response} for {@request}",
                        response.StatusCode,
                        response.ReasonPhrase,
                        loanRequest);
                }
            }

            return await Task.FromResult(enrichedLoanRequest);
        }
    }
}