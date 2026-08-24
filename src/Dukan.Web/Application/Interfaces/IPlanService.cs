using Dukan.Web.Application.DTOs;
using Dukan.Web.Domain.Entities;

namespace Dukan.Web.Application.Interfaces;

public interface IPlanService
{
    Task<IReadOnlyList<PlanDto>> GetActivePlansAsync(CancellationToken ct = default);

    Task<IReadOnlyList<PlanDto>> GetPlansAsync(CancellationToken ct = default);

    Task<PlanDto?> GetPlanAsync(Guid id, CancellationToken ct = default);

    Task<Plan> CreateAsync(PlanForm form, Guid? userId, CancellationToken ct = default);

    Task<bool> UpdateAsync(Guid id, PlanForm form, Guid? userId, CancellationToken ct = default);

    Task<bool> SetActiveAsync(Guid id, bool isActive, Guid? userId, CancellationToken ct = default);

    Task<(bool Success, string? Error)> DeleteAsync(Guid id, Guid? userId, CancellationToken ct = default);
}
