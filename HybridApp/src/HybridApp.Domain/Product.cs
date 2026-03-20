namespace HybridApp.Domain;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }

    public static Product Create(string name, string sku, decimal price)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Sku = sku,
            Price = price,
            StockQuantity = 0
        };
    }

    public void AdjustStock(int quantity)
    {
        StockQuantity += quantity;
        if (StockQuantity < 0)
            StockQuantity = 0;
    }
}
