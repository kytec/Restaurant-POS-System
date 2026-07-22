using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Domain.Enums;

namespace RestaurantPOS.Web.Features.Reports;

public sealed class ReportService(AppDbContext context)
{
    public async Task<DailyReport> GetTodayAsync()
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);

        var orders = await context.Orders
            .Include(order => order.Items)
            .Include(order => order.Payments)
            .Where(order => order.CreatedAt >= start && order.CreatedAt < end)
            .AsNoTracking()
            .ToListAsync();

        var lowStock = await context.InventoryItems
            .Where(item => item.QuantityOnHand <= item.ReorderLevel)
            .AsNoTracking()
            .ToListAsync();

        return new DailyReport(
            orders.Count,
            orders.Count(order => order.Status == OrderStatus.Paid),
            orders.SelectMany(order => order.Payments).Sum(payment => payment.Amount),
            orders.SelectMany(order => order.Items)
                .GroupBy(item => item.MenuItemName)
                .Select(group => new TopMenuItem(group.Key, group.Sum(item => item.Quantity), group.Sum(item => item.LineTotal)))
                .OrderByDescending(item => item.QuantitySold)
                .Take(5)
                .ToList(),
            lowStock.Select(item => new LowStockItem(item.Name, item.QuantityOnHand, item.Unit, item.ReorderLevel)).ToList());
    }
}

public sealed record DailyReport(
    int OrderCount,
    int PaidOrderCount,
    decimal SalesTotal,
    IReadOnlyList<TopMenuItem> TopItems,
    IReadOnlyList<LowStockItem> LowStockItems);

public sealed record TopMenuItem(string Name, int QuantitySold, decimal SalesTotal);

public sealed record LowStockItem(string Name, decimal QuantityOnHand, string Unit, decimal ReorderLevel);
