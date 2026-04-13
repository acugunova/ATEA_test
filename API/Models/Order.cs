namespace ATEA_test.API.Models
{
    public class Order
    {
        public string OrderNumber { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string GatewayId{ get; set; } = null!;
        public string? Description { get; set; }
    }
}
