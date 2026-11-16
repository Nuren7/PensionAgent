using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PensionAgent.Api.Data;
using PensionAgent.Api.Dtos;

namespace PensionAgent.Api.Services;

public sealed class AdvisorUnavailableException(string message) : Exception(message);

public sealed class AgenticAdvisorService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    PortfolioAggregatorService portfolioAggregator,
    CoverageGapService coverageGapService,
    RetirementCalculator retirementCalculator)
{
    private const string DefaultModel = "openai/gpt-oss-120b";
    private const int MaximumToolRounds = 6;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<AdvisorChatResponse> ChatAsync(AdvisorChatRequest request, CancellationToken cancellationToken)
    {
        var apiKey = configuration["GROQ_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new AdvisorUnavailableException("The GROQ_API_KEY environment variable is required for advisor chat.");
        }

        var userId = string.IsNullOrWhiteSpace(request.UserId) ? PensionSeedData.DemoUserId : request.UserId;
        var messages = new List<GroqMessage>
        {
            new("system", """
                You are a careful Swedish pension and insurance information assistant. Use tools for user-specific facts and calculations. Never invent portfolio values. Explain assumptions, distinguish estimates from guarantees, and do not present your response as regulated financial advice. Use English unless the user writes in another language.
                """)
        };
        if (request.ConversationHistory is not null)
        {
            foreach (var item in request.ConversationHistory.TakeLast(12))
            {
                if (item.Role is "user" or "assistant" && !string.IsNullOrWhiteSpace(item.Content))
                {
                    messages.Add(new GroqMessage(item.Role, item.Content));
                }
            }
        }
        messages.Add(new GroqMessage(
            "user",
            $"The active userId is '{userId}'. User message: {request.Message}"));

        var traces = new List<ToolTraceDto>();
        var client = httpClientFactory.CreateClient("groq");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        for (var round = 0; round < MaximumToolRounds; round++)
        {
            var response = await SendCompletionAsync(client, messages, cancellationToken);
            var assistantMessage = response.Choices.FirstOrDefault()?.Message
                ?? throw new HttpRequestException("Groq returned no assistant message.");
            messages.Add(assistantMessage);

            if (assistantMessage.ToolCalls is not { Count: > 0 } toolCalls)
            {
                return new AdvisorChatResponse(assistantMessage.Content ?? string.Empty, traces);
            }

            foreach (var toolCall in toolCalls)
            {
                var (arguments, output) = await ExecuteToolAsync(
                    toolCall.Function.Name,
                    toolCall.Function.Arguments,
                    userId,
                    cancellationToken);
                traces.Add(new ToolTraceDto(
                    toolCall.Function.Name,
                    toolCall.Function.Arguments,
                    arguments,
                    output));
                messages.Add(new GroqMessage(
                    "tool",
                    output.GetRawText(),
                    ToolCallId: toolCall.Id));
            }
        }

        throw new HttpRequestException($"Groq exceeded the maximum of {MaximumToolRounds} tool-calling rounds.");
    }

    private async Task<GroqResponse> SendCompletionAsync(
        HttpClient client,
        IReadOnlyList<GroqMessage> messages,
        CancellationToken cancellationToken)
    {
        var configuredModel = configuration["GROQ_MODEL"]?.Trim();
        var request = new GroqRequest(
            string.IsNullOrEmpty(configuredModel) ? DefaultModel : configuredModel,
            messages,
            CreateTools());
        using var content = new StringContent(JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("chat/completions", content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Groq returned HTTP {(int)response.StatusCode}: {responseBody}",
                null,
                response.StatusCode);
        }

        return JsonSerializer.Deserialize<GroqResponse>(responseBody, JsonOptions)
            ?? throw new HttpRequestException("Groq returned an invalid response body.");
    }

    private async Task<(JsonElement? Arguments, JsonElement Output)> ExecuteToolAsync(
        string toolName,
        string rawArguments,
        string activeUserId,
        CancellationToken cancellationToken)
    {
        JsonElement? parsedArguments = null;
        try
        {
            using var document = JsonDocument.Parse(rawArguments);
            parsedArguments = document.RootElement.Clone();
            var arguments = document.RootElement;
            object result = toolName switch
            {
                "get_user_portfolio" => await portfolioAggregator.GetPortfolioAsync(
                    GetString(arguments, "userId") ?? activeUserId,
                    cancellationToken),
                "calculate_retirement_projection" => Calculate(arguments),
                "detect_coverage_gaps" => await coverageGapService.DetectAsync(
                    GetString(arguments, "userId") ?? activeUserId,
                    cancellationToken),
                _ => throw new InvalidOperationException($"Unknown advisor tool '{toolName}'.")
            };
            return (parsedArguments, JsonSerializer.SerializeToElement(result, JsonOptions));
        }
        catch (JsonException exception)
        {
            return (parsedArguments, JsonSerializer.SerializeToElement(new { error = $"Invalid tool arguments: {exception.Message}" }, JsonOptions));
        }
        catch (ArgumentException exception)
        {
            return (parsedArguments, JsonSerializer.SerializeToElement(new { error = exception.Message }, JsonOptions));
        }
        catch (KeyNotFoundException exception)
        {
            return (parsedArguments, JsonSerializer.SerializeToElement(new { error = exception.Message }, JsonOptions));
        }
        catch (InvalidOperationException exception)
        {
            return (parsedArguments, JsonSerializer.SerializeToElement(new { error = exception.Message }, JsonOptions));
        }
    }

    private object Calculate(JsonElement arguments)
    {
        var currentCapital = GetDecimal(arguments, "currentCapital");
        var monthlyContribution = GetDecimal(arguments, "monthlyContribution");
        var retirementAge = GetInt32(arguments, "retirementAge");
        var expectedAnnualReturn = GetDecimal(arguments, "expectedAnnualReturn");
        return retirementCalculator.Calculate(
            currentCapital,
            monthlyContribution,
            40,
            retirementAge,
            expectedAnnualReturn);
    }

    private static string? GetString(JsonElement arguments, string propertyName) =>
        arguments.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static decimal GetDecimal(JsonElement arguments, string propertyName) =>
        arguments.TryGetProperty(propertyName, out var value) && value.TryGetDecimal(out var result)
            ? result
            : throw new ArgumentException($"Tool argument '{propertyName}' must be a number.");

    private static int GetInt32(JsonElement arguments, string propertyName) =>
        arguments.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result)
            ? result
            : throw new ArgumentException($"Tool argument '{propertyName}' must be an integer.");

    private static IReadOnlyList<GroqTool> CreateTools()
    {
        static JsonElement Schema(string json) => JsonDocument.Parse(json).RootElement.Clone();

        return
        [
            new GroqTool("function", new GroqFunction(
                "get_user_portfolio",
                "Fetch the user's aggregated state and occupational pension portfolio and insurance policies.",
                Schema("""{"type":"object","properties":{"userId":{"type":"string"}},"required":["userId"],"additionalProperties":false}"""))),
            new GroqTool("function", new GroqFunction(
                "calculate_retirement_projection",
                "Calculate projected pension capital and estimated monthly payout. This calculation assumes a current age of 40 because currentAge is not an input.",
                Schema("""{"type":"object","properties":{"currentCapital":{"type":"number"},"monthlyContribution":{"type":"number"},"retirementAge":{"type":"integer"},"expectedAnnualReturn":{"type":"number"}},"required":["currentCapital","monthlyContribution","retirementAge","expectedAnnualReturn"],"additionalProperties":false}"""))),
            new GroqTool("function", new GroqFunction(
                "detect_coverage_gaps",
                "Check for missing active sickness insurance (sjukförsäkring) or survivor protection (efterlevandeskydd).",
                Schema("""{"type":"object","properties":{"userId":{"type":"string"}},"required":["userId"],"additionalProperties":false}""")))
        ];
    }

    private sealed record GroqRequest(
        string Model,
        IReadOnlyList<GroqMessage> Messages,
        IReadOnlyList<GroqTool> Tools,
        [property: JsonPropertyName("tool_choice")] string ToolChoice = "auto");

    private sealed record GroqTool(string Type, GroqFunction Function);
    private sealed record GroqFunction(string Name, string Description, JsonElement Parameters);
    private sealed record GroqMessage(
        string Role,
        string? Content,
        [property: JsonPropertyName("tool_calls")] IReadOnlyList<GroqToolCall>? ToolCalls = null,
        [property: JsonPropertyName("tool_call_id")] string? ToolCallId = null);
    private sealed record GroqToolCall(string Id, string Type, GroqToolCallFunction Function);
    private sealed record GroqToolCallFunction(string Name, string Arguments);
    private sealed record GroqResponse(IReadOnlyList<GroqChoice> Choices);
    private sealed record GroqChoice(GroqMessage Message);
}
