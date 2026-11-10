using System.Globalization;

namespace RoomBook;

public record RoomUsage(int RoomId, string Room, int Bookings, double Hours);
public record UsageReport(string Month, int Bookings, double Hours, decimal TotalSpent, List<RoomUsage> ByRoom);

public class ReportService(Store store)
{
    private readonly Dictionary<string, UsageReport> _cache = new();

    /// <summary>Usage summary for one user and one month (yyyy-MM), based on booking start time.</summary>
    public Result Monthly(string userId, string? month)
    {
        if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
            return new(400, new { error = "month must be formatted yyyy-MM" });

        var cacheKey = $"{userId}:{month}";
        if (_cache.TryGetValue(cacheKey, out var cached))
            return new(200, cached);

        var to = new DateTime(from.Year, from.Month, DateTime.DaysInMonth(from.Year, from.Month));

        var mine = store.Bookings
            .Where(b => b.UserId == userId && !b.Cancelled && b.Start >= from && b.Start <= to)
            .ToList();

        var byRoom = mine
            .GroupBy(b => b.RoomId)
            .Select(g => new RoomUsage(
                g.Key,
                store.Rooms.First(r => r.Id == g.Key).Name,
                g.Count(),
                g.Sum(b => (b.End - b.Start).TotalHours)))
            .OrderBy(r => r.RoomId)
            .ToList();

        var report = new UsageReport(
            month!,
            mine.Count,
            mine.Sum(b => (b.End - b.Start).TotalHours),
            mine.Sum(b => b.Price),
            byRoom);

        _cache[cacheKey] = report;
        return new(200, report);
    }
}
