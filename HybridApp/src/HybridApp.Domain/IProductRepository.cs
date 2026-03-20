namespace HybridApp.Domain;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetBySkuPrefixAsync(string skuPrefix, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
