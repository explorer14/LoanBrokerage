using System.Threading.Tasks;

namespace LoanRequestSender.Filter
{
    public interface ILoanRequestSenderFilter
    {
        Task<LoanQuoteResponse> GetLoanQuotesFromRegisteredBanks(
            LoanQuoteRequest loanQuoteRequest);
    }
}