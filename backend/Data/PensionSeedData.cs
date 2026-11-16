using Microsoft.EntityFrameworkCore;
using PensionAgent.Api.Models;

namespace PensionAgent.Api.Data;

public static class PensionSeedData
{
    public const string DemoUserId = "user-seb-001";

    public static async Task SeedAsync(PensionDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.UserProfiles.AnyAsync(profile => profile.UserId == DemoUserId, cancellationToken))
        {
            return;
        }

        var user = new UserProfile
        {
            UserId = DemoUserId,
            FullName = "Alexandra Svensson",
            BirthYear = 1984,
            PensionAccounts =
            [
                new PensionAccount
                {
                    Id = Guid.Parse("24fe2f25-6028-4af3-bfa5-e0703a0922cb"),
                    UserId = DemoUserId,
                    AccountType = "State",
                    ProviderName = "Swedish Pensions Agency",
                    Balance = 1_140_000m,
                    MonthlyContribution = 4_200m,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new PensionAccount
                {
                    Id = Guid.Parse("3cb9bbbe-850a-43ac-b9cc-412cc85061a8"),
                    UserId = DemoUserId,
                    AccountType = "Occupational",
                    ProviderName = "SEB Pension",
                    Balance = 865_000m,
                    MonthlyContribution = 5_800m,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
            ],
            InsurancePolicies =
            [
                new InsurancePolicy
                {
                    Id = Guid.Parse("eb7f7d93-7950-49d5-907d-7d269d068001"),
                    UserId = DemoUserId,
                    PolicyType = "Life",
                    ProviderName = "SEB Life",
                    CoverageAmount = 1_500_000m,
                    IsActive = true,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new InsurancePolicy
                {
                    Id = Guid.Parse("eb7f7d93-7950-49d5-907d-7d269d068002"),
                    UserId = DemoUserId,
                    PolicyType = "Sickness",
                    ProviderName = "SEB Insurance",
                    CoverageAmount = 25_000m,
                    IsActive = true,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        dbContext.UserProfiles.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
