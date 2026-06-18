namespace Middleground.Api.Models;

/// <summary>Fairness objective used to rank candidate destinations.</summary>
public enum Objective
{
    /// <summary>Minimize the worst (maximum) member travel time — the "fairest" meeting.</summary>
    Minimax,
    /// <summary>Minimize the total/sum of member travel times — utilitarian.</summary>
    Sum,
    /// <summary>Minimize the spread (max − min) of member travel times — equality.</summary>
    Spread,
    /// <summary>Minimize the weighted sum of member travel times.</summary>
    Weighted
}

/// <summary>An origin point belonging to one participant.</summary>
public sealed record MemberPin(string Id, string? Label, double Lat, double Lng, double Weight = 1);

/// <summary>A possible meeting destination evaluated against all members.</summary>
public sealed record Candidate(string Id, string Name, double Lat, double Lng);

/// <summary>Request to rank candidate destinations for a set of member pins.</summary>
public sealed record PlanRequest(
    IReadOnlyList<MemberPin> Members,
    IReadOnlyList<Candidate> Candidates,
    Objective Objective = Objective.Minimax);

/// <summary>Travel time for one member to a specific destination.</summary>
public sealed record MemberTravel(string MemberId, string? Label, double? DurationSeconds);

/// <summary>A candidate destination scored against all members.</summary>
public sealed record DestinationScore(
    Candidate Candidate,
    double Score,
    bool AllReachable,
    double? MaxSeconds,
    double? SumSeconds,
    double? SpreadSeconds,
    IReadOnlyList<MemberTravel> MemberTravels);

/// <summary>Ranked plan result (best destination first).</summary>
public sealed record PlanResult(Objective Objective, IReadOnlyList<DestinationScore> Ranked);
