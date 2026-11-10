using System.Net.Http.Json;

namespace RoomBook.Tests;

public class RoomTests(ApiFixture fx) : ApiTestBase(fx)
{
    [Fact]
    public async Task ListRooms_ReturnsTheSeededRoomsWithCapacityAndRate()
    {
        var client = await SignedIn("fay@example.com");
        var response = await client.GetAsync("/rooms");
        Expect.Status(response, 200, "list rooms");

        var rooms = (await response.Content.ReadFromJsonAsync<List<RoomDto>>())!;
        Expect.Equal(3, rooms.Count, "number of rooms");

        var maple = rooms.Single(r => r.Name == "Maple");
        Expect.Equal(8, maple.Capacity, "Maple capacity");
        Expect.Equal(60m, maple.HourlyRate, "Maple hourly rate");
    }
}
