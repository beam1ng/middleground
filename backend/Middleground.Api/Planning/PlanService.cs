using Middleground.Api.Models;
using Middleground.Api.Routing;

namespace Middleground.Api.Planning;

/// <summary>Builds the travel-time matrix and ranks candidates by the chosen objective.</summary>
public sealed class PlanService(IRoutingMatrixProvider routing, ILogger<PlanService> logger)
{
    public async Task<PlanResult> RankAsync(PlanRequest request, CancellationToken cancellationToken)
    {
        var sources = request.Members.Select(m => (m.Lat, m.Lng)).ToList();
        var destinations = request.Candidates.Select(c => (c.Lat, c.Lng)).ToList();

        // durations[memberIndex][candidateIndex] in seconds (null = unroutable).
        var durations = await routing.GetDurationsAsync(sources, destinations, cancellationToken);

        var scored = request.Candidates
            .Select((candidate, j) => Score(request, candidate, j, durations))
            .OrderByDescending(s => s.AllReachable) // reachable first
            .ThenBy(s => s.Score)
            .ToList();

        logger.LogInformation(
            "Ranked {CandidateCount} candidates for {MemberCount} members ({Objective})",
            request.Candidates.Count, request.Members.Count, request.Objective);

        return new PlanResult(request.Objective, scored);
    }

    private static DestinationScore Score(
        PlanRequest request, Candidate candidate, int candidateIndex, double?[][] durations)
    {
        var travels = request.Members
            .Select((m, i) => new MemberTravel(m.Id, m.Label, durations[i][candidateIndex]))
            .ToList();

        var allReachable = travels.All(t => t.DurationSeconds is not null);
        var reachable = travels.Where(t => t.DurationSeconds is not null)
            .Select(t => t.DurationSeconds!.Value)
            .ToList();

        double? max = reachable.Count > 0 ? reachable.Max() : null;
        double? sum = reachable.Count > 0 ? reachable.Sum() : null;
        double? spread = reachable.Count > 0 ? reachable.Max() - reachable.Min() : null;

        var weightedSum = request.Members
            .Select((m, i) => (durations[i][candidateIndex] ?? 0) * m.Weight)
            .Sum();

        // Unreachable candidates get a worst-possible score so they sort last.
        var score = !allReachable
            ? double.PositiveInfinity
            : request.Objective switch
            {
                Objective.Minimax => max ?? double.PositiveInfinity,
                Objective.Sum => sum ?? double.PositiveInfinity,
                Objective.Spread => spread ?? double.PositiveInfinity,
                Objective.Weighted => weightedSum,
                _ => max ?? double.PositiveInfinity
            };

        return new DestinationScore(candidate, score, allReachable, max, sum, spread, travels);
    }
}
