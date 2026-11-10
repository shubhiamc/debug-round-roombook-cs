using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RoomBook.Tests;

public class AccountTests(ApiFixture fx) : ApiTestBase(fx)
{
    [Fact]
    public async Task Register_WithValidDetails_CreatesAccount()
    {
        var client = Anonymous();
        var response = await client.PostAsJsonAsync("/register", new { email = "ana@example.com", name = "Ana", password = Password });
        Expect.Status(response, 201, "register new account");

        var body = await response.Content.ReadAsStringAsync();
        Expect.True(!body.Contains(Password), "register response must not echo the password", body);
    }

    [Fact]
    public async Task Register_SameEmailInDifferentCase_IsRejected()
    {
        var client = Anonymous();
        await client.PostAsJsonAsync("/register", new { email = "ana@example.com", name = "Ana", password = Password });
        var again = await client.PostAsJsonAsync("/register", new { email = "ANA@Example.com", name = "Ana 2", password = Password });
        Expect.Status(again, 409, "register an email that already exists");
    }

    [Fact]
    public async Task Register_WithShortPassword_IsRejected()
    {
        var client = Anonymous();
        var response = await client.PostAsJsonAsync("/register", new { email = "bo@example.com", name = "Bo", password = "short" });
        Expect.Status(response, 400, "register with a 5 character password");
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        var client = Anonymous();
        await client.PostAsJsonAsync("/register", new { email = "cy@example.com", name = "Cy", password = Password });
        var response = await client.PostAsJsonAsync("/login", new { email = "cy@example.com", password = Password });
        Expect.Status(response, 200, "login with correct password");

        var token = (await response.Content.ReadFromJsonAsync<TokenDto>())!.Token;
        Expect.True(!string.IsNullOrEmpty(token), "login returns a non-empty token");
    }

    [Fact]
    public async Task Login_WithWrongPassword_IsRejected()
    {
        var client = Anonymous();
        await client.PostAsJsonAsync("/register", new { email = "di@example.com", name = "Di", password = Password });
        var response = await client.PostAsJsonAsync("/login", new { email = "di@example.com", password = "not-the-password" });
        Expect.Status(response, 401, "login with wrong password");
    }

    [Fact]
    public async Task Logout_MakesTheTokenUnusable()
    {
        var client = await SignedIn("ed@example.com");
        Expect.Status(await client.GetAsync("/rooms"), 200, "list rooms before logout");

        var logout = await client.PostAsync("/logout", null);
        Expect.Status(logout, 204, "logout");

        var after = await client.GetAsync("/rooms");
        Expect.Status(after, 401, "list rooms with a token that was logged out");
    }

    [Fact]
    public async Task Rooms_WithoutToken_AreNotAvailable()
    {
        var response = await Anonymous().GetAsync("/rooms");
        Expect.Status(response, 401, "list rooms without a token");
    }
}
