using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PensionAgent.Api.Data;
using PensionAgent.Api.Dtos;

namespace PensionAgent.Api.Services;

public sealed class PortfolioAggregatorService(
    IDbContextFactory<PensionDbContext> contextFactory,
    StatePensionProvider stateProvider,
    OccupationalPensionProvider occupationalProvider,
    LifeAndHealthInsuranceProvider insuranceProvider,
    ILogger<PortfolioAggregatorService> logger)
{
    private static readonly TimeSpan ProviderTimeout = TimeSpan.FromSeconds(2);

    public async Task<PensionSummaryDto> GetPortfolioAsync(string userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var user = await dbContext.UserProfiles.AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException($"User '{userId}' was not found.");
        }

        var stateTask = FetchProviderAsync(
            "statePension",
            token => stateProvider.FetchAsync(userId, token),
            cancellationToken);
        var occupationalTask = FetchProviderAsync(
            "occupationalPension",
            token => occupationalProvider.FetchAsync(userId, token),
            cancellationToken);
        var insuranceTask = FetchProviderAsync(
            "lifeAndHealthInsurance",
            token => insuranceProvider.FetchAsync(userId, token),
            cancellationToken);

        await Task.WhenAll(stateTask, occupationalTask, insuranceTask);
        var state = await stateTask;
        var occupational = await occupationalTask;
        var insurance = await insuranceTask;

        var accounts = new List<PensionAccountDto>();
        if (state.Value is not null)
        {
            accounts.Add(new PensionAccountDto(
                "State",
                "Swedish Pensions Agency",
                state.Value.CapitalSek,
                state.Value.MonthlyAccrualSek,
                state.Value.LastUpdated));
        }
        if (occupational.Value is not null)
        {
            accounts.AddRange(occupational.Value.Plans.Select(plan => new PensionAccountDto(
                "Occupational",
                plan.PlanName,
                plan.BalanceSek,
                plan.MonthlyPremiumSek,
                plan.LastUpdated)));
        }

        var policies = insurance.Value?.Coverages.Select(coverage => new InsurancePolicyDto(
            coverage.CoverageType,
            coverage.Insurer,
            coverage.SumInsuredSek,
            coverage.Active,
            coverage.LastUpdated)).ToList() ?? [];
        var providers = new[] { state.Status, occupational.Status, insurance.Status };

        return new PensionSummaryDto(
            user.UserId,
            user.FullName,
            user.Age,
            accounts.Sum(account => account.Balance),
            accounts.Sum(account => account.MonthlyContribution),
            accounts,
            policies,
            providers);
    }

    private async Task<ProviderFetchResult<T>> FetchProviderAsync<T>(
        string providerName,
        Func<CancellationToken, Task<T>> fetch,
        CancellationToken requestCancellation)
    {
        var stopwatch = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestCancellation);
        timeout.CancelAfter(ProviderTimeout);
        try
        {
            var result = await fetch(timeout.Token);
            stopwatch.Stop();
            return new ProviderFetchResult<T>(
                result,
                new ProviderStatusDto(providerName, "available", "Provider data retrieved.", stopwatch.ElapsedMilliseconds));
        }
        catch (OperationCanceledException) when (requestCancellation.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            logger.LogWarning("Provider {ProviderName} timed out after {DurationMs} ms.", providerName, stopwatch.ElapsedMilliseconds);
            return new ProviderFetchResult<T>(
                default,
                new ProviderStatusDto(providerName, "timeout", "Provider request timed out.", stopwatch.ElapsedMilliseconds));
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            logger.LogWarning(exception, "Provider {ProviderName} failed.", providerName);
            return new ProviderFetchResult<T>(
                default,
                new ProviderStatusDto(providerName, "unavailable", "Provider data could not be retrieved.", stopwatch.ElapsedMilliseconds));
        }
    }

    private sealed record ProviderFetchResult<T>(T? Value, ProviderStatusDto Status);
}
