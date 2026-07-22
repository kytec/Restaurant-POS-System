namespace RestaurantPOS.Web.Domain.Entities;

public class MenuCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<MenuItem> Items { get; set; } = [];
}
