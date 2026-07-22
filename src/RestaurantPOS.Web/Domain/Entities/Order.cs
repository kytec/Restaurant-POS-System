using RestaurantPOS.Web.Domain.Enums;

namespace RestaurantPOS.Web.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int? DiningTableId { get; set; }
    public DiningTable? DiningTable { get; set; }
    public int? StaffMemberId { get; set; }
    public StaffMember? StaffMember { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];

    public decimal Total => Items.Sum(item => item.LineTotal);
    public decimal PaidTotal => Payments.Sum(payment => payment.Amount);
    public decimal BalanceDue => Math.Max(0, Total - PaidTotal);
}
