using System.Globalization;

namespace RoomBook;

public record CreateBookingRequest(int RoomId, DateTime Start, DateTime End, int Attendees);

public class BookingService(Store store, PricingService pricing)
{
    public Result Create(string userId, CreateBookingRequest req)
    {
        if (req.End <= req.Start)
            return new(400, new { error = "end must be after start" });
        if (req.Start.Minute % 30 != 0 || req.End.Minute % 30 != 0)
            return new(400, new { error = "bookings must start and end on the hour or half hour" });

        var room = store.Rooms.FirstOrDefault(r => r.Id == req.RoomId);
        if (room is null)
            return new(404, new { error = "room not found" });

        if (req.Attendees < 1 || req.Attendees > room.Capacity)
            return new(400, new { error = $"attendees must be between 1 and {room.Capacity}" });

        if (!store.IsRoomFree(room.Id, req.Start, req.End))
            return new(409, new { error = "room is already booked for that time" });

        var booking = new Booking
        {
            Id = store.NextBookingId(),
            RoomId = room.Id,
            UserId = userId,
            Start = req.Start,
            End = req.End,
            Attendees = req.Attendees,
            Price = pricing.Quote(userId, room, req.Start, req.End),
        };
        store.Bookings.Add(booking);
        return new(201, booking);
    }

    public Result Get(string userId, int id)
    {
        var booking = store.Bookings.FirstOrDefault(b => b.Id == id);
        if (booking is null)
            return new(404, new { error = "booking not found" });
        if (booking.UserId != userId)
            return new(403, new { error = "not your booking" });
        return new(200, booking);
    }

    public Result Cancel(string userId, int id)
    {
        var booking = store.Bookings.FirstOrDefault(b => b.Id == id);
        if (booking is null)
            return new(404, new { error = "booking not found" });
        if (booking.UserId != userId)
            return new(403, new { error = "not your booking" });
        if (booking.Cancelled)
            return new(409, new { error = "booking is already cancelled" });

        booking.Cancelled = true;
        return new(204);
    }

    /// <summary>Active bookings for the user, optionally limited to bookings starting on one day (yyyy-MM-dd).</summary>
    public Result ListForUser(string userId, string? date)
    {
        var query = store.Bookings.Where(b => b.UserId == userId && !b.Cancelled);

        if (!string.IsNullOrEmpty(date))
        {
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
                return new(400, new { error = "date must be formatted yyyy-MM-dd" });
            query = query.Where(b => DateOnly.FromDateTime(b.Start) == day);
        }

        return new(200, query.OrderBy(b => b.Start).ToList());
    }
}
