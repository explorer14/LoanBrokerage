using Azure.StorageQueue.Helper;
using Common.Abstractions;
using Serilog;
using System.Threading.Tasks;

namespace CreditChecker.Filter
{
    public class QueueBackedLoanQuoteSubmissionPipe
        : IPipe<EnrichedLoanRequest>
    {
        private readonly SimpleQueueHelper queueHelper;
        private readonly ILogger logger;

        public QueueBackedLoanQuoteSubmissionPipe(
            SimpleQueueHelper queueHelper,
            ILogger logger)
        {
            this.queueHelper = queueHelper;
            this.logger = logger;
        }

        public async Task Write(EnrichedLoanRequest payload)
        {
            await queueHelper.SendMessage(payload);
            logger.Information(
                "Finished queueing {@request} for submission to registered banks!",
                payload);
        }
    }
}