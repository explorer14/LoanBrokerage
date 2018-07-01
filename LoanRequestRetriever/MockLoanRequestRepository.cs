using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LoanRequestRetriever
{
    internal class MockLoanRequestRepository : ILoanRequestRepository
    {
        public async Task<IReadOnlyCollection<LoanRequest>> AllSubmittedLoanRequests()
        {
            List<LoanRequest> loanRequests = new List<LoanRequest>();

            for (int i = 0; i < 5; i++)
            {
                loanRequests.Add(
                    new LoanRequest
                    {
                        RequestedLoanAmount = (i + 1) * 100,
                        CitizenServiceNumber = Guid.NewGuid().ToString()
                    });
            }

            return await Task.FromResult(loanRequests);
        }
    }
}