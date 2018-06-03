namespace CreditChecker.Filter
{
    public class EnrichedLoanRequest
    {
        public LoanRequest OriginalLoanRequest { get; set; }

        // Let's assume for our cases that a person who has no history of debt has a default good
        // credit rating instead of it being null. IOW, no matter what, the CreditCheckReport object
        // will never be null, it will always have a rating report.
        public CreditCheckReport CreditCheckReport { get; set; }
    }
}