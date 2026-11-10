# RoomBook debugging exercise: answer key

Line numbers refer to the `main` branch. The `solution` branch contains all fixes (including the two hidden ones).
On `main`, 11 of the 34 tests fail; they trace back to the 8 bugs below. 23 tests pass.

## Bugs that fail tests

| # | Area | File:line | Root cause | Fix | Tests that fail |
|---|------|-----------|------------|-----|-----------------|
| 1 | Booking conflicts | `Store.cs:56` (`IsRoomFree`) | The overlap check only detects a new booking that *starts inside* an existing one (`b.Start <= start && start < b.End`). A new booking that starts earlier and runs into, or wraps around, an existing booking is accepted. | Use the standard interval test: `b.Start < end && start < b.End`. (End-exclusive, so back-to-back bookings still work.) | `BookingTests.Create_RunningIntoTheStartOfAnExistingBooking_ReturnsConflict`, `BookingTests.Create_AroundAnExistingBooking_ReturnsConflict` |
| 2 | Token handling (logout) | `Auth.cs:43` (`Logout`) | Tokens are generated as upper-case hex but logout removes `token.ToLowerInvariant()`, so the dictionary key never matches and the token stays valid after logout. | `store.Tokens.Remove(token);` | `AccountTests.Logout_MakesTheTokenUnusable` |
| 3 | Wrong status code | `BookingService.cs:16` (`Create`) | Booking an unknown room returns 400 instead of 404. | `return new(404, ...)` | `BookingTests.Create_ForUnknownRoom_ReturnsNotFound` |
| 4 | Missing ownership check | `BookingService.cs:46-52` (`Cancel`) | `Cancel` finds the booking by id and cancels it without comparing `booking.UserId` to the caller, so any signed-in user can cancel anyone's booking. | After the 404 check add `if (booking.UserId != userId) return new(403, new { error = "not your booking" });` | `BookingTests.Cancel_AnotherUsersBooking_IsForbidden` |
| 5 | Unhandled exception | `BookingService.cs:65` (`ListForUser`) | `DateOnly.Parse(date)` throws `FormatException` for input like `next-tuesday`, which surfaces as a 500 instead of a client error. | Use `DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)` and return 400 when it fails (needs `using System.Globalization;`). | `BookingTests.MyBookings_WithMalformedDate_IsRejected` |
| 6 | Boundary (end-inclusive vs end-exclusive) | `PricingService.cs:11` (`IsPeak`) | Peak is meant to be 09:00 up to but not including 12:00, but the check is `t.Hour <= PeakEndHour`, so the 12:00 and 12:30 half hours are charged the peak surcharge. | `t.Hour < PeakEndHour` | `PricingTests.Price_StartingAtNoon_IsTheNormalRate`, `PricingTests.Price_RunningPastTheEndOfPeak_OnlyChargesPeakForPeakTime` |
| 7 | Time window | `ReportService.cs:22` and `:25` (`Monthly`) | The month end is `new DateTime(year, month, daysInMonth)`, which is midnight at the *start* of the last day, and the filter is `b.Start <= to`. Every booking on the last day of the month after 00:00 is dropped. | `var to = from.AddMonths(1);` and filter with `b.Start < to`. | `ReportTests.Monthly_IncludesBookingsOnTheLastDayOfTheMonth` |
| 8 | Stale shared state | `ReportService.cs:10`, `:19-20`, `:45` | `ReportService` is a singleton and caches each user+month report in a dictionary that is never invalidated, so later bookings and cancellations do not show up in the report. | Remove the cache (or invalidate it whenever a booking is created or cancelled). | `ReportTests.Monthly_ShowsNewBookingsMadeAfterTheReportWasOpened`, `ReportTests.Monthly_DropsBookingsCancelledAfterTheReportWasOpened` |

## Hidden flaws (no failing test)

| # | File:line | Flaw | Fix |
|---|-----------|------|-----|
| H1 | `BookingService.cs:38-43` (`Get`) | `GET /bookings/{id}` returns any booking to any signed-in user. It has the 404 check but, unlike what `Cancel` should do, no ownership check, so people can read other users' bookings. | Return 403 when `booking.UserId != userId`. |
| H2 | `Auth.cs:61-62` (`Hash`), used at `:24` and `:33` | Passwords are stored as a plain, unsalted SHA-256 hash, so identical passwords give identical hashes and the hashes are cheap to brute-force or look up in rainbow tables. | Use a per-user random salt and a slow KDF (the solution branch uses `Rfc2898DeriveBytes.Pbkdf2` and stores `salt:hash`). |

## Mapping to the reported symptoms in the README

1. Old session still works after logout: bug 2.
2. Two teams booked the same room: bug 1.
3. Someone else cancelled the workshop: bug 4 (H1 is the read-side sibling).
4. Noon bookings charged the peak price: bug 6.
5. Report misses the 31st and does not update: bugs 7 and 8.

Bugs 3 and 5 are not in the README symptoms; they are found from the failing tests.
