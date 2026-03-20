namespace HybridApp.Domain;

/// <summary>
/// In-process event bus — a strong monolith signal.
/// True microservices would use a message broker (RabbitMQ, Kafka, etc.).
/// This is an in-memory pub/sub that only works within a single process.
/// </summary>
public class SharedEventBus
{
    private readonly Dictionary<Type, List<Func<object, Task>>> _handlers = new();

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent
    {
        var eventType = typeof(TEvent);
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Func<object, Task>>();

        _handlers[eventType].Add(e => handler((TEvent)e));
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : DomainEvent
    {
        var eventType = typeof(TEvent);
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers)
            {
                await handler(domainEvent);
            }
        }
    }

    public void Clear() => _handlers.Clear();
}
