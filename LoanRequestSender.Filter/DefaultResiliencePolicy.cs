using Common.Abstractions;
using Polly;
using Polly.Wrap;
using Serilog;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LoanRequestSender.Filter
{
    public class DefaultResiliencePolicy : IResiliencePolicy
    {
        private readonly ILogger logger;
        private readonly ResilienceSettings resilienceSettings;

        public DefaultResiliencePolicy(
            ILogger logger,
            ResilienceSettings resilienceSettings =
                default(ResilienceSettings))
        {
            this.logger = logger;
            this.resilienceSettings = resilienceSettings ??
                ResilienceSettings.Default;
        }

        public async Task<TOut> Execute<TOut>(
            Func<Task<TOut>> actionToExecute)
        {
            var retryPolicy = Policy<TOut>
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                resilienceSettings.RetryCount,
                (i) => TimeSpan.FromMilliseconds(i * 1000),
                (result, retryDelay) =>
                {
                    logger.Error(
                                "The service call to failed with Error {@ex}",
                                $"Retrying in...{retryDelay} s",
                                result.Exception);
                });

            var circuitBreakerPolicy = Policy<TOut>
                .Handle<HttpRequestException>()
                .CircuitBreakerAsync(
                resilienceSettings
                    .NumberOfErrorsBeforeBreakingCircuit,
                TimeSpan.FromSeconds(
                    resilienceSettings
                    .NumberOfSecondsToKeepCircuitBroken),
                (result, timespan) =>
                {
                    logger.Warning("Circuit Breaker opened! Result: {@result}", result);
                },
                () =>
                {
                    logger.Warning("Circuit Breaker reset!");
                });

            PolicyWrap<TOut> policyWrapper =
                Policy.WrapAsync(
                    new IAsyncPolicy<TOut>[]
                    {
                        retryPolicy,
                        circuitBreakerPolicy
                    });
            var response = await policyWrapper.ExecuteAsync(actionToExecute);

            return response;
        }
    }
}