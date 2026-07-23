using RestaurantPOS.Web.Domain.Enums;
using RestaurantPOS.Web.Security;

namespace RestaurantPOS.Web.Features.Staff;

public static class StaffRoleMapper
{
    public static string ToAppRole(StaffRole role) => role switch
    {
        StaffRole.Admin => AppRoles.Admin,
        StaffRole.Cashier => AppRoles.Cashier,
        StaffRole.Waiter => AppRoles.Waiter,
        StaffRole.Kitchen => AppRoles.Kitchen,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported staff role.")
    };
}
