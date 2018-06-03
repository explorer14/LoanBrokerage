using Serilog;
using System.Threading.Tasks;

namespace CustomerNotifier.Filter
{
    public class ConsoleNotificationService
        : INotificationService
    {
        private readonly ILogger logger;

        public ConsoleNotificationService(
            ILogger logger)
        {
            this.logger = logger;
        }

        public async Task Notify(
            string destinationAddress,
            string message)
        {
            logger.Warning(
                $"Notifying {destinationAddress} " +
                $"with a message {message}");

            await Task.CompletedTask;
        }
    }
}