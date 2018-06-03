using System.Threading.Tasks;

namespace Common.Abstractions
{
    public interface IPipe<T>
    {
        Task Write(T payload);
    }
}