using System.Text.Json.Serialization;
using Middleground.Api.Models;
using Middleground.Api.Planning;
using Middleground.Api.Routing;

var builder = WebApplication.CreateBuilder(args);

const string AngularCors = "angular-dev";

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOptions<OsrmOptions>()
    .Bind(builder.Configuration.GetSection(OsrmOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "Osrm:BaseUrl is required.")
    .ValidateOnStart();

builder.Services.AddHttpClient<IRoutingMatrixProvider, OsrmMatrixProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Middleground/0.1 (+portfolio MVP)");
});

builder.Services.AddScoped<PlanService>();

builder.Services.AddCors(options =>
    options.AddPolicy(AngularCors, policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(AngularCors);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/plan", async (
    PlanRequest request, PlanService planService, CancellationToken cancellationToken) =>
{
    if (!PlanRequestValidator.TryValidate(request, out var error))
    {
        return Results.BadRequest(ApiResponse<PlanResult>.Fail(error));
    }

    try
    {
        var result = await planService.RankAsync(request, cancellationToken);
        return Results.Ok(ApiResponse<PlanResult>.Ok(result));
    }
    catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
    {
        return Results.Json(
            ApiResponse<PlanResult>.Fail("Routing backend unavailable. Try again."),
            statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("CreatePlan");

app.Run();

// Exposed for integration testing via WebApplicationFactory.
public partial class Program;
