using CustomerNotifier.Filter;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Serilog;
using System.IO;
using System.Threading.Tasks;

namespace CustomerNotifier
{
    public class Functions
    {
        private readonly ICustomerNotifierFilter customerNotifierFilter;

        public Functions(
            ICustomerNotifierFilter customerNotifierFilter)
        {
            this.customerNotifierFilter = customerNotifierFilter;
        }

        public async Task ProcessQueueMessage(
            [QueueTrigger("aggregated-loan-quotes")] string message,
            TextWriter log)
        {
            await customerNotifierFilter
                .NotifyCustomer(
                    ConvertQueueMessageToLoanQuote(message));
        }

        private CustomerLoanQuote ConvertQueueMessageToLoanQuote(
            string message)
        {
            return JsonConvert
                .DeserializeObject<CustomerLoanQuote>(
                message);
        }
    }
}