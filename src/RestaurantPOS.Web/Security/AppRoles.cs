namespace RestaurantPOS.Web.Security;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Cashier = "Cashier";
    public const string Waiter = "Waiter";
    public const string Kitchen = "Kitchen";

    public const string AdminOnly = Admin;
    public const string OrderAccess = $"{Admin},{Cashier},{Waiter}";
    public const string KitchenAccess = $"{Admin},{Kitchen}";
    public const string PaymentAccess = $"{Admin},{Cashier}";
    public const string InventoryAccess = $"{Admin}";
    public const string ReportsAccess = $"{Admin}";

    public static readonly string[] All = [Admin, Cashier, Waiter, Kitchen];
}
