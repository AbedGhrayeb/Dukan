using Dukan.Web.Domain.Enums;

namespace Dukan.Web.Application.Configuration;

public sealed class SeedSettings
{
    public const string SectionName = "SeedData";

    public IReadOnlyList<PlanSeed> Plans { get; init; } = [];

    public AdminSeed Admin { get; init; } = new();

    public sealed class PlanSeed
    {
        public string Name { get; init; } = string.Empty;

        public int Duration { get; init; }

        public DurationUnit DurationUnit { get; init; }

        public decimal Price { get; init; }

        public string Currency { get; init; } = "ILS";

        public bool IsTrial { get; init; }

        public int DisplayOrder { get; init; }

        public string Description { get; init; } = string.Empty;
    }

    public sealed class AdminSeed
    {
        public string UserName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;
    }
}
