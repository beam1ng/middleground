namespace Middleground.Api.Routing;

/// <summary>Abstraction over a travel-time matrix backend (OSRM today, OTP/Valhalla later).</summary>
public interface IRoutingMatrixProvider
{
    /// <summary>
    /// Returns a durations matrix in seconds. Result[i][j] is the travel time from
    /// <paramref name="sources"/>[i] to <paramref name="destinations"/>[j], or null if unroutable.
    /// </summary>
    Task<double?[][]> GetDurationsAsync(
        IReadOnlyList<(double Lat, double Lng)> sources,
        IReadOnlyList<(double Lat, double Lng)> destinations,
        CancellationToken cancellationToken);
}
