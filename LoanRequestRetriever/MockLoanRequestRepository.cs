using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LoanRequestRetriever
{
    internal class MockLoanRequestRepository : ILoanRequestRepository
    {
        public async Task<IReadOnlyCollection<LoanRequest>> AllSubmittedLoanRequests()
        {
            return await Task.FromResult(
                new[]
                {
                    new LoanRequest
                    {
                        RequestedLoanAmount = 100.0m,
                        CitizenServiceNumber = Guid.NewGuid().ToString()
                    },
                    new LoanRequest
                    {
                        RequestedLoanAmount = 200.0m,
                        CitizenServiceNumber = Guid.NewGuid().ToString()
                    },
                    new LoanRequest
                    {
                        RequestedLoanAmount = 300.0m,
                        CitizenServiceNumber = Guid.NewGuid().ToString()
                    },
                    new LoanRequest
                    {
                        RequestedLoanAmount = 400.0m,
                        CitizenServiceNumber = Guid.NewGuid().ToString()
                    },
                    new LoanRequest
                    {
                        RequestedLoanAmount = 500.0m,
                        CitizenServiceNumber = Guid.NewGuid().ToString()
                    },
                    new LoanRequest()
                });
        }
    }
}