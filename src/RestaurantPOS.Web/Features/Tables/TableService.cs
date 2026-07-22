using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Domain.Entities;
using RestaurantPOS.Web.Domain.Enums;
using RestaurantPOS.Web.Services;

namespace RestaurantPOS.Web.Features.Tables;

public sealed class TableService(AppDbContext context, PosNotificationService notifications)
{
    public Task<List<DiningTable>> GetTablesAsync()
    {
        return context.DiningTables
            .OrderBy(table => table.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task SetStatusAsync(int tableId, TableStatus status)
    {
        var table = await context.DiningTables.FindAsync(tableId)
            ?? throw new InvalidOperationException("Table was not found.");

        table.Status = status;
        await context.SaveChangesAsync();
        notifications.NotifyChanged();
    }
}
