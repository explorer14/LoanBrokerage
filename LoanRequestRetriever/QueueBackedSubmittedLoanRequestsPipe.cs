using Azure.StorageQueue.Helper;
using Common.Abstractions;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LoanRequestRetriever
{
    public class QueueBackedSubmittedLoanRequestsPipe
        : IPipe<IReadOnlyCollection<LoanRequest>>
    {
        private readonly SimpleQueueHelper queueHelper;
        private readonly ILogger logger;

        public QueueBackedSubmittedLoanRequestsPipe(
            SimpleQueueHelper queueHelper,
            ILogger logger)
        {
            this.queueHelper = queueHelper;
            this.logger = logger;
        }

        public async Task Write(IReadOnlyCollection<LoanRequest> payload)
        {
            foreach (var item in payload)
            {
                logger.Information($"Submitting loan request " +
                    "{@payload} to be credit checked...", item);
                await queueHelper.SendMessage(item);
            }
        }
    }
}