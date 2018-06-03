using Common.Abstractions;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;
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

        public LoanRequestSenderFilter(
            IHttpMessageHandlerFactory httpMessageHandlerFactory,
            ILogger logger)
        {
            this.httpMessageHandlerFactory = httpMessageHandlerFactory;
            this.logger = logger;
        }

        public async Task<LoanQuoteResponse> GetLoanQuotesFromRegisteredBanks(
            LoanQuoteRequest loanQuoteRequest)
        {
            var loanQuoteResponse = new LoanQuoteResponse();
            loanQuoteResponse.BSN = loanQuoteRequest.BSN;
            loanQuoteResponse.OriginalAmountRequested = loanQuoteRequest.LoanAmount;

            // Since the number of banks is very unlikely to be too high to
            // create a problem with a new HttpClient per request, I am
            // gonna just let it be like this for now.
            var allRegisteredBanks = RegisteredBanks.All();

            // this code also assumes that all banks provide a JSON API
            // In a more realistic scenario they will probably have a mixed
            // content type like JSON, XML, etc in which case, it will be a better
            // idea to create a strongly typed client per registered bank that can
            // encapsulate bank specific requirements. But that is beyond the scope of this
            // exercise. Also, no auth!
            foreach (var bank in allRegisteredBanks)
            {
                logger.Information(
                    "Sending {@request} to {bank} bank ...",
                    loanQuoteRequest,
                    bank.Name);
                using (var httpClient = new HttpClient(
                    httpMessageHandlerFactory.Create(),
                    true))
                {
                    httpClient.BaseAddress = bank.BaseUri;

                    var stringContent = new StringContent(
                            JsonConvert.SerializeObject(loanQuoteRequest),
                            Encoding.UTF8,
                            "application/json");
                    var response = await httpClient.PutAsync(bank.Endpoint,
                        stringContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        logger.Error("Loan quote {@request} sending to {bank} failed : " +
                            $"{response.StatusCode}: {response.ReasonPhrase}",
                            loanQuoteRequest,
                            bank.Name);
                    }
                    else
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var quoteResponse = JsonConvert.DeserializeObject<LoanQuote>(content);
                        loanQuoteResponse.Quotes.Add(quoteResponse);
                        logger.Information(
                            "Request {@request} succeeded! Request was successfully processed by {bank}! Response was {@response}",
                            loanQuoteRequest,
                            bank.Name,
                            quoteResponse);
                    }
                }
            }

            return loanQuoteResponse;
        }
    }
}