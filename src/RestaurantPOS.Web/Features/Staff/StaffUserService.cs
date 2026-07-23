using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Domain.Entities;
using RestaurantPOS.Web.Domain.Enums;
using RestaurantPOS.Web.Security;

namespace RestaurantPOS.Web.Features.Staff;

public sealed class StaffUserService(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IStaffInvitationEmailSender invitationEmailSender)
{
    public async Task<IdentityResult> CreateStaffUserAsync(string fullName, string email, StaffRole role)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return IdentityResult.Failed(new IdentityError { Description = "Staff name is required." });
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return IdentityResult.Failed(new IdentityError { Description = "Email is required." });
        }

        var normalizedEmail = email.Trim();
        if (!new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            return IdentityResult.Failed(new IdentityError { Description = "Enter a valid email address." });
        }

        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "A user with this email already exists." });
        }

        var appRole = StaffRoleMapper.ToAppRole(role);
        var temporaryPassword = GenerateTemporaryPassword();
        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, temporaryPassword);
        if (!result.Succeeded)
        {
            return result;
        }

        result = await userManager.AddToRoleAsync(user, appRole);
        if (!result.Succeeded)
        {
            return result;
        }

        result = await userManager.AddClaimAsync(user, new Claim(AppClaimTypes.RequiresCredentialSetup, bool.TrueString));
        if (!result.Succeeded)
        {
            return result;
        }

        context.StaffMembers.Add(new StaffMember { FullName = fullName.Trim(), Role = role });
        await context.SaveChangesAsync();
        await invitationEmailSender.SendInvitationAsync(user, temporaryPassword);

        return IdentityResult.Success;
    }

    private static string GenerateTemporaryPassword()
    {
        var token = Guid.NewGuid().ToString("N")[..12];
        return $"Temp-{token}aA1!";
    }
}
