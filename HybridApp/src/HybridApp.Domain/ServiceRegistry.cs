namespace HybridApp.Domain;

/// <summary>
/// Static registry of available services — a strong monolith signal.
/// True microservices would use service discovery (Consul, Eureka, Kubernetes DNS).
/// A static in-memory registry implies all services are in the same process.
/// </summary>
public static class ServiceRegistry
{
    private static readonly Dictionary<string, ServiceDescriptor> _services = new();

    public static void Register(string serviceName, string baseUrl, string healthEndpoint)
    {
        _services[serviceName] = new ServiceDescriptor(serviceName, baseUrl, healthEndpoint);
    }

    public static ServiceDescriptor? GetService(string serviceName)
    {
        return _services.TryGetValue(serviceName, out var descriptor) ? descriptor : null;
    }

    public static IReadOnlyCollection<ServiceDescriptor> GetAll() => _services.Values.ToList().AsReadOnly();

    public static void Clear() => _services.Clear();
}

public record ServiceDescriptor(string Name, string BaseUrl, string HealthEndpoint);
