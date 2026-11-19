using System.Security.Claims;
using CarWashDigital.Web.Data;
using CarWashDigital.Web.Models.Entities;
using CarWashDigital.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarWashDigital.Web.Controllers;

[Route("seguimiento")]
public class TrackingController : Controller
{
    private readonly CarWashDbContext _db;
    public TrackingController(CarWashDbContext db) => _db = db;

    [HttpGet("{code?}")]
    public async Task<IActionResult> Index(string? code)
    {
        var myBookings = new List<BookingOptionVm>();
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idClaim, out var userId))
        {
            myBookings = await _db.Bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.Date).ThenByDescending(b => b.TimeSlot)
                .Select(b => new BookingOptionVm(b.BookingCode, b.Date, b.TimeSlot))
                .ToListAsync();
        }

        Booking? found = null;
        if (!string.IsNullOrWhiteSpace(code))
        {
            var normalized = code.Trim().ToUpperInvariant();
            found = await _db.Bookings
                .Include(b => b.History)
                .FirstOrDefaultAsync(b => b.BookingCode == normalized);
        }

        return View(new TrackingVm
        {
            SearchCode = code?.Trim(),
            FoundBooking = found,
            MyBookings = myBookings
        });
    }
}
