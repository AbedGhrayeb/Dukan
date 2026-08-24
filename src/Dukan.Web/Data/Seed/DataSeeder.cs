using Dukan.Web.Application.Configuration;
using Dukan.Web.Data;
using Dukan.Web.Domain.Constants;
using Dukan.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dukan.Web.Data.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, SeedSettings settings)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Dukan.Seeder");

        await SeedPlansAsync(services, settings.Plans, logger);
        await SeedAdminAsync(services, settings.Admin, logger);
    }

    private static async Task SeedPlansAsync(IServiceProvider services, IReadOnlyList<SeedSettings.PlanSeed> plans, ILogger logger)
    {
        if (plans.Count == 0)
        {
            return;
        }

        var db = services.GetRequiredService<ApplicationDbContext>();

        if (await db.Plans.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        db.Plans.AddRange(plans.Select((p, index) => new Plan
        {
            Name = p.Name,
            Duration = p.Duration,
            DurationUnit = p.DurationUnit,
            Price = p.Price,
            Currency = p.Currency,
            IsTrial = p.IsTrial,
            IsActive = true,
            DisplayOrder = p.DisplayOrder,
            Description = p.Description,
            CreatedAt = now,
            UpdatedAt = now,
        }));

        await db.SaveChangesAsync();

        logger.LogInformation("Seeded {PlanCount} plans from configuration.", plans.Count);
    }

    private static async Task SeedAdminAsync(IServiceProvider services, SeedSettings.AdminSeed admin, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(admin.UserName) ||
            string.IsNullOrWhiteSpace(admin.Email) ||
            string.IsNullOrWhiteSpace(admin.Password))
        {
            logger.LogWarning("SeedData:Admin is not fully configured. The development admin user was not created.");
            return;
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (await userManager.FindByEmailAsync(admin.Email) is not null)
        {
            return;
        }

        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Admin));
        }

        var user = new ApplicationUser
        {
            UserName = admin.UserName,
            Email = admin.Email,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, admin.Password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, Roles.Admin);
            logger.LogInformation("Seeded development admin user '{UserName}'.", admin.UserName);
        }
        else
        {
            logger.LogError("Failed to seed development admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
