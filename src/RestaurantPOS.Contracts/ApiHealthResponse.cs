namespace RestaurantPOS.Contracts;

public sealed record ApiHealthResponse(string Status, DateTimeOffset CheckedAt);
