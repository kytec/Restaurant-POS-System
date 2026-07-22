using RestaurantPOS.Web.Domain.Enums;

namespace RestaurantPOS.Web.Domain.Entities;

public class StockMovement
{
    public int Id { get; set; }
    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public StockMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public decimal SignedQuantity =>
        Type is StockMovementType.AdjustmentIn ? Quantity : -Quantity;
}
