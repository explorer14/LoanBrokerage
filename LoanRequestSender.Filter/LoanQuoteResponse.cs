using Newtonsoft.Json;
using System.Collections.Generic;

namespace LoanRequestSender.Filter
{
    public class LoanQuoteResponse
    {
        [JsonProperty]
        public string BSN { get; internal set; }

        [JsonProperty]
        public decimal OriginalAmountRequested { get; internal set; }

        [JsonProperty]
        public List<LoanQuote> Quotes { get; internal set; } = new List<LoanQuote>();
    }
}