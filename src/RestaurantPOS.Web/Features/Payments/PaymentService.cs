using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Web.Data;
using RestaurantPOS.Web.Domain.Entities;
using RestaurantPOS.Web.Domain.Enums;
using RestaurantPOS.Web.Services;

namespace RestaurantPOS.Web.Features.Payments;

public sealed class PaymentService(AppDbContext context, PosNotificationService notifications)
{
    public Task<List<Order>> GetPayableOrdersAsync()
    {
        return context.Orders
            .Include(order => order.DiningTable)
            .Include(order => order.Items)
            .Include(order => order.Payments)
            .Where(order => order.Status == OrderStatus.Ready || order.Status == OrderStatus.Served)
            .OrderBy(order => order.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Payment> RecordPaymentAsync(int orderId, PaymentMethod method, decimal amount, string reference)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        }

        var order = await context.Orders
            .Include(existing => existing.Items)
            .Include(existing => existing.Payments)
            .FirstOrDefaultAsync(existing => existing.Id == orderId)
            ?? throw new InvalidOperationException("Order was not found.");

        if (order.Status is OrderStatus.Paid or OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("This order cannot accept payments.");
        }

        if (amount > order.BalanceDue)
        {
            throw new InvalidOperationException("Payment cannot exceed the balance due.");
        }

        var balanceBeforePayment = order.BalanceDue;
        var payment = new Payment
        {
            OrderId = orderId,
            Method = method,
            Amount = amount,
            Reference = reference.Trim()
        };

        order.Payments.Add(payment);

        if (amount >= balanceBeforePayment)
        {
            order.Status = OrderStatus.Paid;
            order.PaidAt = DateTime.UtcNow;

            if (order.DiningTableId is not null)
            {
                var table = await context.DiningTables.FindAsync(order.DiningTableId.Value);
                if (table is not null)
                {
                    table.Status = TableStatus.Available;
                }
            }
        }

        await context.SaveChangesAsync();
        notifications.NotifyChanged();
        return payment;
    }
}
