namespace PensionAgent.Api.Models;

public sealed class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int BirthYear { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Age => DateTime.UtcNow.Year - BirthYear;
    public ICollection<PensionAccount> PensionAccounts { get; set; } = new List<PensionAccount>();
    public ICollection<InsurancePolicy> InsurancePolicies { get; set; } = new List<InsurancePolicy>();
}
