using Middleground.Api.Models;

namespace Middleground.Api.Planning;

/// <summary>Boundary validation for incoming plan requests.</summary>
public static class PlanRequestValidator
{
    private const int MaxMembers = 25;
    private const int MaxCandidates = 100;

    public static bool TryValidate(PlanRequest request, out string error)
    {
        if (request.Members is null || request.Members.Count == 0)
        {
            error = "At least one member pin is required.";
            return false;
        }

        if (request.Members.Count > MaxMembers)
        {
            error = $"Too many members (max {MaxMembers}).";
            return false;
        }

        if (request.Candidates is null || request.Candidates.Count == 0)
        {
            error = "At least one candidate destination is required.";
            return false;
        }

        if (request.Candidates.Count > MaxCandidates)
        {
            error = $"Too many candidates (max {MaxCandidates}).";
            return false;
        }

        foreach (var m in request.Members)
        {
            if (!IsValidCoord(m.Lat, m.Lng))
            {
                error = $"Member '{m.Id}' has invalid coordinates.";
                return false;
            }

            if (m.Weight <= 0)
            {
                error = $"Member '{m.Id}' must have a positive weight.";
                return false;
            }
        }

        foreach (var c in request.Candidates)
        {
            if (!IsValidCoord(c.Lat, c.Lng))
            {
                error = $"Candidate '{c.Id}' has invalid coordinates.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool IsValidCoord(double lat, double lng) =>
        lat is >= -90 and <= 90 && lng is >= -180 and <= 180;
}
