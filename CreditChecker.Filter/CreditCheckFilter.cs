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

        public CreditCheckFilter(IHttpMessageHandlerFactory httpMessageHandlerFactory, ILogger logger)
        {
            this.httpMessageHandlerFactory = httpMessageHandlerFactory;
            this.logger = logger;
        }

        public async Task<EnrichedLoanRequest> PerformCreditCheck(LoanRequest loanRequest)
        {
            if (loanRequest.RequestedLoanAmount == 0 || string.IsNullOrWhiteSpace(loanRequest.CitizenServiceNumber))
            {
                throw new ArgumentException("Requested loan amount must be > 0 and BSN must be provided");
            }

            EnrichedLoanRequest enrichedLoanRequest = default(EnrichedLoanRequest);
            enrichedLoanRequest = await CreditCheckLoanRequest(loanRequest, enrichedLoanRequest);

            return await Task.FromResult(enrichedLoanRequest);
        }

        private async Task<EnrichedLoanRequest> CreditCheckLoanRequest(LoanRequest loanRequest, EnrichedLoanRequest enrichedLoanRequest)
        {
            using (var httpClient = new HttpClient(httpMessageHandlerFactory.Create(), true))
            {
                httpClient.BaseAddress = httpMessageHandlerFactory.BaseUri;

                var stringContent = new StringContent(JsonConvert.SerializeObject(loanRequest), Encoding.UTF8, "application/json");
                httpClient.DefaultRequestHeaders.Add("Request-Id", loanRequest.CitizenServiceNumber);
                logger.Information("Contacting credit check agency...");
                var response = await httpClient.PutAsync("api/creditcheck", stringContent);

                if (response.IsSuccessStatusCode)
                {
                    enrichedLoanRequest = await ProcessSuccessResponse(loanRequest, enrichedLoanRequest, response);
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

            return enrichedLoanRequest;
        }

        private async Task<EnrichedLoanRequest> ProcessSuccessResponse(LoanRequest loanRequest, EnrichedLoanRequest enrichedLoanRequest, HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

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
            return enrichedLoanRequest;
        }
    }
}