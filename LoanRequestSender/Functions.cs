using Common.Abstractions;
using LoanRequestSender.Filter;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Serilog;
using System.IO;
using System.Threading.Tasks;

namespace LoanRequestSender
{
    public class Functions
    {
        private readonly ILoanRequestSenderFilter loanRequestSenderFilter;
        private readonly IPipe<LoanQuoteResponse> pipe;
        private readonly ILogger logger;

        public Functions(ILoanRequestSenderFilter loanRequestSenderFilter, IPipe<LoanQuoteResponse> pipe, ILogger logger)
        {
            this.loanRequestSenderFilter = loanRequestSenderFilter;
            this.pipe = pipe;
            this.logger = logger;
        }

        public async Task ProcessQueueMessage([QueueTrigger("credit-checked-loan-requests")] string message, TextWriter log)
        {
            logger.Information("Retrieving credit checked loan request...");
            dynamic creditCheckedLoanRequest = JsonConvert.DeserializeObject(message);
            var loanQuoteRequest = new LoanQuoteRequest
            {
                BSN = creditCheckedLoanRequest.OriginalLoanRequest.CitizenServiceNumber,

                CreditRating = $"{creditCheckedLoanRequest.CreditCheckReport.CreditScore} " +
                $"({creditCheckedLoanRequest.CreditCheckReport.CreditRating})",

                LoanAmount = creditCheckedLoanRequest.OriginalLoanRequest.RequestedLoanAmount
            };
            logger.Information("Now sending the loan quote {@request} to all the registered banks...", loanQuoteRequest);
            var quoteResponse = await loanRequestSenderFilter.GetLoanQuotesFromRegisteredBanks(loanQuoteRequest);
            await pipe.Write(quoteResponse);
            logger.Information("Loan Request Sender Done!");
        }
    }
}