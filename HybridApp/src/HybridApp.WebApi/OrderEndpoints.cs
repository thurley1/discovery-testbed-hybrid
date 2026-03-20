using HybridApp.Domain;

namespace HybridApp.WebApi;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (Guid id, IOrderRepository repo) =>
        {
            var order = await repo.GetByIdAsync(id);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        });

        group.MapGet("/by-customer/{customerId:guid}", async (Guid customerId, IOrderRepository repo) =>
        {
            var orders = await repo.GetByCustomerIdAsync(customerId);
            return Results.Ok(orders);
        });

        group.MapPost("/", async (CreateOrderRequest request, IOrderRepository repo) =>
        {
            var order = Order.Create(request.CustomerId, request.TotalAmount);
            await repo.AddAsync(order);
            await repo.SaveAsync();
            return Results.Created($"/api/orders/{order.Id}", order);
        });

        group.MapPost("/{id:guid}/confirm", async (Guid id, IOrderRepository repo) =>
        {
            var order = await repo.GetByIdAsync(id);
            if (order is null) return Results.NotFound();

            order.Confirm();
            await repo.SaveAsync();
            return Results.Ok(order);
        });

        return group;
    }
}

public record CreateOrderRequest(Guid CustomerId, decimal TotalAmount);
