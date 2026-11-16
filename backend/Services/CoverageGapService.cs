using Microsoft.EntityFrameworkCore;
using PensionAgent.Api.Data;
using PensionAgent.Api.Dtos;

namespace PensionAgent.Api.Services;

public sealed class CoverageGapService(IDbContextFactory<PensionDbContext> contextFactory)
{
    public async Task<CoverageGapReport> DetectAsync(string userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var userExists = await dbContext.UserProfiles.AsNoTracking()
            .AnyAsync(profile => profile.UserId == userId, cancellationToken);
        if (!userExists)
        {
            throw new KeyNotFoundException($"User '{userId}' was not found.");
        }

        var activeTypes = await dbContext.InsurancePolicies.AsNoTracking()
            .Where(policy => policy.UserId == userId && policy.IsActive)
            .Select(policy => policy.PolicyType)
            .ToListAsync(cancellationToken);
        var hasSickness = activeTypes.Any(type =>
            type.Equals("Sickness", StringComparison.OrdinalIgnoreCase)
            || type.Equals("Sjukförsäkring", StringComparison.OrdinalIgnoreCase));
        var hasSurvivor = activeTypes.Any(type =>
            type.Equals("Survivor", StringComparison.OrdinalIgnoreCase)
            || type.Equals("SurvivorProtection", StringComparison.OrdinalIgnoreCase)
            || type.Equals("Efterlevandeskydd", StringComparison.OrdinalIgnoreCase));
        var gaps = new List<string>();
        if (!hasSickness)
        {
            gaps.Add("Missing sickness insurance (sjukförsäkring).");
        }
        if (!hasSurvivor)
        {
            gaps.Add("Missing survivor protection (efterlevandeskydd).");
        }

        return new CoverageGapReport(userId, hasSickness, hasSurvivor, gaps);
    }
}
