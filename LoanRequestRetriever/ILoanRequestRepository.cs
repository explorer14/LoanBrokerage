using System.Collections.Generic;
using System.Threading.Tasks;

namespace LoanRequestRetriever
{
    public interface ILoanRequestRepository
    {
        Task<IReadOnlyCollection<LoanRequest>> AllSubmittedLoanRequests();
    }
}