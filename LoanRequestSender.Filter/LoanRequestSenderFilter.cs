using Common.Abstractions;
using Newtonsoft.Json;
using Serilog;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LoanRequestSender.Filter
{
    public class LoanRequestSenderFilter :
        ILoanRequestSenderFilter
    {
        private readonly IHttpMessageHandlerFactory httpMessageHandlerFactory;
        private readonly ILogger logger;
        private readonly IResiliencePolicy resiliencePolicy;

        public LoanRequestSenderFilter(IHttpMessageHandlerFactory httpMessageHandlerFactory, ILogger logger, IResiliencePolicy resiliencePolicy)
        {
            this.httpMessageHandlerFactory = httpMessageHandlerFactory;
            this.logger = logger;
            this.resiliencePolicy = resiliencePolicy;
        }

        public async Task<LoanQuoteResponse> GetLoanQuotesFromRegisteredBanks(LoanQuoteRequest loanQuoteRequest)
        {
            var loanQuoteResponse = new LoanQuoteResponse();
            loanQuoteResponse.BSN = loanQuoteRequest.BSN;
            loanQuoteResponse.OriginalAmountRequested = loanQuoteRequest.LoanAmount;
            var allRegisteredBanks = RegisteredBanks.All();

            foreach (var bank in allRegisteredBanks)
            {
                logger.Information("Sending {@request} to {bank} bank ...", loanQuoteRequest, bank.Name);

                var result = await ExecuteWithResilience(loanQuoteRequest, bank);

                if (result != null)
                {
                    loanQuoteResponse.Quotes.Add(result);
                }
            }

            return loanQuoteResponse;
        }

        private async Task<LoanQuote> ExecuteWithResilience(LoanQuoteRequest loanQuoteRequest, IRegisteredBank bank)
        {
            return await resiliencePolicy.Execute(async () =>
            {
                using (var httpClient = new HttpClient(httpMessageHandlerFactory.Create(), true))
                {
                    return await SendLoanRequest(loanQuoteRequest, bank, httpClient);
                }
            });
        }

        private async Task<LoanQuote> SendLoanRequest(LoanQuoteRequest loanQuoteRequest, IRegisteredBank bank, HttpClient httpClient)
        {
            httpClient.BaseAddress = bank.BaseUri;
            var stringContent = new StringContent(JsonConvert.SerializeObject(loanQuoteRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync(bank.Endpoint, stringContent);

            if (!response.IsSuccessStatusCode)
            {
                logger.Error("Loan quote {@request} sending to {bank} failed : " +
                    $"{response.StatusCode}: {response.ReasonPhrase}",
                    loanQuoteRequest,
                    bank.Name);
                return null;
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                var quoteResponse = JsonConvert.DeserializeObject<LoanQuote>(content);
                logger.Information(
                    "Request {@request} succeeded! Request was successfully processed by {bank}! Response was {@response}",
                    loanQuoteRequest,
                    bank.Name,
                    quoteResponse);

                return quoteResponse;
            }
        }
    }
}