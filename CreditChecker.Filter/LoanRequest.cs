namespace CreditChecker.Filter
{
    public class LoanRequest
    {
        public decimal RequestedLoanAmount { get; set; }
        public string CitizenServiceNumber { get; set; }
    }
}