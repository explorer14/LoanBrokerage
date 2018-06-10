using Azure.StorageQueue.Helper;
using Common.Abstractions;
using LoanRequestSender.Filter;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Configuration;

namespace LoanRequestSender
{
    internal class Program
    {
        private static void Main()
        {
            var dashboardConnectionString = ConfigurationManager
                .AppSettings["AzureWebJobsDashboard"];
            var storageConnectionString = ConfigurationManager
                .AppSettings["AzureWebJobsStorage"];

            var svcCollection = new ServiceCollection();
            svcCollection.AddSingleton<ILogger>((_) =>
                new LoggerConfiguration().WriteTo.Console().CreateLogger());

            svcCollection.AddScoped<IHttpMessageHandlerFactory,
                MockHttpMessageHandlerFactory>();

            svcCollection.AddScoped<ILoanRequestSenderFilter,
                LoanRequestSenderFilter>();

            svcCollection.AddScoped<IPipe<LoanQuoteResponse>,
                AggregatedLoanQuotesPipe>();

            svcCollection.AddScoped<IResiliencePolicy,
                DefaultResiliencePolicy>();

            svcCollection.AddSingleton(_ =>
            SimpleQueueHelperFactory.Create(
                "aggregated-loan-quotes",
                storageConnectionString));

            svcCollection.AddTransient<Functions>();

            var config = new JobHostConfiguration();
            config.DashboardConnectionString = dashboardConnectionString;
            config.StorageConnectionString = storageConnectionString;
            config.JobActivator = new MyActivator(
                svcCollection.BuildServiceProvider());

            var host = new JobHost(config);
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
    }

    internal class MyActivator : IJobActivator
    {
        private readonly IServiceProvider serviceProvider;

        public MyActivator(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public T CreateInstance<T>()
        {
            return this.serviceProvider.GetService<T>();
        }
    }
}