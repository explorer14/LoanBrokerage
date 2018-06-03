using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace CustomerNotifier.Filter
{
    public class CustomerNotifierTests
    {
        [Fact]
        public async void When_CustomerLoanQuote_Does_Not_Contain_Quotes_Notifier_Throws()
        {
            ICustomerNotifierFilter notifier = new CustomerNotifierFilter(
                new MockNotificationService(),
                new MockCustomerRepository2(),
                MockLogger());
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () =>
                    await notifier.NotifyCustomer(
                        new CustomerLoanQuote()));
        }

        [Fact]
        public async void When_CustomerLoanQuote_Is_Null_Notifier_Throws()
        {
            ICustomerNotifierFilter notifier = new CustomerNotifierFilter(
                new MockNotificationService(),
                new MockCustomerRepository2(),
                MockLogger());
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () =>
                    await notifier.NotifyCustomer(
                        null));
        }

        [Fact]
        public async void When_Valid_CustomerLoanQuote_Is_Sent_Notifier_Sends_Message_To_Customer()
        {
            ICustomerNotifierFilter notifier = new CustomerNotifierFilter(
                new MockNotificationService(),
                new MockCustomerRepository2(),
                MockLogger());
            var quotesFromBanks = new LoanQuote[]
            {
                new LoanQuote
                {
                     ApprovableAmount = 90.0m,
                     Bank = "ABN Amro"
                },
                new LoanQuote
                {
                    ApprovableAmount = 40.0m,
                    Bank = "ING Vysya"
                }
            };

            var loanQuote = new CustomerLoanQuote
            {
                BSN = "12345",
                OriginalAmountRequested = 100.0m,
                Quotes = quotesFromBanks
            };

            await notifier.NotifyCustomer(loanQuote);
            MockNotificationService
                .SENT_MESSAGE
                .Should()
                .NotBeNull();
            MockNotificationService
                .SENT_MESSAGE
                .Should()
                .Be(
                JsonConvert
                .SerializeObject(
                    quotesFromBanks));
        }

        private ILogger MockLogger()
        {
            var mockLogger = new Mock<ILogger>();
            mockLogger.Setup(x => x.Information(It.IsAny<string>()));
            mockLogger.Setup(x => x.Warning(It.IsAny<string>()));
            mockLogger.Setup(x => x.Error(It.IsAny<string>()));

            return mockLogger.Object;
        }
    }

    internal class MockCustomerRepository2
        : ICustomerRepository
    {
        public async Task<CustomerInfo> CustomerByBSN(
            string bsn)
        {
            return await Task.FromResult(
                new CustomerInfo
                {
                    Email = "test@yahoo.com"
                });
        }
    }

    internal class MockNotificationService
        : INotificationService
    {
        public static string SENT_MESSAGE;

        public async Task Notify(
            string destinationAddress,
            string message)
        {
            Debug.WriteLine(
                $"Sending message to {destinationAddress} " +
                $"with a message {message}");
            SENT_MESSAGE = message;
            await Task.CompletedTask;
        }
    }
}