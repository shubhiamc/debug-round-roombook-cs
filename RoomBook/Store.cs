namespace RoomBook;

public record User(string Id, string Email, string Name, string PasswordHash);

public record Room(int Id, string Name, int Capacity, decimal HourlyRate);

public class Booking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string UserId { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int Attendees { get; set; }
    public decimal Price { get; set; }
    public bool Cancelled { get; set; }
}

public record Result(int Status, object? Body = null);

/// <summary>In-memory data store shared by the whole app (registered as a singleton).</summary>
public class Store
{
    public List<User> Users { get; } = new();
    public List<Room> Rooms { get; } = new();
    public List<Booking> Bookings { get; } = new();

    // token -> user id
    public Dictionary<string, string> Tokens { get; } = new();

    private int _nextBookingId = 1;

    public Store() => Reset();

    public void Reset()
    {
        Users.Clear();
        Bookings.Clear();
        Tokens.Clear();
        _nextBookingId = 1;

        Rooms.Clear();
        Rooms.Add(new Room(1, "Orchid", 4, 40m));
        Rooms.Add(new Room(2, "Maple", 8, 60m));
        Rooms.Add(new Room(3, "Atlas", 20, 120m));
    }

    public int NextBookingId() => _nextBookingId++;

    /// <summary>True when no active booking in the room overlaps [start, end).</summary>
    public bool IsRoomFree(int roomId, DateTime start, DateTime end)
    {
        return !Bookings.Any(b =>
            b.RoomId == roomId &&
            !b.Cancelled &&
            b.Start <= start && start < b.End);
    }
}
