namespace LoanRequestSender.Filter
{
    public class LoanQuoteRequest
    {
        public string BSN { get; set; }
        public decimal LoanAmount { get; set; }
        public string CreditRating { get; set; }
    }
}