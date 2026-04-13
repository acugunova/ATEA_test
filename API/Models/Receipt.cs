namespace ATEA_test.API.Models
{
    public class Receipt
    {
        public string OrderNumber { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string? PaymentConfirmation { get; set; }
    }
}
