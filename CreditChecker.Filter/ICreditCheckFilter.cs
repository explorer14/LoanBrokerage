using System.Threading.Tasks;

namespace CreditChecker.Filter
{
    public interface ICreditCheckFilter
    {
        Task<EnrichedLoanRequest> PerformCreditCheck(
            LoanRequest loanRequest);
    }
}