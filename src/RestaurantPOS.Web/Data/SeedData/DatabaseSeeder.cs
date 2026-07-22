using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Web.Domain.Entities;
using RestaurantPOS.Web.Domain.Enums;
using RestaurantPOS.Web.Security;

namespace RestaurantPOS.Web.Data.SeedData;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var context = provider.GetRequiredService<AppDbContext>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();
        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager, configuration);
        await SeedPosDataAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var email = configuration["SeedAdmin:Email"] ?? "admin@restaurant.local";
        var password = configuration["SeedAdmin:Password"] ?? "ChangeMe123!";
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Could not seed admin user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            await userManager.AddToRoleAsync(user, AppRoles.Admin);
        }
    }

    private static async Task SeedPosDataAsync(AppDbContext context)
    {
        if (!await context.DiningTables.AnyAsync())
        {
            context.DiningTables.AddRange(
                new DiningTable { Name = "Table 1", Capacity = 2 },
                new DiningTable { Name = "Table 2", Capacity = 4 },
                new DiningTable { Name = "Table 3", Capacity = 4 },
                new DiningTable { Name = "Patio 1", Capacity = 6 });
        }

        if (!await context.StaffMembers.AnyAsync())
        {
            context.StaffMembers.AddRange(
                new StaffMember { FullName = "Avery Admin", Role = StaffRole.Admin },
                new StaffMember { FullName = "Casey Cashier", Role = StaffRole.Cashier },
                new StaffMember { FullName = "Will Waiter", Role = StaffRole.Waiter },
                new StaffMember { FullName = "Kai Kitchen", Role = StaffRole.Kitchen });
        }

        if (!await context.MenuCategories.AnyAsync())
        {
            var mains = new MenuCategory { Name = "Mains", DisplayOrder = 1 };
            var drinks = new MenuCategory { Name = "Drinks", DisplayOrder = 2 };

            mains.Items.AddRange([
                new MenuItem { Name = "House Burger", Description = "Beef patty, greens, and house sauce", Price = 12.50m },
                new MenuItem { Name = "Grilled Chicken Bowl", Description = "Rice, vegetables, and lemon dressing", Price = 13.75m }
            ]);
            drinks.Items.AddRange([
                new MenuItem { Name = "Fresh Lemonade", Description = "Pressed lemon and mint", Price = 3.50m },
                new MenuItem { Name = "Iced Tea", Description = "Black tea served over ice", Price = 3.00m }
            ]);

            context.MenuCategories.AddRange(mains, drinks);
        }

        if (!await context.InventoryItems.AnyAsync())
        {
            context.InventoryItems.AddRange(
                new InventoryItem { Name = "Burger Buns", Unit = "pcs", QuantityOnHand = 48, ReorderLevel = 20 },
                new InventoryItem { Name = "Chicken Breast", Unit = "kg", QuantityOnHand = 12, ReorderLevel = 5 },
                new InventoryItem { Name = "Lemons", Unit = "pcs", QuantityOnHand = 30, ReorderLevel = 15 });
        }

        await context.SaveChangesAsync();
    }
}
