namespace Middleground.Api.Routing;

/// <summary>Configuration for the OSRM routing backend.</summary>
public sealed class OsrmOptions
{
    public const string SectionName = "Osrm";

    /// <summary>Base URL of the OSRM HTTP API (public demo for MVP, self-hosted later).</summary>
    public required string BaseUrl { get; init; }
}
