using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RoomBook;

namespace RoomBook.Tests;

public record TokenDto(string Token);
public record RoomDto(int Id, string Name, int Capacity, decimal HourlyRate);
public record BookingDto(int Id, int RoomId, string UserId, DateTime Start, DateTime End, int Attendees, decimal Price, bool Cancelled);
public record RoomUsageDto(int RoomId, string Room, int Bookings, double Hours);
public record ReportDto(string Month, int Bookings, double Hours, decimal TotalSpent, List<RoomUsageDto> ByRoom);

/// <summary>One test server shared by every test; each test class resets the store first.</summary>
public class ApiFixture : IDisposable
{
    public WebApplicationFactory<Program> Factory { get; } = new WebApplicationFactory<Program>();
    public Store Store => Factory.Services.GetRequiredService<Store>();
    public void Dispose() => Factory.Dispose();
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFixture> { }

/// <summary>Assertions that always say what was being checked.</summary>
public static class Expect
{
    public static void Status(HttpResponseMessage response, int expected, string what)
    {
        var actual = (int)response.StatusCode;
        Assert.True(actual == expected, $"{what}: expected {expected}, got {actual}");
    }

    public static void Equal<T>(T expected, T actual, string what)
    {
        Assert.True(EqualityComparer<T>.Default.Equals(expected, actual), $"{what}: expected {expected}, got {actual}");
    }

    public static void True(bool condition, string what, string detail = "")
    {
        Assert.True(condition, $"{what}: expected true, got false {detail}".TrimEnd());
    }
}

[Collection("api")]
public abstract class ApiTestBase
{
    protected const string Password = "correct-horse-1";
    protected readonly ApiFixture Fx;

    protected ApiTestBase(ApiFixture fx)
    {
        Fx = fx;
        fx.Store.Reset();
    }

    protected HttpClient Anonymous() => Fx.Factory.CreateClient();

    /// <summary>Registers (if needed) and logs in, returning a client that sends the bearer token.</summary>
    protected async Task<HttpClient> SignedIn(string email)
    {
        var client = Anonymous();
        await client.PostAsJsonAsync("/register", new { email, name = email.Split('@')[0], password = Password });
        var login = await client.PostAsJsonAsync("/login", new { email, password = Password });
        Expect.Status(login, 200, $"login as {email}");
        var token = (await login.Content.ReadFromJsonAsync<TokenDto>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected static DateTime At(int day, int hour, int minute = 0, int month = 3) => new(2030, month, day, hour, minute, 0);

    protected static Task<HttpResponseMessage> Book(HttpClient client, int roomId, DateTime start, DateTime end, int attendees = 2) =>
        client.PostAsJsonAsync("/bookings", new { roomId, start, end, attendees });

    /// <summary>Books a room and checks the booking was accepted.</summary>
    protected static async Task<BookingDto> BookOk(HttpClient client, int roomId, DateTime start, DateTime end, int attendees = 2)
    {
        var response = await Book(client, roomId, start, end, attendees);
        Expect.Status(response, 201, $"set-up booking of room {roomId} at {start:yyyy-MM-dd HH:mm}");
        return (await response.Content.ReadFromJsonAsync<BookingDto>())!;
    }

    protected static async Task<List<BookingDto>> MyBookings(HttpClient client, string query = "")
    {
        var response = await client.GetAsync("/bookings" + query);
        Expect.Status(response, 200, "list my bookings");
        return (await response.Content.ReadFromJsonAsync<List<BookingDto>>())!;
    }
}
