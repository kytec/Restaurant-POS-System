namespace RestaurantPOS.Contracts;

public sealed record MenuItemDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    bool IsAvailable);
