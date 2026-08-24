using Dukan.Web.Application.DTOs;
using Dukan.Web.Application.Services;
using Dukan.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dukan.Web.Tests;

public sealed class SubscriptionServiceTests
{
    [Theory]
    [InlineData(DurationUnit.Day, 7)]
    [InlineData(DurationUnit.Week, 2)]
    [InlineData(DurationUnit.Month, 3)]
    [InlineData(DurationUnit.Year, 2)]
    public void CalculateEndDate_ComputesExpectedDate(DurationUnit unit, int duration)
    {
        var start = new DateTime(2026, 1, 31, 12, 0, 0, DateTimeKind.Utc);
        var end = SubscriptionService.CalculateEndDate(start, duration, unit);
        Assert.Equal(unit switch
        {
            DurationUnit.Day => start.AddDays(duration),
            DurationUnit.Week => start.AddDays(duration * 7),
            DurationUnit.Month => start.AddMonths(duration),
            _ => start.AddYears(duration),
        }, end);
    }

    [Fact]
    public async Task NewRequestAsync_CreatesCustomerWithStoreName()
    {
        var db = TestHarness.CreateDbContext();
        var plan = TestHarness.CreatePlan();
        db.Plans.Add(plan);
        await db.SaveChangesAsync();

        var service = TestHarness.CreateSubscriptionService(db);
        var form = new SubscriptionRequestForm
        {
            FullName = "أحمد محمد",
            StoreName = "سوبرماركت الأمانة",
            Phone = "0500000001",
            WhatsAppNumber = "0500000001",
            PlanId = plan.Id,
            Notes = "ملاحظة"
        };

        var (success, _) = await service.NewRequestAsync(form);
        Assert.True(success);
        var customer = await db.Customers.FirstAsync(c => c.Phone == "0500000001");
        Assert.Equal("سوبرماركت الأمانة", customer.StoreName);
        Assert.Equal("أحمد محمد", customer.FullName);
    }

    [Fact]
    public async Task NewRequestAsync_RequiresStoreName()
    {
        var db = TestHarness.CreateDbContext();
        var plan = TestHarness.CreatePlan();
        db.Plans.Add(plan);
        await db.SaveChangesAsync();

        var service = TestHarness.CreateSubscriptionService(db);
        var form = new SubscriptionRequestForm
        {
            FullName = "أحمد محمد",
            StoreName = "",
            Phone = "0500000002",
            WhatsAppNumber = "0500000002",
            PlanId = plan.Id
        };

        var (success, error) = await service.NewRequestAsync(form);
        Assert.False(success);
        Assert.Contains("جميع الحقول", error);
    }

    [Fact]
    public async Task CancelAsync_CancelsActiveSubscription()
    {
        var db = TestHarness.CreateDbContext();
        var plan = TestHarness.CreatePlan();
        var customer = TestHarness.CreateCustomer();
        db.Plans.Add(plan);
        db.Customers.Add(customer);
        var subscription = TestHarness.CreateSubscription(customer, plan, SubscriptionStatus.Active);
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var service = TestHarness.CreateSubscriptionService(db);
        Assert.True(await service.CancelAsync(subscription.Id, null));
        Assert.Equal(SubscriptionStatus.Cancelled, (await db.Subscriptions.FindAsync(subscription.Id))!.Status);
    }

    [Fact]
    public async Task ExpireOverdueAsync_ExpiresOnlyOverdueSubscriptions()
    {
        var db = TestHarness.CreateDbContext();
        var plan = TestHarness.CreatePlan();
        var customer = TestHarness.CreateCustomer();
        db.Plans.Add(plan);
        db.Customers.Add(customer);

        var overdue = TestHarness.CreateSubscription(customer, plan, SubscriptionStatus.Active, end: DateTime.UtcNow.AddDays(-2));
        var stillActive = TestHarness.CreateSubscription(customer, plan, SubscriptionStatus.Active, end: DateTime.UtcNow.AddDays(2));

        db.Subscriptions.AddRange(overdue, stillActive);
        await db.SaveChangesAsync();

        var service = TestHarness.CreateSubscriptionService(db);
        var count = await service.ExpireOverdueAsync();
        Assert.Equal(1, count);
        Assert.Equal(SubscriptionStatus.Expired, (await db.Subscriptions.FindAsync(overdue.Id))!.Status);
        Assert.Equal(SubscriptionStatus.Active, (await db.Subscriptions.FindAsync(stillActive.Id))!.Status);
    }
}
