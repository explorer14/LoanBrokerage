using Common.Abstractions;
using Microsoft.Azure.WebJobs;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LoanRequestRetriever
{
    public class Functions
    {
        private readonly ILoanRequestRepository loanRequestRepository;
        private readonly IPipe<IReadOnlyCollection<LoanRequest>> pipe;
        private readonly ILogger logger;

        public Functions(ILoanRequestRepository loanRequestRepository,
            IPipe<IReadOnlyCollection<LoanRequest>> pipe,
            ILogger logger)
        {
            this.loanRequestRepository = loanRequestRepository;
            this.pipe = pipe;
            this.logger = logger;
        }

        public async Task ProcessQueueMessage(
            [TimerTrigger("%TriggerTime%")] TimerInfo timer,
            TextWriter log)
        {
            logger.Information(
                "LoanRequestRetriever triggered at {@timer}...retrieving submitted loan requests...",
                timer);
            var submittedLoanRequests = await loanRequestRepository
                .AllSubmittedLoanRequests();
            logger.Information(
                $"Loaded {submittedLoanRequests.Count} loan requests!");
            await pipe.Write(submittedLoanRequests);
            logger.Information("Loan Request Retriver Done!!!");
        }
    }
}