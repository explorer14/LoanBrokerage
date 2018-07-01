using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Splunk;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ServiceCollection AddSplunkLogging(
            this ServiceCollection services,
            SplunkOptions splunkOptions)
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback +=
                     (HttpRequestMessage message,
                     X509Certificate2 cert,
                     X509Chain chain,
                     SslPolicyErrors policyErrors) => true;

            services.AddSingleton<ILogger>(_ =>
                new LoggerConfiguration()
                .WriteTo
                .Console()
                .WriteTo
                .EventCollector(
                    splunkOptions.SplunkHost,
                    splunkOptions.Token,
                    new SplunkJsonFormatter(true, null),
                    "services/collector",
                    LogEventLevel.Verbose,
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}",
                    2, 100, handler)
                .CreateLogger());

            return services;
        }
    }

    public class SplunkOptions
    {
        public string SplunkHost { get; set; }
        public string Token { get; set; }
    }
}