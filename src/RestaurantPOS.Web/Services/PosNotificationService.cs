namespace RestaurantPOS.Web.Services;

public sealed class PosNotificationService
{
    public event Action? Changed;

    public void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
