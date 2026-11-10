using System.Security.Cryptography;
using System.Text;

namespace RoomBook;

public record RegisterRequest(string? Email, string? Name, string? Password);
public record LoginRequest(string? Email, string? Password);

public class AuthService(Store store)
{
    public Result Register(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            return new(400, new { error = "a valid email is required" });
        if (string.IsNullOrWhiteSpace(req.Name))
            return new(400, new { error = "name is required" });
        if (req.Password is null || req.Password.Length < 8)
            return new(400, new { error = "password must be at least 8 characters" });

        var email = req.Email.Trim().ToLowerInvariant();
        if (store.Users.Any(u => u.Email == email))
            return new(409, new { error = "email already registered" });

        var user = new User(Guid.NewGuid().ToString("N"), email, req.Name.Trim(), Hash(req.Password));
        store.Users.Add(user);
        return new(201, new { user.Id, user.Email, user.Name });
    }

    public Result Login(LoginRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var user = store.Users.FirstOrDefault(u => u.Email == email);
        if (user is null || user.PasswordHash != Hash(req.Password ?? ""))
            return new(401, new { error = "invalid email or password" });

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        store.Tokens[token] = user.Id;
        return new(200, new { token });
    }

    public Result Logout(string token)
    {
        store.Tokens.Remove(token.ToLowerInvariant());
        return new(204);
    }

    /// <summary>Resolves the bearer token on the request to a user, or null.</summary>
    public (User? User, string Token) Authenticate(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return (null, "");

        var token = header["Bearer ".Length..].Trim();
        if (!store.Tokens.TryGetValue(token, out var userId))
            return (null, token);

        return (store.Users.FirstOrDefault(u => u.Id == userId), token);
    }

    private static string Hash(string password) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
}
