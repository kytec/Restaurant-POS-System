using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Domain.Entities;
using RestaurantPOS.Web.Domain.Enums;

namespace RestaurantPOS.Web.Features.Staff;

public sealed class StaffService(AppDbContext context)
{
    public Task<List<StaffMember>> GetStaffAsync()
    {
        return context.StaffMembers
            .OrderBy(staff => staff.Role)
            .ThenBy(staff => staff.FullName)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<StaffMember> CreateStaffAsync(string fullName, StaffRole role)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Staff name is required.", nameof(fullName));
        }

        var staff = new StaffMember { FullName = fullName.Trim(), Role = role };
        context.StaffMembers.Add(staff);
        await context.SaveChangesAsync();
        return staff;
    }
}
