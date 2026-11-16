using Microsoft.EntityFrameworkCore;
using PensionAgent.Api.Models;

namespace PensionAgent.Api.Data;

public sealed class PensionDbContext(DbContextOptions<PensionDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<PensionAccount> PensionAccounts => Set<PensionAccount>();
    public DbSet<InsurancePolicy> InsurancePolicies => Set<InsurancePolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(profile => profile.UserId);
            entity.Property(profile => profile.UserId).HasMaxLength(100);
            entity.Property(profile => profile.FullName).HasMaxLength(200);
        });

        modelBuilder.Entity<PensionAccount>(entity =>
        {
            entity.HasKey(account => account.Id);
            entity.Property(account => account.AccountType).HasMaxLength(50);
            entity.Property(account => account.ProviderName).HasMaxLength(100);
            entity.Property(account => account.Balance).HasPrecision(18, 2);
            entity.Property(account => account.MonthlyContribution).HasPrecision(18, 2);
            entity.HasOne(account => account.UserProfile)
                .WithMany(profile => profile.PensionAccounts)
                .HasForeignKey(account => account.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(account => account.UserId);
        });

        modelBuilder.Entity<InsurancePolicy>(entity =>
        {
            entity.HasKey(policy => policy.Id);
            entity.Property(policy => policy.PolicyType).HasMaxLength(50);
            entity.Property(policy => policy.ProviderName).HasMaxLength(100);
            entity.Property(policy => policy.CoverageAmount).HasPrecision(18, 2);
            entity.HasOne(policy => policy.UserProfile)
                .WithMany(profile => profile.InsurancePolicies)
                .HasForeignKey(policy => policy.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(policy => policy.UserId);
        });
    }
}
