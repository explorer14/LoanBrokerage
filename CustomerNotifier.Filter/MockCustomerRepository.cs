using Serilog;
using System.Threading.Tasks;

namespace CustomerNotifier.Filter
{
    public class MockCustomerRepository
        : ICustomerRepository
    {
        private readonly ILogger logger;

        public MockCustomerRepository(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task<CustomerInfo> CustomerByBSN(
            string bsn)
        {
            logger.Information("Retrieved customer information for {bsn}", bsn);
            return await Task.FromResult(
                new CustomerInfo
                {
                    Email = "test@gmail.com"
                });
        }
    }
}