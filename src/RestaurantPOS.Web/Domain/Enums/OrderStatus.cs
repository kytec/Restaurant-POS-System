namespace RestaurantPOS.Web.Domain.Enums;

public enum OrderStatus
{
    Draft = 1,
    SentToKitchen = 2,
    Preparing = 3,
    Ready = 4,
    Served = 5,
    Paid = 6,
    Cancelled = 7
}
