namespace PensionAgent.Api.Models;

public sealed class InsurancePolicy
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PolicyType { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public decimal CoverageAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public UserProfile UserProfile { get; set; } = null!;
}
