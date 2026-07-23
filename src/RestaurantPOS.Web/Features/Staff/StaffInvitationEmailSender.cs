using Microsoft.AspNetCore.Identity.UI.Services;
using RestaurantPOS.Web.Data;

namespace RestaurantPOS.Web.Features.Staff;

public interface IStaffInvitationEmailSender
{
    Task SendInvitationAsync(ApplicationUser user, string temporaryPassword);
}

public sealed class StaffInvitationEmailSender(IEmailSender emailSender) : IStaffInvitationEmailSender
{
    public Task SendInvitationAsync(ApplicationUser user, string temporaryPassword)
    {
        var body = $"""
            <p>Your Restaurant POS staff account has been created.</p>
            <p>Sign in with these temporary credentials:</p>
            <ul>
                <li>Email: {user.Email}</li>
                <li>Temporary password: {temporaryPassword}</li>
            </ul>
            <p>You will be asked to choose your own username and password after your first sign in.</p>
            """;

        return emailSender.SendEmailAsync(user.Email!, "Your Restaurant POS staff account", body);
    }
}
