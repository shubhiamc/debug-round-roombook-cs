namespace RoomBook;

public class PricingService(Store store)
{
    public const decimal PeakSurcharge = 0.25m;     // +25% on peak half-hours
    public const decimal LoyaltyDiscount = 0.10m;   // -10% for returning customers
    public const int LoyaltyThreshold = 5;          // active bookings needed before the discount applies
    public const int PeakStartHour = 9;             // peak = 09:00 up to (not including) 12:00
    public const int PeakEndHour = 12;

    public static bool IsPeak(DateTime t) => t.Hour >= PeakStartHour && t.Hour < PeakEndHour;

    /// <summary>Price for booking the room from start to end, priced per half hour.</summary>
    public decimal Quote(string userId, Room room, DateTime start, DateTime end)
    {
        decimal total = 0;
        for (var t = start; t < end; t = t.AddMinutes(30))
        {
            var slot = room.HourlyRate / 2;
            if (IsPeak(t))
                slot *= 1 + PeakSurcharge;
            total += slot;
        }

        var activeBookings = store.Bookings.Count(b => b.UserId == userId && !b.Cancelled);
        if (activeBookings >= LoyaltyThreshold)
            total *= 1 - LoyaltyDiscount;

        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }
}
