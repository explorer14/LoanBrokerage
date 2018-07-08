using Azure.StorageQueue.Helper;
using Common.Abstractions;
using Common.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;

namespace LoanRequestRetriever
{
    internal class Program
    {
        private static void Main()
        {
            ServicePointManager.DefaultConnectionLimit = 20;

            var dashboardConnectionString = ConfigurationManager.AppSettings["AzureWebJobsDashboard"];
            var storageConnectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            var svcCollection = new ServiceCollection();
            svcCollection.AddSplunkLogging(new SplunkOptions { SplunkHost = ConfigurationManager.AppSettings["SplunkHost"], Token = ConfigurationManager.AppSettings["LRRToken"] });
            svcCollection.AddScoped<ILoanRequestRepository, MockLoanRequestRepository>();
            svcCollection.AddScoped<IPipe<IReadOnlyCollection<LoanRequest>>, QueueBackedSubmittedLoanRequestsPipe>();
            svcCollection.AddSingleton(_ => SimpleQueueHelperFactory.Create("submitted-loan-requests", storageConnectionString));
            svcCollection.AddTransient<Functions>();
            var config = new JobHostConfiguration();
            config.NameResolver = new TriggerExpressionResolver();
            config.DashboardConnectionString = dashboardConnectionString;
            config.StorageConnectionString = storageConnectionString;
            config.JobActivator = new MyActivator(svcCollection.BuildServiceProvider());
            config.UseTimers();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

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

    internal class TriggerExpressionResolver
        : INameResolver
    {
        public string Resolve(string name)
        {
            if (name
                .ToLower()
                .Trim() == "triggertime")
            {
                return ConfigurationManager.AppSettings["CronExp"];
            }

            return string.Empty;
        }
    }
}