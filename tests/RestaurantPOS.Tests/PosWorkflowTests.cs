using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Domain.Entities;
using RestaurantPOS.Web.Domain.Enums;
using RestaurantPOS.Web.Features.Inventory;
using RestaurantPOS.Web.Features.Orders;
using RestaurantPOS.Web.Features.Payments;
using RestaurantPOS.Web.Services;

namespace RestaurantPOS.Tests;

public sealed class PosWorkflowTests
{
    [Fact]
    public void Order_total_sums_order_item_line_totals()
    {
        var order = new Order
        {
            Items =
            [
                new OrderItem { MenuItemName = "Burger", UnitPrice = 12.50m, Quantity = 2 },
                new OrderItem { MenuItemName = "Tea", UnitPrice = 3.00m, Quantity = 1 }
            ]
        };

        Assert.Equal(28.00m, order.Total);
    }

    [Fact]
    public void Inventory_item_reports_low_stock_at_reorder_level()
    {
        var item = new InventoryItem { Name = "Lemons", QuantityOnHand = 10, ReorderLevel = 10 };

        Assert.True(item.IsLowStock);
    }

    [Fact]
    public async Task Order_can_be_created_and_sent_to_kitchen()
    {
        await using var context = CreateContext();
        await SeedOrderDataAsync(context);
        var service = new OrderService(context, new PosNotificationService());

        var order = await service.CreateDraftOrderAsync(tableId: 1, staffMemberId: 1);
        await service.AddItemAsync(order.Id, menuItemId: 1, quantity: 2, notes: "No onion");
        await service.SendToKitchenAsync(order.Id);

        var savedOrder = await context.Orders.Include(existing => existing.Items).FirstAsync(existing => existing.Id == order.Id);
        var table = await context.DiningTables.FindAsync(1);

        Assert.Equal(OrderStatus.SentToKitchen, savedOrder.Status);
        Assert.Single(savedOrder.Items);
        Assert.Equal(TableStatus.WaitingForFood, table!.Status);
    }

    [Fact]
    public async Task Kitchen_item_status_marks_order_ready_when_all_items_are_ready()
    {
        await using var context = CreateContext();
        await SeedOrderDataAsync(context);
        var service = new OrderService(context, new PosNotificationService());
        var order = await service.CreateDraftOrderAsync(tableId: 1, staffMemberId: 1);
        await service.AddItemAsync(order.Id, menuItemId: 1, quantity: 1, notes: string.Empty);
        await service.SendToKitchenAsync(order.Id);
        var itemId = await context.OrderItems.Where(item => item.OrderId == order.Id).Select(item => item.Id).FirstAsync();

        await service.UpdateKitchenItemStatusAsync(itemId, KitchenItemStatus.Ready);

        var savedOrder = await context.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Ready, savedOrder!.Status);
    }

    [Fact]
    public async Task Partial_payment_does_not_close_order()
    {
        await using var context = CreateContext();
        var order = new Order
        {
            OrderNumber = "POS-1",
            Status = OrderStatus.Ready,
            Items = [new OrderItem { MenuItemName = "Burger", UnitPrice = 10, Quantity = 1 }]
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var service = new PaymentService(context, new PosNotificationService());

        await service.RecordPaymentAsync(order.Id, PaymentMethod.Cash, 5, string.Empty);

        var savedOrder = await context.Orders.Include(existing => existing.Payments).FirstAsync(existing => existing.Id == order.Id);
        Assert.Equal(OrderStatus.Ready, savedOrder.Status);
        Assert.Equal(5, savedOrder.BalanceDue);
    }

    [Fact]
    public async Task Full_payment_marks_order_paid()
    {
        await using var context = CreateContext();
        var order = new Order
        {
            OrderNumber = "POS-2",
            Status = OrderStatus.Ready,
            Items = [new OrderItem { MenuItemName = "Burger", UnitPrice = 10, Quantity = 1 }]
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var service = new PaymentService(context, new PosNotificationService());

        await service.RecordPaymentAsync(order.Id, PaymentMethod.Card, 10, string.Empty);

        var savedOrder = await context.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Paid, savedOrder!.Status);
        Assert.NotNull(savedOrder.PaidAt);
    }

    [Fact]
    public async Task Inventory_adjustment_updates_quantity_and_records_movement()
    {
        await using var context = CreateContext();
        context.InventoryItems.Add(new InventoryItem { Name = "Buns", Unit = "pcs", QuantityOnHand = 10, ReorderLevel = 3 });
        await context.SaveChangesAsync();
        var service = new InventoryService(context);

        await service.AdjustStockAsync(1, StockMovementType.AdjustmentOut, 4, "Spoiled");

        var item = await context.InventoryItems.FindAsync(1);
        var movementCount = await context.StockMovements.CountAsync();
        Assert.Equal(6, item!.QuantityOnHand);
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task Ef_core_can_create_read_and_update_core_entities()
    {
        await using var context = CreateContext();
        context.MenuCategories.Add(new MenuCategory
        {
            Name = "Mains",
            Items = [new MenuItem { Name = "Pasta", Description = "Tomato sauce", Price = 14 }]
        });
        await context.SaveChangesAsync();

        var item = await context.MenuItems.FirstAsync();
        item.IsAvailable = false;
        await context.SaveChangesAsync();

        var unavailableItem = await context.MenuItems.AsNoTracking().FirstAsync();
        Assert.False(unavailableItem.IsAvailable);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task SeedOrderDataAsync(AppDbContext context)
    {
        context.DiningTables.Add(new DiningTable { Id = 1, Name = "Table 1", Capacity = 4 });
        context.StaffMembers.Add(new StaffMember { Id = 1, FullName = "Will Waiter", Role = StaffRole.Waiter });
        context.MenuCategories.Add(new MenuCategory
        {
            Id = 1,
            Name = "Mains",
            Items =
            [
                new MenuItem { Id = 1, Name = "Burger", Description = "House burger", Price = 10, IsAvailable = true }
            ]
        });
        await context.SaveChangesAsync();
    }
}
