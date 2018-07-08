using Azure.StorageQueue.Helper;
using Common.Abstractions;
using Common.Extensions;
using CreditChecker.Filter;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;

namespace CreditChecker
{
    internal class Program
    {
        private static void Main()
        {
            var dashboardConnectionString = ConfigurationManager.AppSettings["AzureWebJobsDashboard"];
            var storageConnectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];

            var svcCollection = new ServiceCollection();
            svcCollection.AddScoped<IHttpMessageHandlerFactory, MockHttpMessageHandlerFactory>();
            svcCollection.AddScoped<ICreditCheckFilter, CreditCheckFilter>();
            svcCollection.AddScoped<IPipe<EnrichedLoanRequest>, QueueBackedLoanQuoteSubmissionPipe>();
            svcCollection.AddSingleton(_ => SimpleQueueHelperFactory.Create("credit-checked-loan-requests", storageConnectionString));
            svcCollection.AddSplunkLogging(new SplunkOptions { SplunkHost = ConfigurationManager.AppSettings["SplunkHost"], Token = ConfigurationManager.AppSettings["CCToken"] });
            svcCollection.AddTransient<Functions>();
            var config = new JobHostConfiguration();
            config.DashboardConnectionString = dashboardConnectionString;
            config.StorageConnectionString = storageConnectionString;
            config.JobActivator = new MyActivator(svcCollection.BuildServiceProvider());
            var host = new JobHost(config);
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