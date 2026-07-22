using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Domain.Entities;
using RestaurantPOS.Web.Domain.Enums;

namespace RestaurantPOS.Web.Features.Kitchen;

public sealed class KitchenService(AppDbContext context)
{
    public Task<List<OrderItem>> GetKitchenItemsAsync()
    {
        return context.OrderItems
            .Include(item => item.Order)
            .ThenInclude(order => order!.DiningTable)
            .Where(item => item.Order != null &&
                item.Order.Status != OrderStatus.Draft &&
                item.Order.Status != OrderStatus.Paid &&
                item.Order.Status != OrderStatus.Cancelled)
            .OrderBy(item => item.KitchenStatus)
            .ThenBy(item => item.Order!.SubmittedAt)
            .AsNoTracking()
            .ToListAsync();
    }
}
