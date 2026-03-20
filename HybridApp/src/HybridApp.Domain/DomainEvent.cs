namespace HybridApp.Domain;

public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}

public sealed class OrderConfirmedEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
}

public sealed class CustomerRegisteredEvent : DomainEvent
{
    public Guid CustomerId { get; init; }
    public string Email { get; init; } = string.Empty;
}
