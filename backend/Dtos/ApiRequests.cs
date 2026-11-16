namespace PensionAgent.Api.Dtos;

public sealed record SimulationRequest(string? UserId, int RetirementAge, decimal ExtraMonthlySaving);

public sealed record SimulationResponse(
    string UserId,
    int RetirementAge,
    decimal CurrentCapital,
    decimal CurrentMonthlyContribution,
    decimal ExtraMonthlySaving,
    decimal ExpectedAnnualReturn,
    int YearsToRetirement,
    decimal ProjectedCapital,
    decimal EstimatedMonthlyPayout);

public sealed record AdvisorChatRequest(
    string? UserId,
    string Message,
    IReadOnlyList<ConversationMessage>? ConversationHistory);

public sealed record ConversationMessage(string Role, string Content);

public sealed record ToolTraceDto(
    string ToolName,
    string ArgumentsJson,
    System.Text.Json.JsonElement? Arguments,
    System.Text.Json.JsonElement Output);

public sealed record AdvisorChatResponse(string Reply, IReadOnlyList<ToolTraceDto> ToolTraces);

public sealed record ProjectionResult(
    int YearsToRetirement,
    decimal ProjectedCapital,
    decimal EstimatedMonthlyPayout);

public sealed record CoverageGapReport(
    string UserId,
    bool HasSicknessInsurance,
    bool HasSurvivorProtection,
    IReadOnlyList<string> Gaps);
