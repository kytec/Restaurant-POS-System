using RestaurantPOS.Web.Domain.Enums;

namespace RestaurantPOS.Web.Domain.Entities;

public class StaffMember
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public StaffRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}
