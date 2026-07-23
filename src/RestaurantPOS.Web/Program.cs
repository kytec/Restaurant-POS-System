using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using RestaurantPOS.Web.Components;
using RestaurantPOS.Web.Components.Account;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Data.SeedData;
using RestaurantPOS.Web.Features.Inventory;
using RestaurantPOS.Web.Features.Kitchen;
using RestaurantPOS.Web.Features.Menu;
using RestaurantPOS.Web.Features.Orders;
using RestaurantPOS.Web.Features.Payments;
using RestaurantPOS.Web.Features.Reports;
using RestaurantPOS.Web.Features.Staff;
using RestaurantPOS.Web.Features.Tables;
using RestaurantPOS.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<AppDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, Microsoft.AspNetCore.Identity.UI.Services.NoOpEmailSender>();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddSingleton<PosNotificationService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<TableService>();
builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<IStaffInvitationEmailSender, StaffInvitationEmailSender>();
builder.Services.AddScoped<StaffUserService>();
builder.Services.AddScoped<StaffCredentialSetupService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<KitchenService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

try
{
    await DatabaseSeeder.SeedAsync(app.Services, app.Configuration);
}
catch (Exception ex) when (app.Environment.IsDevelopment())
{
    app.Logger.LogWarning(ex, "Database migration and seed data were skipped. Verify SQL Server LocalDB is installed and run migrations before using authenticated POS screens.");
}

app.Run();
