using Newtonsoft.Json;

namespace LoanRequestSender.Filter
{
    public class LoanQuote
    {
        [JsonProperty]
        public string Bank { get; internal set; }

        [JsonProperty]
        public decimal ApprovableAmount { get; internal set; }
    }
}