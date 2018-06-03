using System.Threading.Tasks;

namespace CustomerNotifier.Filter
{
    public interface ICustomerRepository
    {
        Task<CustomerInfo> CustomerByBSN(string bsn);
    }
}