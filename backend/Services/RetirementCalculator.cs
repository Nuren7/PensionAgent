using PensionAgent.Api.Dtos;

namespace PensionAgent.Api.Services;

public sealed class RetirementCalculator
{
    private const int RetirementPayoutMonths = 20 * 12;

    public ProjectionResult Calculate(
        decimal currentCapital,
        decimal monthlyContribution,
        int currentAge,
        int retirementAge,
        decimal expectedAnnualReturn)
    {
        if (currentCapital < 0 || monthlyContribution < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentCapital), "Capital and monthly contribution cannot be negative.");
        }
        if (currentAge < 0 || retirementAge < currentAge || retirementAge > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(retirementAge), "Retirement age must be between current age and 100.");
        }
        if (expectedAnnualReturn is < 0 or > 0.25m)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedAnnualReturn), "Expected annual return must be between 0 and 25 percent.");
        }

        var years = retirementAge - currentAge;
        var months = years * 12;
        var monthlyRate = (double)Math.Pow((double)(1 + expectedAnnualReturn), 1d / 12d) - 1d;
        var capital = (double)currentCapital;
        var contribution = (double)monthlyContribution;
        for (var month = 0; month < months; month++)
        {
            capital = capital * (1 + monthlyRate) + contribution;
        }

        var payoutMonthlyRate = Math.Pow(1.02d, 1d / 12d) - 1d;
        var payoutFactor = payoutMonthlyRate == 0
            ? RetirementPayoutMonths
            : (1 - Math.Pow(1 + payoutMonthlyRate, -RetirementPayoutMonths)) / payoutMonthlyRate;
        var monthlyPayout = capital / payoutFactor;

        return new ProjectionResult(
            years,
            decimal.Round((decimal)capital, 2),
            decimal.Round((decimal)monthlyPayout, 2));
    }
}
