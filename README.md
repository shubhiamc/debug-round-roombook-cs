# RoomBook

RoomBook is a small meeting-room booking service for an office. People register and log in, browse the
rooms (each has a capacity and an hourly rate), book a room for a time slot, see their own bookings
(optionally for one day), cancel them, and pull a monthly usage report. Prices include a peak-hour
surcharge and a loyalty discount for frequent bookers.

It is a .NET 8 ASP.NET Core minimal API with everything held in memory (no database), plus a tiny
browser front end in `RoomBook/wwwroot`.

```
RoomBook/          the API (Program.cs, Auth.cs, Store.cs, BookingService.cs, PricingService.cs, ReportService.cs)
RoomBook/wwwroot/  small web UI served by the API
RoomBook.Tests/    xUnit tests that call the API through WebApplicationFactory
```

## Setup

Requires the .NET 8 SDK.

```bash
dotnet restore
dotnet test
dotnet run --project RoomBook     # then open the URL it prints (http://localhost:5000 or similar)
```

## Reported problems

These came in from customers and the product manager this week:

1. "I logged out on a shared laptop, but someone reopened my old tab and was still able to use my session."
2. "Two teams turned up for the Atlas room on Tuesday morning. The system let both of them book it."
3. "My Friday workshop disappeared from the schedule. Someone else on the team says they cancelled it by mistake, and the app just let them."
4. "Finance says our bookings that start at noon are being charged the busy-morning price."
5. "My monthly usage summary doesn't match what I booked: the meeting I had on the 31st isn't counted, and after I cancel or add a booking the totals don't change."

There are several issues; use the failing tests and the app's behavior to find them.
