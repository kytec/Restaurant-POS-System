using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Security;

namespace RestaurantPOS.Web.Features.Staff;

public sealed class StaffCredentialSetupService(UserManager<ApplicationUser> userManager)
{
    public async Task<IdentityResult> CompleteSetupAsync(
        ClaimsPrincipal principal,
        string username,
        string temporaryPassword,
        string newPassword)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "Unable to load the current user." });
        }

        if (!await RequiresSetupAsync(user))
        {
            return IdentityResult.Failed(new IdentityError { Description = "This account has already completed setup." });
        }

        var trimmedUsername = username.Trim();
        if (string.IsNullOrWhiteSpace(trimmedUsername))
        {
            return IdentityResult.Failed(new IdentityError { Description = "Username is required." });
        }

        var existingUser = await userManager.FindByNameAsync(trimmedUsername);
        if (existingUser is not null && existingUser.Id != user.Id)
        {
            return IdentityResult.Failed(new IdentityError { Description = "That username is already in use." });
        }

        var result = await userManager.ChangePasswordAsync(user, temporaryPassword, newPassword);
        if (!result.Succeeded)
        {
            return result;
        }

        result = await userManager.SetUserNameAsync(user, trimmedUsername);
        if (!result.Succeeded)
        {
            return result;
        }

        var setupClaims = (await userManager.GetClaimsAsync(user))
            .Where(claim => claim.Type == AppClaimTypes.RequiresCredentialSetup)
            .ToList();

        foreach (var claim in setupClaims)
        {
            result = await userManager.RemoveClaimAsync(user, claim);
            if (!result.Succeeded)
            {
                return result;
            }
        }

        return IdentityResult.Success;
    }

    private async Task<bool> RequiresSetupAsync(ApplicationUser user)
    {
        var claims = await userManager.GetClaimsAsync(user);
        return claims.Any(claim => claim.Type == AppClaimTypes.RequiresCredentialSetup);
    }
}
