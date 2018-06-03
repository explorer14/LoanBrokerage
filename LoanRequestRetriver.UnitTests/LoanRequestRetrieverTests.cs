using Common.Abstractions;
using LoanRequestRetriever;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace LoanRequestRetriver.UnitTests
{
    public class LoanRequestRetrieverTests
    {
        [Fact]
        public async void Retrieving_Submitted_Loan_Requests_Will_Retrieve_Them()
        {
            ILoanRequestRepository repo = new InMemoryLoanRequestRepository();
            IReadOnlyCollection<LoanRequest> submittedLoanRequests =
                await repo.AllSubmittedLoanRequests();
            Assert.True(submittedLoanRequests != null);
            Assert.True(submittedLoanRequests.Any());
            Assert.True(
                submittedLoanRequests
                .Count(x => x.RequestedLoanAmount > 0.0m) == submittedLoanRequests.Count);
            Assert.True(
                submittedLoanRequests
                .Count(x => !string.IsNullOrWhiteSpace(x.CitizenServiceNumber)) == submittedLoanRequests.Count);
        }

        [Fact]
        public async void Piping_Submitted_Loan_Requests_Will_Queue_Them()
        {
            ILoanRequestRepository repo = new InMemoryLoanRequestRepository();
            IReadOnlyCollection<LoanRequest> submittedLoanRequests =
                await repo.AllSubmittedLoanRequests();
            IPipe<IReadOnlyCollection<LoanRequest>> pipe = new InMemoryQueueBackedPipe();
            await pipe.Write(submittedLoanRequests);
        }
    }

    internal class InMemoryQueueBackedPipe
        : IPipe<IReadOnlyCollection<LoanRequest>>
    {
        private Queue<LoanRequest> queue =
            new Queue<LoanRequest>();

        public async Task Write(
            IReadOnlyCollection<LoanRequest> payload)
        {
            Debug.WriteLine($"{payload.Count} items were recieved!");

            foreach (var item in payload)
                queue.Enqueue(item);

            Debug.WriteLine($"{queue.Count} items were actually written to the pipe!");

            await Task.CompletedTask;
        }
    }

    public class InMemoryLoanRequestRepository
        : ILoanRequestRepository
    {
        public async Task<IReadOnlyCollection<LoanRequest>>
            AllSubmittedLoanRequests()
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
                    }
                });
        }
    }
}