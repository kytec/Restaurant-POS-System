using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Domain.Entities;
using RestaurantPOS.Web.Domain.Enums;

namespace RestaurantPOS.Web.Features.Inventory;

public sealed class InventoryService(AppDbContext context)
{
    public Task<List<InventoryItem>> GetInventoryAsync()
    {
        return context.InventoryItems
            .OrderBy(item => item.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<InventoryItem> CreateItemAsync(string name, string unit, decimal quantityOnHand, decimal reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Inventory item name is required.", nameof(name));
        }

        var item = new InventoryItem
        {
            Name = name.Trim(),
            Unit = string.IsNullOrWhiteSpace(unit) ? "unit" : unit.Trim(),
            QuantityOnHand = quantityOnHand,
            ReorderLevel = reorderLevel
        };

        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public async Task AdjustStockAsync(int itemId, StockMovementType type, decimal quantity, string reason)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var item = await context.InventoryItems.FindAsync(itemId)
            ?? throw new InvalidOperationException("Inventory item was not found.");

        var movement = new StockMovement
        {
            InventoryItemId = itemId,
            Type = type,
            Quantity = quantity,
            Reason = reason.Trim()
        };

        item.QuantityOnHand += movement.SignedQuantity;
        context.StockMovements.Add(movement);
        await context.SaveChangesAsync();
    }
}
