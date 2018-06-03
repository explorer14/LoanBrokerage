using System.Threading.Tasks;

namespace CustomerNotifier.Filter
{
    public interface ICustomerNotifierFilter
    {
        Task NotifyCustomer(
            CustomerLoanQuote customerLoanQuote);
    }
}