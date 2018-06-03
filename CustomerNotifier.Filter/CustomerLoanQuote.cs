using Newtonsoft.Json;
using System.Collections.Generic;

namespace CustomerNotifier.Filter
{
    public class CustomerLoanQuote
    {
        [JsonProperty]
        public string BSN { get; internal set; }

        [JsonProperty]
        public decimal OriginalAmountRequested { get; internal set; }

        [JsonProperty]
        public IReadOnlyCollection<LoanQuote> Quotes { get; internal set; }
    }
}