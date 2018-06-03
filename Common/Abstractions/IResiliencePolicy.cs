using System;
using System.Threading.Tasks;

namespace Common.Abstractions
{
    public interface IResiliencePolicy
    {
        Task<TOut> Execute<TOut>(Func<Task<TOut>> actionToExecute);
    }
}