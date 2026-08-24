using Dukan.Web.Application.Configuration;
using Dukan.Web.Application.Interfaces;
using Dukan.Web.Application.Services;
using Dukan.Web.Data;
using Dukan.Web.Domain.Entities;
using Dukan.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dukan.Web.Tests;

internal static class TestHarness
{
    public static ApplicationDbContext CreateDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString("N"))
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static IAuditLogger CreateAuditLogger(ApplicationDbContext db) => new AuditLogger(db);

    public static IPlanService CreatePlanService(ApplicationDbContext db) =>
        new PlanService(db, CreateAuditLogger(db), NullLogger<PlanService>.Instance);

    public static ISubscriptionService CreateSubscriptionService(ApplicationDbContext db) =>
        new SubscriptionService(
            db,
            CreateAuditLogger(db),
            NullLogger<SubscriptionService>.Instance);

    public static Plan CreatePlan(
        string name = "خطة شهرية",
        int duration = 1,
        DurationUnit unit = DurationUnit.Month,
        decimal price = 49,
        bool isActive = true,
        bool isTrial = false) => new()
    {
        Name = name,
        Duration = duration,
        DurationUnit = unit,
        Price = price,
        Currency = "SAR",
        IsTrial = isTrial,
        IsActive = isActive,
        DisplayOrder = 1,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    public static Customer CreateCustomer(
        string name = "محمد أحمد",
        string storeName = "متجر محمد",
        string phone = "0500000001",
        string? whatsApp = null) => new()
    {
        FullName = name,
        StoreName = storeName,
        Phone = phone,
        WhatsAppNumber = whatsApp ?? phone,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    public static Subscription CreateSubscription(
        Customer customer,
        Plan plan,
        SubscriptionStatus status = SubscriptionStatus.Active,
        DateTime? start = null,
        DateTime? end = null) => new()
    {
        CustomerId = customer.Id,
        Customer = customer,
        PlanId = plan.Id,
        Plan = plan,
        Status = status,
        StartDate = start ?? DateTime.UtcNow,
        EndDate = end ?? DateTime.UtcNow.AddDays(30),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };
}
