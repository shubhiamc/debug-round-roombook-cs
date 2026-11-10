using RoomBook;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Store>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<PricingService>();
builder.Services.AddSingleton<BookingService>();
builder.Services.AddSingleton<ReportService>();

var app = builder.Build();

app.UseExceptionHandler(errors => errors.Run(async ctx =>
{
    ctx.Response.StatusCode = 500;
    await ctx.Response.WriteAsJsonAsync(new { error = "internal server error" });
}));
app.UseDefaultFiles();
app.UseStaticFiles();

static IResult Send(Result r) =>
    r.Body is null ? Results.StatusCode(r.Status) : Results.Json(r.Body, statusCode: r.Status);

// Runs the handler for the signed-in user, or answers 401.
static IResult Authed(AuthService auth, HttpRequest req, Func<User, string, Result> handler)
{
    var (user, token) = auth.Authenticate(req);
    return user is null
        ? Results.Json(new { error = "unauthorized" }, statusCode: 401)
        : Send(handler(user, token));
}

app.MapPost("/register", (AuthService auth, RegisterRequest body) => Send(auth.Register(body)));
app.MapPost("/login", (AuthService auth, LoginRequest body) => Send(auth.Login(body)));
app.MapPost("/logout", (AuthService auth, HttpRequest req) =>
    Authed(auth, req, (_, token) => auth.Logout(token)));

app.MapGet("/rooms", (AuthService auth, Store store, HttpRequest req) =>
    Authed(auth, req, (_, _) => new Result(200, store.Rooms)));

app.MapPost("/bookings", (AuthService auth, BookingService bookings, HttpRequest req, CreateBookingRequest body) =>
    Authed(auth, req, (user, _) => bookings.Create(user.Id, body)));

app.MapGet("/bookings", (AuthService auth, BookingService bookings, HttpRequest req, string? date) =>
    Authed(auth, req, (user, _) => bookings.ListForUser(user.Id, date)));

app.MapGet("/bookings/{id:int}", (AuthService auth, BookingService bookings, HttpRequest req, int id) =>
    Authed(auth, req, (user, _) => bookings.Get(user.Id, id)));

app.MapDelete("/bookings/{id:int}", (AuthService auth, BookingService bookings, HttpRequest req, int id) =>
    Authed(auth, req, (user, _) => bookings.Cancel(user.Id, id)));

app.MapGet("/reports/monthly", (AuthService auth, ReportService reports, HttpRequest req, string? month) =>
    Authed(auth, req, (user, _) => reports.Monthly(user.Id, month)));

app.Run();

public partial class Program { }
