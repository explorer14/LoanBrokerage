using System.Threading.Tasks;

namespace CustomerNotifier.Filter
{
    public interface INotificationService
    {
        Task Notify(
            string destinationAddress,
            string message);
    }
}