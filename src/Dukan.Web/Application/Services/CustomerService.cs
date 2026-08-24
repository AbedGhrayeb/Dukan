using Dukan.Web.Application.DTOs;
using Dukan.Web.Application.Interfaces;
using Dukan.Web.Application.Mapper;
using Dukan.Web.Data;
using Dukan.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dukan.Web.Application.Services;

public sealed class CustomerService(
    ApplicationDbContext db,
    IAuditLogger auditLogger,
    ILogger<CustomerService> logger) : ICustomerService
{
    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(term) ||
                c.StoreName.ToLower().Contains(term) ||
                c.Phone.Contains(term) ||
                c.WhatsAppNumber.Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => c.ToDto())
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<CustomerDto>(items, page, pageSize, total);
    }

    public async Task<CustomerDto?> GetCustomerAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .Include(c => c.Subscriptions)
                .ThenInclude(s => s.Plan)
            .SingleOrDefaultAsync(c => c.Id == id, ct);

        return customer?.ToDto();
    }

    public async Task<bool> UpdateNotesAsync(Guid id, string? notes, Guid? userId, CancellationToken ct = default)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == id, ct);

        if (customer is null)
        {
            return false;
        }

        customer.Notes = notes?.Trim();
        customer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(
            nameof(Customer),
            customer.Id.ToString(),
            "Customer.Updated",
            "تم تعديل بيانات العميل.",
            userId,
            ct);

        logger.LogInformation("Customer {CustomerId} updated.", customer.Id);

        return true;
    }
}
