namespace HybridApp.Domain;

public class Customer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime RegisteredAt { get; private set; }
    public bool IsActive { get; private set; }

    public static Customer Create(string name, string email)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            RegisteredAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
