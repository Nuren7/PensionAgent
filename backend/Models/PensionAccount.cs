namespace PensionAgent.Api.Models;

public sealed class PensionAccount
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal MonthlyContribution { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public UserProfile UserProfile { get; set; } = null!;
}
