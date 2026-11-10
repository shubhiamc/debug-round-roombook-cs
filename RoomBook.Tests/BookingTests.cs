using System.Net.Http.Json;

namespace RoomBook.Tests;

public class BookingTests(ApiFixture fx) : ApiTestBase(fx)
{
    private const int Maple = 2;
    private const int Atlas = 3;

    [Fact]
    public async Task Create_ValidBooking_ReturnsCreatedBooking()
    {
        var client = await SignedIn("gus@example.com");
        var response = await Book(client, Maple, At(4, 14), At(4, 15), attendees: 5);
        Expect.Status(response, 201, "create booking");

        var booking = (await response.Content.ReadFromJsonAsync<BookingDto>())!;
        Expect.Equal(Maple, booking.RoomId, "booking room");
        Expect.Equal(5, booking.Attendees, "booking attendees");
        Expect.Equal(At(4, 14), booking.Start, "booking start");
    }

    [Fact]
    public async Task Create_ForUnknownRoom_ReturnsNotFound()
    {
        var client = await SignedIn("hal@example.com");
        var response = await Book(client, roomId: 99, At(4, 14), At(4, 15));
        Expect.Status(response, 404, "book a room that does not exist");
    }

    [Fact]
    public async Task Create_WithMoreAttendeesThanCapacity_IsRejected()
    {
        var client = await SignedIn("ivy@example.com");
        var response = await Book(client, Maple, At(4, 14), At(4, 15), attendees: 9);
        Expect.Status(response, 400, "book Maple (capacity 8) for 9 people");
    }

    [Fact]
    public async Task Create_ExactlyTheSameSlot_ReturnsConflict()
    {
        var client = await SignedIn("jo@example.com");
        await BookOk(client, Maple, At(4, 14), At(4, 15));

        var other = await SignedIn("kim@example.com");
        var response = await Book(other, Maple, At(4, 14), At(4, 15));
        Expect.Status(response, 409, "book a slot that is already taken");
    }

    [Fact]
    public async Task Create_RunningIntoTheStartOfAnExistingBooking_ReturnsConflict()
    {
        var client = await SignedIn("lou@example.com");
        await BookOk(client, Maple, At(4, 14), At(4, 16));

        var response = await Book(client, Maple, At(4, 13, 30), At(4, 14, 30));
        Expect.Status(response, 409, "book 13:30-14:30 when 14:00-16:00 is taken");
    }

    [Fact]
    public async Task Create_AroundAnExistingBooking_ReturnsConflict()
    {
        var client = await SignedIn("max@example.com");
        await BookOk(client, Maple, At(4, 14), At(4, 15));

        var response = await Book(client, Maple, At(4, 13), At(4, 17));
        Expect.Status(response, 409, "book 13:00-17:00 when 14:00-15:00 is taken");
    }

    [Fact]
    public async Task Create_BackToBackBookings_AreBothAccepted()
    {
        var client = await SignedIn("nia@example.com");
        await BookOk(client, Maple, At(4, 14), At(4, 15));

        var response = await Book(client, Maple, At(4, 15), At(4, 16));
        Expect.Status(response, 201, "book 15:00-16:00 right after a 14:00-15:00 booking");
    }

    [Fact]
    public async Task Cancel_OwnBooking_Succeeds()
    {
        var client = await SignedIn("oz@example.com");
        var booking = await BookOk(client, Maple, At(4, 14), At(4, 15));

        var response = await client.DeleteAsync($"/bookings/{booking.Id}");
        Expect.Status(response, 204, "cancel own booking");

        var mine = await MyBookings(client);
        Expect.Equal(0, mine.Count, "my bookings after cancelling the only one");
    }

    [Fact]
    public async Task Cancel_AnotherUsersBooking_IsForbidden()
    {
        var owner = await SignedIn("pat@example.com");
        var booking = await BookOk(owner, Atlas, At(5, 10), At(5, 12), attendees: 12);

        var intruder = await SignedIn("quinn@example.com");
        var response = await intruder.DeleteAsync($"/bookings/{booking.Id}");
        Expect.Status(response, 403, "cancel a booking that belongs to someone else");
    }

    [Fact]
    public async Task Cancel_AlreadyCancelledBooking_ReturnsConflict()
    {
        var client = await SignedIn("ray@example.com");
        var booking = await BookOk(client, Maple, At(4, 14), At(4, 15));
        await client.DeleteAsync($"/bookings/{booking.Id}");

        var response = await client.DeleteAsync($"/bookings/{booking.Id}");
        Expect.Status(response, 409, "cancel a booking twice");
    }

    [Fact]
    public async Task Cancel_UnknownBooking_ReturnsNotFound()
    {
        var client = await SignedIn("sue@example.com");
        var response = await client.DeleteAsync("/bookings/12345");
        Expect.Status(response, 404, "cancel a booking that does not exist");
    }

    [Fact]
    public async Task MyBookings_ListsOnlyMyOwnBookingsInTimeOrder()
    {
        var me = await SignedIn("tom@example.com");
        var someoneElse = await SignedIn("uma@example.com");
        await BookOk(me, Maple, At(6, 15), At(6, 16));
        await BookOk(me, Maple, At(4, 15), At(4, 16));
        await BookOk(someoneElse, Maple, At(5, 15), At(5, 16));

        var mine = await MyBookings(me);
        Expect.Equal(2, mine.Count, "number of my bookings");
        Expect.Equal(4, mine[0].Start.Day, "first booking in the list is the earliest");
    }

    [Fact]
    public async Task MyBookings_WithDateFilter_ReturnsThatDayOnly()
    {
        var client = await SignedIn("val@example.com");
        await BookOk(client, Maple, At(4, 15), At(4, 16));
        await BookOk(client, Maple, At(5, 15), At(5, 16));

        var mine = await MyBookings(client, "?date=2030-03-05");
        Expect.Equal(1, mine.Count, "bookings on 2030-03-05");
        Expect.Equal(5, mine[0].Start.Day, "day of the returned booking");
    }

    [Fact]
    public async Task MyBookings_WithMalformedDate_IsRejected()
    {
        var client = await SignedIn("wes@example.com");
        var response = await client.GetAsync("/bookings?date=next-tuesday");
        Expect.Status(response, 400, "list bookings with date=next-tuesday");
    }
}
