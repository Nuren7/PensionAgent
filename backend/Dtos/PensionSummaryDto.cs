namespace PensionAgent.Api.Dtos;

public sealed record PensionSummaryDto(
    string UserId,
    string FullName,
    int CurrentAge,
    decimal TotalCapital,
    decimal MonthlyContribution,
    IReadOnlyList<PensionAccountDto> Accounts,
    IReadOnlyList<InsurancePolicyDto> InsurancePolicies,
    IReadOnlyList<ProviderStatusDto> Providers);

public sealed record PensionAccountDto(
    string AccountType,
    string ProviderName,
    decimal Balance,
    decimal MonthlyContribution,
    DateTimeOffset UpdatedAt);

public sealed record InsurancePolicyDto(
    string PolicyType,
    string ProviderName,
    decimal CoverageAmount,
    bool IsActive,
    DateTimeOffset UpdatedAt);

public sealed record ProviderStatusDto(
    string Provider,
    string Status,
    string Message,
    long DurationMs);
