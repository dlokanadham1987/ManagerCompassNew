using ManagerCompass.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string AngularDevCors = "AngularDev";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Swap this registration for a real SharePoint/Power BI-backed implementation once SME access is confirmed.
builder.Services.AddSingleton<IGuidanceDataService, MockGuidanceDataService>();
builder.Services.AddSingleton<IDataExplorerService, DataExplorerService>();

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
