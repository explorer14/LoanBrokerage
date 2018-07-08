using Newtonsoft.Json;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CustomerNotifier.Filter
{
    public class CustomerNotifierFilter : ICustomerNotifierFilter
    {
        private readonly INotificationService notificationService;
        private readonly ICustomerRepository customerRepository;
        private readonly ILogger logger;

        public CustomerNotifierFilter(INotificationService notificationService, ICustomerRepository customerRepository, ILogger logger)
        {
            this.notificationService = notificationService;
            this.customerRepository = customerRepository;
            this.logger = logger;
        }

        public async Task NotifyCustomer(CustomerLoanQuote customerLoanQuote)
        {
            if (customerLoanQuote == null)
            {
                throw new ArgumentNullException($"{nameof(customerLoanQuote)} is null!");
            }

            if (customerLoanQuote?.Quotes == null)
            {
                throw new ArgumentNullException($"{nameof(CustomerLoanQuote.Quotes)} is null!");
            }

            logger.Information("Retrieving customer information from the database...");
            var customerInfo = await customerRepository.CustomerByBSN(customerLoanQuote.BSN);
            await notificationService.Notify(customerInfo.Email, JsonConvert.SerializeObject(customerLoanQuote.Quotes));
        }
    }
}