using ManagerCompass.Api.Services;

// Load KEY=VALUE pairs from a local, git-ignored .env file into environment variables so they reach
// configuration (e.g. ANTHROPIC_API_KEY). Real environment variables win; blank values are skipped.
var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
        var eq = trimmed.IndexOf('=');
        if (eq <= 0) continue;
        var name = trimmed[..eq].Trim();
        var value = trimmed[(eq + 1)..].Trim().Trim('"', '\'');
        if (value.Length > 0 && Environment.GetEnvironmentVariable(name) is null)
            Environment.SetEnvironmentVariable(name, value);
    }
}

var builder = WebApplication.CreateBuilder(args);

const string AngularDevCors = "AngularDev";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Swap this registration for a real SharePoint/Power BI-backed implementation once SME access is confirmed.
builder.Services.AddSingleton<IGuidanceDataService, MockGuidanceDataService>();
builder.Services.AddSingleton<IDataExplorerService, DataExplorerService>();
builder.Services.AddSingleton<ITeamNotesService, TeamNotesService>();

// AI provider: Ai:Provider ("anthropic" | "openai") wins; otherwise Anthropic if its key is set, else OpenAI.
var aiProvider = builder.Configuration["Ai:Provider"]?.ToLowerInvariant()
    ?? (string.IsNullOrWhiteSpace(builder.Configuration["ANTHROPIC_API_KEY"]) ? "openai" : "anthropic");
if (aiProvider == "openai")
    builder.Services.AddHttpClient<IAiClient, OpenAiClient>(c => c.Timeout = TimeSpan.FromSeconds(60));
else
    builder.Services.AddHttpClient<IAiClient, ClaudeClient>(c => c.Timeout = TimeSpan.FromSeconds(60));

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCors, policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(AngularDevCors);
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
