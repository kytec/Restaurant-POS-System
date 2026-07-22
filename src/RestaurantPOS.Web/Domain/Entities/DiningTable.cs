using RestaurantPOS.Web.Domain.Enums;

namespace RestaurantPOS.Web.Domain.Entities;

public class DiningTable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public TableStatus Status { get; set; } = TableStatus.Available;
}
