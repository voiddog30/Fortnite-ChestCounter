namespace ChestCounter;

/// <summary>Rates per hour played and the dry streak since the last treasure.</summary>
public sealed record Rates(double HoursPlayed, double? ChestsPerHour, double? TreasuresPerHour, int ChestsSinceTreasure, bool AnyTreasure)
{
    /// <summary>"42.5" chests per hour, or "—" when no match has a known duration.</summary>
    public string ChestsPerHourText => ChestsPerHour is double v ? v.ToString("0.0", L.Culture) : "—";

    /// <summary>Treasures are rare: two decimals below 1 per hour ("0.35"), one above.</summary>
    public string TreasuresPerHourText => TreasuresPerHour is double v ? v.ToString(v < 1 ? "0.00" : "0.0", L.Culture) : "—";

    public string HoursPlayedText => HoursPlayed.ToString(HoursPlayed < 10 ? "0.0" : "0", L.Culture) + " h";
}

public static class Stats
{
    /// <param name="matches">Finished matches (any order).</param>
    /// <param name="live">Match in progress, counted after the finished ones.</param>
    public static Rates Compute(IEnumerable<MatchRecord> matches, MatchRecord? live = null)
    {
        var ordered = matches.OrderBy(m => m.MatchStartUtc ?? m.AnalyzedAtUtc).ToList();
        if (live != null)
        {
            ordered.Add(live);
        }

        // Matches without a known duration are left out of both sides of the rates.
        var timed = ordered.Where(m => m.DurationSeconds is > 0).ToList();
        var hours = timed.Sum(m => m.DurationSeconds!.Value) / 3600.0;
        double? chestsPerHour = hours > 0 ? timed.Sum(m => m.ChestsOpened) / hours : null;
        double? treasuresPerHour = hours > 0 ? timed.Sum(m => m.TreasureFinds.Count) / hours : null;

        var since = 0;
        var any = false;
        foreach (var m in ordered)
        {
            if (m.TreasureFinds.Count == 0)
            {
                since += m.ChestsOpened;
                continue;
            }
            any = true;
            // A treasure of unknown time is taken as the end of the match: no chest of that match counts.
            var last = m.TreasureFinds.Max(f => f.ObtainedAtSeconds ?? float.MaxValue);
            since = m.Chests.Count(c => c.TimeSeconds > last);
        }
        return new Rates(hours, chestsPerHour, treasuresPerHour, since, any);
    }
}
