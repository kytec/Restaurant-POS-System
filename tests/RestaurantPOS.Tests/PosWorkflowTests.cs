using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Data.SeedData;
using RestaurantPOS.Web.Domain.Entities;
using RestaurantPOS.Web.Domain.Enums;
using RestaurantPOS.Web.Features.Inventory;
using RestaurantPOS.Web.Features.Orders;
using RestaurantPOS.Web.Features.Payments;
using RestaurantPOS.Web.Features.Staff;
using RestaurantPOS.Web.Security;
using RestaurantPOS.Web.Services;

namespace RestaurantPOS.Tests;

public sealed class PosWorkflowTests
{
    [Fact]
    public async Task Seeder_creates_admin_login_and_pos_roles()
    {
        using var provider = CreateIdentityProvider();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = new ConfigurationBuilder().Build();

        await DatabaseSeeder.SeedIdentityAsync(roleManager, userManager, configuration);

        await AssertSeededUserAsync(userManager, "admin@restaurant.local", AppRoles.Admin);
        foreach (var role in AppRoles.All)
        {
            Assert.True(await roleManager.RoleExistsAsync(role));
        }

        Assert.Null(await userManager.FindByEmailAsync("waiter@restaurant.local"));
    }

    [Fact]
    public async Task Admin_can_create_staff_login_with_role_and_setup_requirement()
    {
        using var provider = CreateIdentityProvider();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = provider.GetRequiredService<ApplicationDbContext>();
        var invitationEmailSender = new CapturingInvitationEmailSender();
        var configuration = new ConfigurationBuilder().Build();
        await DatabaseSeeder.SeedIdentityAsync(roleManager, userManager, configuration);
        var service = new StaffUserService(context, userManager, invitationEmailSender);

        var result = await service.CreateStaffUserAsync("Will Waiter", "will@example.com", StaffRole.Waiter);

        Assert.True(result.Succeeded);
        var user = await userManager.FindByEmailAsync("will@example.com");
        Assert.NotNull(user);
        Assert.Equal("will@example.com", invitationEmailSender.Email);
        Assert.True(await userManager.CheckPasswordAsync(user, invitationEmailSender.TemporaryPassword!));
        Assert.True(await userManager.IsInRoleAsync(user, AppRoles.Waiter));
        var claims = await userManager.GetClaimsAsync(user);
        Assert.Contains(claims, claim => claim.Type == AppClaimTypes.RequiresCredentialSetup);
        Assert.True(await context.StaffMembers.AnyAsync(staff => staff.FullName == "Will Waiter" && staff.Role == StaffRole.Waiter));
    }

    [Fact]
    public async Task Staff_login_creation_rejects_invalid_email()
    {
        using var provider = CreateIdentityProvider();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = provider.GetRequiredService<ApplicationDbContext>();
        var invitationEmailSender = new CapturingInvitationEmailSender();
        var configuration = new ConfigurationBuilder().Build();
        await DatabaseSeeder.SeedIdentityAsync(roleManager, userManager, configuration);
        var service = new StaffUserService(context, userManager, invitationEmailSender);

        var result = await service.CreateStaffUserAsync("Will Waiter", "not-an-email", StaffRole.Waiter);

        Assert.False(result.Succeeded);
        Assert.Null(invitationEmailSender.Email);
        Assert.False(await context.StaffMembers.AnyAsync(staff => staff.FullName == "Will Waiter"));
    }

    [Fact]
    public async Task Staff_user_can_complete_first_login_credential_setup()
    {
        using var provider = CreateIdentityProvider();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = provider.GetRequiredService<ApplicationDbContext>();
        var invitationEmailSender = new CapturingInvitationEmailSender();
        var configuration = new ConfigurationBuilder().Build();
        await DatabaseSeeder.SeedIdentityAsync(roleManager, userManager, configuration);
        var staffUserService = new StaffUserService(context, userManager, invitationEmailSender);
        await staffUserService.CreateStaffUserAsync("Will Waiter", "will@example.com", StaffRole.Waiter);
        var user = await userManager.FindByEmailAsync("will@example.com");
        var principal = CreatePrincipal(user!);
        var setupService = new StaffCredentialSetupService(userManager);

        var result = await setupService.CompleteSetupAsync(principal, "will.waiter", invitationEmailSender.TemporaryPassword!, "ReadyToServe123!");

        Assert.True(result.Succeeded);
        var updatedUser = await userManager.FindByEmailAsync("will@example.com");
        Assert.NotNull(updatedUser);
        Assert.Equal("will.waiter", updatedUser.UserName);
        Assert.True(await userManager.CheckPasswordAsync(updatedUser, "ReadyToServe123!"));
        var claims = await userManager.GetClaimsAsync(updatedUser);
        Assert.DoesNotContain(claims, claim => claim.Type == AppClaimTypes.RequiresCredentialSetup);
    }

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
    public async Task Ready_order_can_be_marked_served()
    {
        await using var context = CreateContext();
        context.DiningTables.Add(new DiningTable { Id = 1, Name = "Table 1", Capacity = 4, Status = TableStatus.WaitingForFood });
        var order = new Order
        {
            OrderNumber = "POS-READY",
            DiningTableId = 1,
            Status = OrderStatus.Ready
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var service = new OrderService(context, new PosNotificationService());

        await service.MarkServedAsync(order.Id);

        var savedOrder = await context.Orders.FindAsync(order.Id);
        var table = await context.DiningTables.FindAsync(1);
        Assert.Equal(OrderStatus.Served, savedOrder!.Status);
        Assert.Equal(TableStatus.ReadyToPay, table!.Status);
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

    private static ServiceProvider CreateIdentityProvider()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }

    private static async Task AssertSeededUserAsync(UserManager<ApplicationUser> userManager, string email, string role)
    {
        var user = await userManager.FindByEmailAsync(email);

        Assert.NotNull(user);
        Assert.True(await userManager.CheckPasswordAsync(user, "ChangeMe123!"));
        Assert.True(await userManager.IsInRoleAsync(user, role));
    }

    private static ClaimsPrincipal CreatePrincipal(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id)],
            IdentityConstants.ApplicationScheme);

        return new ClaimsPrincipal(identity);
    }

    private sealed class CapturingInvitationEmailSender : IStaffInvitationEmailSender
    {
        public string? Email { get; private set; }
        public string? TemporaryPassword { get; private set; }

        public Task SendInvitationAsync(ApplicationUser user, string temporaryPassword)
        {
            Email = user.Email;
            TemporaryPassword = temporaryPassword;
            return Task.CompletedTask;
        }
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
