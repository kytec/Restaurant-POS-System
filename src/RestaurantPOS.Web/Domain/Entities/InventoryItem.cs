namespace RestaurantPOS.Web.Domain.Entities;

public class InventoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "unit";
    public decimal QuantityOnHand { get; set; }
    public decimal ReorderLevel { get; set; }
    public List<StockMovement> Movements { get; set; } = [];

    public bool IsLowStock => QuantityOnHand <= ReorderLevel;
}
