using System.Net.Http.Json;

namespace RoomBook.Tests;

public class ReportTests(ApiFixture fx) : ApiTestBase(fx)
{
    private const int Orchid = 1;
    private const int Maple = 2;

    private static async Task<ReportDto> Monthly(HttpClient client, string month)
    {
        var response = await client.GetAsync($"/reports/monthly?month={month}");
        Expect.Status(response, 200, $"monthly report for {month}");
        return (await response.Content.ReadFromJsonAsync<ReportDto>())!;
    }

    [Fact]
    public async Task Monthly_SummarisesBookingsHoursAndSpend()
    {
        var client = await SignedIn("gia@example.com");
        await BookOk(client, Maple, At(4, 14), At(4, 16));   // 2h x 60 = 120
        await BookOk(client, Orchid, At(5, 14), At(5, 15));  // 1h x 40 = 40

        var report = await Monthly(client, "2030-03");
        Expect.Equal(2, report.Bookings, "bookings in 2030-03");
        Expect.Equal(3.0, report.Hours, "hours booked in 2030-03");
        Expect.Equal(160m, report.TotalSpent, "total spent in 2030-03");
        Expect.Equal(2, report.ByRoom.Count, "rooms used in 2030-03");
    }

    [Fact]
    public async Task Monthly_IncludesBookingsOnTheLastDayOfTheMonth()
    {
        var client = await SignedIn("hugo@example.com");
        await BookOk(client, Maple, At(1, 14), At(1, 15));
        await BookOk(client, Maple, At(31, 14), At(31, 15));

        var report = await Monthly(client, "2030-03");
        Expect.Equal(2, report.Bookings, "bookings in 2030-03 (one on the 1st, one on the 31st)");
    }

    [Fact]
    public async Task Monthly_LeavesOutOtherMonthsAndCancelledBookings()
    {
        var client = await SignedIn("ida@example.com");
        await BookOk(client, Maple, At(28, 14, month: 2), At(28, 15, month: 2));
        await BookOk(client, Maple, At(1, 14, month: 4), At(1, 15, month: 4));
        var cancelled = await BookOk(client, Maple, At(10, 14), At(10, 15));
        await BookOk(client, Maple, At(11, 14), At(11, 15));
        await client.DeleteAsync($"/bookings/{cancelled.Id}");

        var report = await Monthly(client, "2030-03");
        Expect.Equal(1, report.Bookings, "bookings in 2030-03 (February, April and cancelled ones excluded)");
    }

    [Fact]
    public async Task Monthly_ShowsNewBookingsMadeAfterTheReportWasOpened()
    {
        var client = await SignedIn("jack@example.com");
        await BookOk(client, Maple, At(4, 14), At(4, 15));
        var first = await Monthly(client, "2030-03");
        Expect.Equal(1, first.Bookings, "bookings in 2030-03 on first view");

        await BookOk(client, Maple, At(5, 14), At(5, 15));
        var second = await Monthly(client, "2030-03");
        Expect.Equal(2, second.Bookings, "bookings in 2030-03 after booking another room slot");
    }

    [Fact]
    public async Task Monthly_DropsBookingsCancelledAfterTheReportWasOpened()
    {
        var client = await SignedIn("kai@example.com");
        var booking = await BookOk(client, Maple, At(4, 14), At(4, 15));
        await BookOk(client, Maple, At(5, 14), At(5, 15));
        var first = await Monthly(client, "2030-03");
        Expect.Equal(2, first.Bookings, "bookings in 2030-03 on first view");

        await client.DeleteAsync($"/bookings/{booking.Id}");
        var second = await Monthly(client, "2030-03");
        Expect.Equal(1, second.Bookings, "bookings in 2030-03 after cancelling one");
    }

    [Fact]
    public async Task Monthly_WithMalformedMonth_IsRejected()
    {
        var client = await SignedIn("lena@example.com");
        var response = await client.GetAsync("/reports/monthly?month=march");
        Expect.Status(response, 400, "monthly report with month=march");
    }
}
