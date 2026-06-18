using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Middleground.Api.Routing;

/// <summary>
/// Travel-time matrix via the OSRM <c>table</c> service. All sources and destinations are
/// packed into one coordinate list; OSRM is asked for the sources×destinations sub-matrix.
/// </summary>
public sealed class OsrmMatrixProvider(
    HttpClient httpClient,
    IOptions<OsrmOptions> options,
    ILogger<OsrmMatrixProvider> logger) : IRoutingMatrixProvider
{
    private readonly string _baseUrl = options.Value.BaseUrl.TrimEnd('/');

    public async Task<double?[][]> GetDurationsAsync(
        IReadOnlyList<(double Lat, double Lng)> sources,
        IReadOnlyList<(double Lat, double Lng)> destinations,
        CancellationToken cancellationToken)
    {
        if (sources.Count == 0 || destinations.Count == 0)
        {
            return [];
        }

        var all = new List<(double Lat, double Lng)>(sources.Count + destinations.Count);
        all.AddRange(sources);
        all.AddRange(destinations);

        // OSRM expects lng,lat order, ';'-separated.
        var coords = string.Join(';', all.Select(p =>
            string.Create(CultureInfo.InvariantCulture, $"{p.Lng},{p.Lat}")));

        var sourceIdx = string.Join(';', Enumerable.Range(0, sources.Count));
        var destIdx = string.Join(';', Enumerable.Range(sources.Count, destinations.Count));

        var url = $"{_baseUrl}/table/v1/driving/{coords}" +
                  $"?sources={sourceIdx}&destinations={destIdx}&annotations=duration";

        var table = await httpClient.GetFromJsonAsync<OsrmTableResponse>(url, cancellationToken)
            ?? throw new InvalidOperationException("OSRM returned an empty response.");

        if (!string.Equals(table.Code, "Ok", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError("OSRM table request failed with code {Code}", table.Code);
            throw new InvalidOperationException($"OSRM error: {table.Code}");
        }

        return table.Durations ?? throw new InvalidOperationException("OSRM returned no durations.");
    }

    private sealed record OsrmTableResponse
    {
        [JsonPropertyName("code")] public string? Code { get; init; }
        [JsonPropertyName("durations")] public double?[][]? Durations { get; init; }
    }
}
