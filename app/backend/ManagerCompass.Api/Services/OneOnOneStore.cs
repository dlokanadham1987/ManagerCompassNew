using System.Text.Json;

namespace ManagerCompass.Api.Services;

public interface IOneOnOneStore
{
    /// <summary>The saved 1:1 state as JSON text, or an empty state if nothing has been saved yet.</summary>
    string Get();
    void Save(JsonElement state);
}

/// <summary>
/// Persists the whole 1:1 state (people, meetings, post notes, action items) to App_Data/one-on-ones.json.
/// Kept as raw JSON so the file mirrors exactly what the 1:1 screen works with; swap for a database-backed
/// store later without touching the controller.
/// </summary>
public class OneOnOneStore : IOneOnOneStore
{
    private const string EmptyState = """{"reports":[],"meetings":[]}""";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly object _lock = new();

    public OneOnOneStore(IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "one-on-ones.json");
    }

    public string Get()
    {
        lock (_lock)
            return File.Exists(_filePath) ? File.ReadAllText(_filePath) : EmptyState;
    }

    public void Save(JsonElement state)
    {
        lock (_lock)
            File.WriteAllText(_filePath, JsonSerializer.Serialize(state, JsonOptions));
    }
}
