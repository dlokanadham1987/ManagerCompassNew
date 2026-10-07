namespace ManagerCompass.Api.Models;

/// <summary>One guidance category (recruiting, engagement, etc.) with its grounded answer.</summary>
public class Topic
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Blurb { get; set; } = "";
    public string Color { get; set; } = "";
    public string Contact { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string Answer { get; set; } = "";
    public List<string> Sources { get; set; } = new();
    public List<string> Checklist { get; set; } = new();
    public string Escalation { get; set; } = "";
    public List<string> Keywords { get; set; } = new();

    /// <summary>Alternate example phrasings for this same topic, shown as follow-up prompt chips
    /// once a manager has opened it — distinct from <see cref="Prompt"/>, which is the one asked
    /// by default when the topic is picked from the side nav.</summary>
    public List<string> ExamplePrompts { get; set; } = new();
}
