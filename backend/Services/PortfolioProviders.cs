using Microsoft.EntityFrameworkCore;
using PensionAgent.Api.Data;
using PensionAgent.Api.Models;

namespace PensionAgent.Api.Services;

public sealed record StatePensionResponse(decimal CapitalSek, decimal MonthlyAccrualSek, DateTimeOffset LastUpdated);
public sealed record OccupationalPlan(string PlanName, decimal BalanceSek, decimal MonthlyPremiumSek, DateTimeOffset LastUpdated);
public sealed record OccupationalPensionResponse(IReadOnlyList<OccupationalPlan> Plans);
public sealed record CoverageRecord(string CoverageType, string Insurer, decimal SumInsuredSek, bool Active, DateTimeOffset LastUpdated);
public sealed record InsuranceProviderResponse(IReadOnlyList<CoverageRecord> Coverages);

public sealed class StatePensionProvider(IDbContextFactory<PensionDbContext> contextFactory)
{
    public async Task<StatePensionResponse> FetchAsync(string userId, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(180), cancellationToken);
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var accounts = await dbContext.PensionAccounts.AsNoTracking()
            .Where(account => account.UserId == userId && account.AccountType == "State")
            .ToListAsync(cancellationToken);
        return new StatePensionResponse(
            accounts.Sum(account => account.Balance),
            accounts.Sum(account => account.MonthlyContribution),
            accounts.Select(account => account.UpdatedAt).DefaultIfEmpty(DateTimeOffset.UtcNow).Max());
    }
}

public sealed class OccupationalPensionProvider(IDbContextFactory<PensionDbContext> contextFactory)
{
    public async Task<OccupationalPensionResponse> FetchAsync(string userId, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(260), cancellationToken);
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var records = await dbContext.PensionAccounts.AsNoTracking()
            .Where(account => account.UserId == userId && account.AccountType == "Occupational")
            .Select(account => new
            {
                account.ProviderName,
                account.Balance,
                account.MonthlyContribution,
                account.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new OccupationalPensionResponse(records.Select(record => new OccupationalPlan(
            record.ProviderName,
            record.Balance,
            record.MonthlyContribution,
            record.UpdatedAt)).ToList());
    }
}

public sealed class LifeAndHealthInsuranceProvider(IDbContextFactory<PensionDbContext> contextFactory)
{
    public async Task<InsuranceProviderResponse> FetchAsync(string userId, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(220), cancellationToken);
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var policies = await dbContext.InsurancePolicies.AsNoTracking()
            .Where(policy => policy.UserId == userId)
            .Select(policy => new
            {
                policy.PolicyType,
                policy.ProviderName,
                policy.CoverageAmount,
                policy.IsActive,
                policy.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new InsuranceProviderResponse(policies.Select(policy => new CoverageRecord(
            policy.PolicyType,
            policy.ProviderName,
            policy.CoverageAmount,
            policy.IsActive,
            policy.UpdatedAt)).ToList());
    }
}
