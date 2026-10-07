namespace ManagerCompass.Api.Models;

public class AskRequest
{
    public string Text { get; set; } = "";
    public string? TopicKey { get; set; }
}

/// <summary>Result of asking Manager Compass a question: a grounded topic answer, a guardrail refusal, or a clarifying question.</summary>
public class AskResponse
{
    public string Type { get; set; } = ""; // "topic" | "guardrail" | "clarify"
    public Topic? Topic { get; set; }
    public string? Message { get; set; }
}
