using Common.Abstractions;
using CreditChecker.Filter;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Serilog;
using System.IO;
using System.Threading.Tasks;

namespace CreditChecker
{
    public class Functions
    {
        private readonly ICreditCheckFilter creditCheckFilter;
        private readonly IPipe<EnrichedLoanRequest> pipe;
        private readonly ILogger logger;

        public Functions(
            ICreditCheckFilter creditCheckFilter,
            IPipe<EnrichedLoanRequest> pipe,
            ILogger logger)
        {
            this.creditCheckFilter = creditCheckFilter;
            this.pipe = pipe;
            this.logger = logger;
        }

        public async Task ProcessQueueMessage(
            [QueueTrigger("submitted-loan-requests")] string loanRequestPayload,
            TextWriter log)
        {
            logger.Information("Retrieving submitted loan requests...");
            var submittedLoanRequest = JsonConvert
                .DeserializeObject<LoanRequest>(
                loanRequestPayload);
            logger.Information("Sending request {@request} " +
                $"for credit check...", submittedLoanRequest);
            var enrichedLoanRequest = await creditCheckFilter
                .PerformCreditCheck(submittedLoanRequest);
            logger.Information("Credit Check finished! Response was: {@response} " +
                "Preparing for request submissions...", enrichedLoanRequest);
            await pipe.Write(enrichedLoanRequest);
            logger.Information("Credit Checker Done!!!");
        }
    }
}