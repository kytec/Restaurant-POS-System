using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantPOS.Web.Domain.Entities;

namespace RestaurantPOS.Web.Data.Configurations;

public sealed class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        builder.Property(category => category.Name).HasMaxLength(80).IsRequired();
        builder.HasMany(category => category.Items)
            .WithOne(item => item.Category)
            .HasForeignKey(item => item.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.Property(item => item.Name).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(300);
        builder.Property(item => item.Price).HasPrecision(10, 2);
    }
}

public sealed class DiningTableConfiguration : IEntityTypeConfiguration<DiningTable>
{
    public void Configure(EntityTypeBuilder<DiningTable> builder)
    {
        builder.Property(table => table.Name).HasMaxLength(40).IsRequired();
    }
}

public sealed class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.Property(staff => staff.FullName).HasMaxLength(120).IsRequired();
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(order => order.OrderNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(order => order.OrderNumber).IsUnique();
        builder.HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(order => order.Payments)
            .WithOne(payment => payment.Order)
            .HasForeignKey(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(item => item.MenuItemName).HasMaxLength(120).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(10, 2);
        builder.Property(item => item.Notes).HasMaxLength(240);
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(payment => payment.Amount).HasPrecision(10, 2);
        builder.Property(payment => payment.Reference).HasMaxLength(120);
    }
}

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.Property(item => item.Name).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Unit).HasMaxLength(30).IsRequired();
        builder.Property(item => item.QuantityOnHand).HasPrecision(10, 2);
        builder.Property(item => item.ReorderLevel).HasPrecision(10, 2);
    }
}

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.Property(movement => movement.Quantity).HasPrecision(10, 2);
        builder.Property(movement => movement.Reason).HasMaxLength(200);
    }
}
