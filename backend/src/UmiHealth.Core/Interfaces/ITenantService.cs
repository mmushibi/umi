namespace UmiHealth.Core.Interfaces;

public interface ITenantService
{
    Task<Tenant?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Tenant?> GetTenantBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default);
    Task<Tenant> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task<Tenant> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Branch> CreateBranchAsync(Guid tenantId, CreateBranchRequest request, CancellationToken cancellationToken = default);
    Task<Branch> UpdateBranchAsync(Guid branchId, UpdateBranchRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteBranchAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Branch>> GetTenantBranchesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> IsSubscriptionActiveAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public record CreateTenantRequest(
    string Name,
    string Description,
    string Subdomain,
    string ContactEmail,
    string ContactPhone,
    string Address,
    string City,
    string Country,
    string PostalCode,
    string SubscriptionPlan
);

public record UpdateTenantRequest(
    string Name,
    string Description,
    string ContactEmail,
    string ContactPhone,
    string Address,
    string City,
    string Country,
    string PostalCode
);

public record CreateBranchRequest(
    string Name,
    string Code,
    string Address,
    string City,
    string Country,
    string PostalCode,
    string Phone,
    string Email,
    bool IsMainBranch,
    string? ManagerName,
    string? ManagerPhone
);

public record UpdateBranchRequest(
    string Name,
    string Code,
    string Address,
    string City,
    string Country,
    string PostalCode,
    string Phone,
    string Email,
    string? ManagerName,
    string? ManagerPhone
);
