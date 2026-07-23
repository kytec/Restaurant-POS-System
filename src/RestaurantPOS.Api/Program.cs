using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Contracts;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Data.SeedData;
using RestaurantPOS.Web.Features.Menu;
using RestaurantPOS.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddCors(options =>
{
    options.AddPolicy("RestaurantPOSClient", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("AllowedClientOrigins").Get<string[]>() ?? ["https://localhost:7243", "http://localhost:5087"])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<AppDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddSingleton<PosNotificationService>();
builder.Services.AddScoped<MenuService>();

var app = builder.Build();
var logger = app.Logger;

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("RestaurantPOSClient");

app.Use(async (context, next) =>
{
    var requestId = context.TraceIdentifier;
    context.Response.Headers["x-request-id"] = requestId;

    using var scope = logger.BeginScope(new Dictionary<string, object?>
    {
        ["RequestId"] = requestId,
        ["Method"] = context.Request.Method,
        ["Path"] = context.Request.Path.Value
    });

    var startedAt = TimeProvider.System.GetTimestamp();
    await next();
    var elapsed = TimeProvider.System.GetElapsedTime(startedAt);

    logger.LogInformation(
        "api_request_completed {Method} {Path} {StatusCode} {ElapsedMilliseconds}",
        context.Request.Method,
        context.Request.Path.Value,
        context.Response.StatusCode,
        elapsed.TotalMilliseconds);
});

try
{
    await DatabaseSeeder.SeedAsync(app.Services, app.Configuration);
}
catch (Exception ex) when (app.Environment.IsDevelopment())
{
    logger.LogWarning(ex, "Database migration and seed data were skipped. Verify SQL Server LocalDB is installed and run migrations before using API-backed POS screens.");
}

app.MapGet(ApiRoutes.Health, () => Results.Ok(new ApiHealthResponse("ok", DateTimeOffset.UtcNow)))
    .WithName("GetApiHealth");

app.MapGet(ApiRoutes.Menu, async (MenuService menuService, ILoggerFactory loggerFactory) =>
    {
        var endpointLogger = loggerFactory.CreateLogger("RestaurantPOS.Api.Menu");
        var categories = await menuService.GetMenuAsync();
        var response = categories
            .Select(category => new MenuCategoryDto(
                category.Id,
                category.Name,
                category.DisplayOrder,
                category.Items.Select(item => new MenuItemDto(
                    item.Id,
                    item.Name,
                    item.Description,
                    item.Price,
                    item.IsAvailable)).ToList()))
            .ToList();

        endpointLogger.LogInformation("menu_listed {CategoryCount} {ItemCount}", response.Count, response.Sum(category => category.Items.Count));
        return Results.Ok(response);
    })
    .WithName("GetMenu");

app.Run();
