namespace RestaurantPOS.Contracts;

public sealed record MenuCategoryDto(
    int Id,
    string Name,
    int DisplayOrder,
    IReadOnlyList<MenuItemDto> Items);
