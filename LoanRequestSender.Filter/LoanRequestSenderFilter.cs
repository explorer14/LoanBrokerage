using Common.Abstractions;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
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

        public LoanRequestSenderFilter(
            IHttpMessageHandlerFactory httpMessageHandlerFactory,
            ILogger logger,
            IResiliencePolicy resiliencePolicy)
        {
            this.httpMessageHandlerFactory = httpMessageHandlerFactory;
            this.logger = logger;
            this.resiliencePolicy = resiliencePolicy;
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

                var result = await resiliencePolicy.Execute(async () =>
                {
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
                            // atleast in Azure WebJob's case, throwing an exception
                            // will force it to retry atleast 5 times, before it gives up
                            // and moves the message to a poison queue. Out of the box
                            // resiliency. Even for the HttpRequestException, Polly will handle it
                            // at first but then if the error doesn't resolve itself, the exception
                            // will be thrown back to WebJobs runtime who's inbuilt resilience will kick
                            // in automatically and it will simply retry the operation 5 times regardless
                            // of what Polly reported back originally. In essence, it doesn't make a
                            // whole lot of sense to use Polly with WebJobs unless you
                            // want nicer looking messages that you get when you override the OnBreak/OnRetry methods
                            // with Polly.
                            throw new Exception();
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
                });

                if (result != null)
                    loanQuoteResponse.Quotes.Add(result);
            }

            return loanQuoteResponse;
        }
    }
}