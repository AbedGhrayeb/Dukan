using Dukan.Web.Application.DTOs;

namespace Dukan.Web.Application.Interfaces;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(string? search, int page, int pageSize, CancellationToken ct = default);

    Task<CustomerDto?> GetCustomerAsync(Guid id, CancellationToken ct = default);

    Task<bool> UpdateNotesAsync(Guid id, string? notes, Guid? userId, CancellationToken ct = default);
}
