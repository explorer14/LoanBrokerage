﻿using CustomerNotifier.Filter;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Configuration;

namespace CustomerNotifier
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

            svcCollection.AddScoped<ICustomerNotifierFilter,
                CustomerNotifierFilter>();
            svcCollection.AddScoped<INotificationService,
                ConsoleNotificationService>();
            svcCollection.AddScoped<ICustomerRepository,
                MockCustomerRepository>();
            svcCollection.AddSingleton<Functions>();
            svcCollection.AddSingleton<ILogger>(_ =>
            new LoggerConfiguration()
            .WriteTo
            .Console()
            .CreateLogger());

            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

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