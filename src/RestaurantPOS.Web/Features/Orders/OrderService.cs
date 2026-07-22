using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Domain.Entities;
using RestaurantPOS.Web.Domain.Enums;
using RestaurantPOS.Web.Services;

namespace RestaurantPOS.Web.Features.Orders;

public sealed class OrderService(AppDbContext context, PosNotificationService notifications)
{
    public Task<List<Order>> GetOpenOrdersAsync()
    {
        return context.Orders
            .Include(order => order.DiningTable)
            .Include(order => order.StaffMember)
            .Include(order => order.Items)
            .Where(order => order.Status != OrderStatus.Paid && order.Status != OrderStatus.Cancelled)
            .OrderByDescending(order => order.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<Order?> GetOrderAsync(int orderId)
    {
        return context.Orders
            .Include(order => order.DiningTable)
            .Include(order => order.StaffMember)
            .Include(order => order.Items)
            .Include(order => order.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(order => order.Id == orderId);
    }

    public async Task<Order> CreateDraftOrderAsync(int? tableId, int? staffMemberId)
    {
        var order = new Order
        {
            OrderNumber = $"POS-{DateTime.UtcNow:yyyyMMddHHmmss}",
            DiningTableId = tableId,
            StaffMemberId = staffMemberId
        };

        context.Orders.Add(order);

        if (tableId is not null)
        {
            var table = await context.DiningTables.FindAsync(tableId.Value);
            if (table is not null)
            {
                table.Status = TableStatus.Ordering;
            }
        }

        await context.SaveChangesAsync();
        notifications.NotifyChanged();
        return order;
    }

    public async Task AddItemAsync(int orderId, int menuItemId, int quantity, string notes)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var order = await context.Orders.Include(existing => existing.Items).FirstOrDefaultAsync(existing => existing.Id == orderId)
            ?? throw new InvalidOperationException("Order was not found.");

        if (order.Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft orders can be changed.");
        }

        var menuItem = await context.MenuItems.AsNoTracking().FirstOrDefaultAsync(item => item.Id == menuItemId)
            ?? throw new InvalidOperationException("Menu item was not found.");

        if (!menuItem.IsAvailable)
        {
            throw new InvalidOperationException("This menu item is unavailable.");
        }

        order.Items.Add(new OrderItem
        {
            MenuItemId = menuItem.Id,
            MenuItemName = menuItem.Name,
            UnitPrice = menuItem.Price,
            Quantity = quantity,
            Notes = notes.Trim()
        });

        await context.SaveChangesAsync();
        notifications.NotifyChanged();
    }

    public async Task SendToKitchenAsync(int orderId)
    {
        var order = await context.Orders.Include(existing => existing.Items).FirstOrDefaultAsync(existing => existing.Id == orderId)
            ?? throw new InvalidOperationException("Order was not found.");

        if (order.Items.Count == 0)
        {
            throw new InvalidOperationException("Add at least one item before sending an order to the kitchen.");
        }

        order.Status = OrderStatus.SentToKitchen;
        order.SubmittedAt = DateTime.UtcNow;

        if (order.DiningTableId is not null)
        {
            var table = await context.DiningTables.FindAsync(order.DiningTableId.Value);
            if (table is not null)
            {
                table.Status = TableStatus.WaitingForFood;
            }
        }

        await context.SaveChangesAsync();
        notifications.NotifyChanged();
    }

    public async Task UpdateKitchenItemStatusAsync(int orderItemId, KitchenItemStatus status)
    {
        var item = await context.OrderItems.Include(existing => existing.Order).FirstOrDefaultAsync(existing => existing.Id == orderItemId)
            ?? throw new InvalidOperationException("Order item was not found.");

        item.KitchenStatus = status;

        if (item.Order is not null)
        {
            var itemStatuses = await context.OrderItems
                .Where(existing => existing.OrderId == item.OrderId)
                .Select(existing => existing.Id == orderItemId ? status : existing.KitchenStatus)
                .ToListAsync();

            if (itemStatuses.All(existing => existing == KitchenItemStatus.Ready))
            {
                item.Order.Status = OrderStatus.Ready;
                item.Order.CompletedAt = DateTime.UtcNow;
                await MarkTableAsync(item.Order.DiningTableId, TableStatus.ReadyToPay);
            }
            else if (itemStatuses.Any(existing => existing == KitchenItemStatus.Preparing))
            {
                item.Order.Status = OrderStatus.Preparing;
            }
        }

        await context.SaveChangesAsync();
        notifications.NotifyChanged();
    }

    public async Task MarkServedAsync(int orderId)
    {
        var order = await context.Orders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Order was not found.");

        order.Status = OrderStatus.Served;
        await MarkTableAsync(order.DiningTableId, TableStatus.ReadyToPay);
        await context.SaveChangesAsync();
        notifications.NotifyChanged();
    }

    private async Task MarkTableAsync(int? tableId, TableStatus status)
    {
        if (tableId is null)
        {
            return;
        }

        var table = await context.DiningTables.FindAsync(tableId.Value);
        if (table is not null)
        {
            table.Status = status;
        }
    }
}
