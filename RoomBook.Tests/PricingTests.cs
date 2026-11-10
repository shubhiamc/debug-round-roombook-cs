namespace RoomBook.Tests;

public class PricingTests(ApiFixture fx) : ApiTestBase(fx)
{
    private const int Maple = 2; // 60 per hour

    [Fact]
    public async Task Price_OutsideBusyHours_IsTheHourlyRate()
    {
        var client = await SignedIn("abe@example.com");
        var booking = await BookOk(client, Maple, At(4, 14), At(4, 15));
        Expect.Equal(60m, booking.Price, "price of 14:00-15:00 in Maple");
    }

    [Fact]
    public async Task Price_DuringMorningPeak_AddsTwentyFivePercent()
    {
        var client = await SignedIn("bea@example.com");
        var booking = await BookOk(client, Maple, At(4, 10), At(4, 11));
        Expect.Equal(75m, booking.Price, "price of 10:00-11:00 in Maple");
    }

    [Fact]
    public async Task Price_StartingAtNoon_IsTheNormalRate()
    {
        var client = await SignedIn("cal@example.com");
        var booking = await BookOk(client, Maple, At(4, 12), At(4, 13));
        Expect.Equal(60m, booking.Price, "price of 12:00-13:00 in Maple");
    }

    [Fact]
    public async Task Price_RunningPastTheEndOfPeak_OnlyChargesPeakForPeakTime()
    {
        var client = await SignedIn("dee@example.com");
        var booking = await BookOk(client, Maple, At(4, 11), At(4, 13));
        Expect.Equal(135m, booking.Price, "price of 11:00-13:00 in Maple (1 peak hour + 1 normal hour)");
    }

    [Fact]
    public async Task Price_SixthBooking_GetsLoyaltyDiscount()
    {
        var client = await SignedIn("eli@example.com");
        for (var day = 1; day <= 5; day++)
            await BookOk(client, Maple, At(day, 14), At(day, 15));

        var sixth = await BookOk(client, Maple, At(6, 14), At(6, 15));
        Expect.Equal(54m, sixth.Price, "price of the sixth booking (10% loyalty discount on 60)");
    }

    [Fact]
    public async Task Price_FifthBooking_IsNotYetDiscounted()
    {
        var client = await SignedIn("flo@example.com");
        for (var day = 1; day <= 4; day++)
            await BookOk(client, Maple, At(day, 14), At(day, 15));

        var fifth = await BookOk(client, Maple, At(5, 14), At(5, 15));
        Expect.Equal(60m, fifth.Price, "price of the fifth booking (no discount yet)");
    }
}
