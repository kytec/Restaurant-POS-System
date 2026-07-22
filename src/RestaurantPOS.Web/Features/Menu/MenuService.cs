using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Domain.Entities;

namespace RestaurantPOS.Web.Features.Menu;

public sealed class MenuService(AppDbContext context)
{
    public Task<List<MenuCategory>> GetMenuAsync()
    {
        return context.MenuCategories
            .Include(category => category.Items.OrderBy(item => item.Name))
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<MenuItem> CreateMenuItemAsync(int categoryId, string name, string description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Menu item name is required.", nameof(name));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Menu item price must be greater than zero.");
        }

        var item = new MenuItem
        {
            MenuCategoryId = categoryId,
            Name = name.Trim(),
            Description = description.Trim(),
            Price = price
        };

        context.MenuItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public async Task<MenuCategory> CreateCategoryAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        var displayOrder = await context.MenuCategories.CountAsync() + 1;
        var category = new MenuCategory { Name = name.Trim(), DisplayOrder = displayOrder };
        context.MenuCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    public async Task SetAvailabilityAsync(int itemId, bool isAvailable)
    {
        var item = await context.MenuItems.FindAsync(itemId)
            ?? throw new InvalidOperationException("Menu item was not found.");

        item.IsAvailable = isAvailable;
        await context.SaveChangesAsync();
    }
}
