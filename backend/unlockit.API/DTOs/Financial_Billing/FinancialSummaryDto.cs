namespace unlockit.API.DTOs.Financial_Billing
{
    public class FinancialSummaryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
    }
}
