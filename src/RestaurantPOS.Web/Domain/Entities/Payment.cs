using RestaurantPOS.Web.Domain.Enums;

namespace RestaurantPOS.Web.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}
