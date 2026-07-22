namespace RestaurantPOS.Web.Domain.Entities;

public class MenuItem
{
    public int Id { get; set; }
    public int MenuCategoryId { get; set; }
    public MenuCategory? Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
