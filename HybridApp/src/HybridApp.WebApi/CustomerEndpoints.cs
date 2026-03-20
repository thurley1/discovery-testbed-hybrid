using HybridApp.Domain;

namespace HybridApp.WebApi;

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (Guid id, ICustomerRepository repo) =>
        {
            var customer = await repo.GetByIdAsync(id);
            return customer is not null ? Results.Ok(customer) : Results.NotFound();
        });

        group.MapGet("/by-email/{email}", async (string email, ICustomerRepository repo) =>
        {
            var customer = await repo.GetByEmailAsync(email);
            return customer is not null ? Results.Ok(customer) : Results.NotFound();
        });

        group.MapPost("/", async (CreateCustomerRequest request, ICustomerRepository repo) =>
        {
            var customer = Customer.Create(request.Name, request.Email);
            await repo.AddAsync(customer);
            await repo.SaveAsync();
            return Results.Created($"/api/customers/{customer.Id}", customer);
        });

        return group;
    }
}

public record CreateCustomerRequest(string Name, string Email);
