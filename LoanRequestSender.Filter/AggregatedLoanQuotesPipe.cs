using Azure.StorageQueue.Helper;
using Common.Abstractions;
using Serilog;
using System.Threading.Tasks;

namespace LoanRequestSender.Filter
{
    public class AggregatedLoanQuotesPipe : IPipe<LoanQuoteResponse>
    {
        private readonly SimpleQueueHelper simpleQueueHelper;
        private readonly ILogger logger;

        public AggregatedLoanQuotesPipe(
            SimpleQueueHelper simpleQueueHelper,
            ILogger logger)
        {
            this.simpleQueueHelper = simpleQueueHelper;
            this.logger = logger;
        }

        public async Task Write(LoanQuoteResponse payload)
        {
            logger.Information("Queuing the loan quote response for the next stage... ");
            await simpleQueueHelper.SendMessage(payload);
        }
    }
}