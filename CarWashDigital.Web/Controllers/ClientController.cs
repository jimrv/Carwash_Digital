using System.Security.Claims;
using CarWashDigital.Web.Data;
using CarWashDigital.Web.Models.Entities;
using CarWashDigital.Web.Models.ViewModels;
using CarWashDigital.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarWashDigital.Web.Controllers;

[Authorize, Route("cliente")]
public class ClientController : Controller
{
    private readonly CarWashDbContext _db;
    private readonly IBookingService _bookingService;
    private readonly PasswordHasher<User> _hasher = new();

    public ClientController(CarWashDbContext db, IBookingService bookingService)
    {
        _db = db;
        _bookingService = bookingService;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;
        var user = await _db.Users.FirstAsync(u => u.Id == userId);
        var mine = await _db.Bookings
            .Include(b => b.History)
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.Date).ThenBy(b => b.TimeSlot)
            .ToListAsync();

        var next = mine.FirstOrDefault(b => b.Status != "Cancelada" && b.Status != "Entregada");
        var completed = mine.Where(b => b.Status == "Entregada").OrderByDescending(b => b.Date).ToList();

        return View(new ClientDashboardVm
        {
            User = user,
            NextBooking = next,
            CompletedBookings = completed
        });
    }

    [HttpGet("reservas")]
    public async Task<IActionResult> Bookings(string filter = "prox")
    {
        var userId = CurrentUserId;
        var mine = await _db.Bookings
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.Date).ThenBy(b => b.TimeSlot)
            .ToListAsync();

        var filtered = filter switch
        {
            "comp" => mine.Where(b => b.Status == "Entregada").ToList(),
            "canc" => mine.Where(b => b.Status == "Cancelada").ToList(),
            _ => mine.Where(b => b.Status != "Cancelada" && b.Status != "Entregada").ToList()
        };

        ViewBag.Filter = filter;
        return View(filtered);
    }

    [HttpGet("reservas/{id:guid}")]
    public async Task<IActionResult> BookingDetail(Guid id)
    {
        var userId = CurrentUserId;
        var booking = await _db.Bookings
            .Include(b => b.History)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (booking == null) return NotFound();
        return View(booking);
    }

    [HttpPost("reservas/{id:guid}/reprogramar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(Guid id)
    {
        var userId = CurrentUserId;
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (booking == null || booking.Status != "Confirmada")
        {
            TempData["Error"] = "Solo se pueden reprogramar reservas confirmadas.";
            return RedirectToAction(nameof(BookingDetail), new { id });
        }

        return RedirectToAction("ScheduleStep", "Booking", new
        {
            serviceId = booking.ServiceId,
            vehicleId = booking.VehicleId,
            date = booking.Date,
            rescheduleId = booking.Id
        });
    }

    [HttpPost("reservas/{id:guid}/cancelar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = CurrentUserId;
        var ok = await _bookingService.CancelAsync(id, userId, User.Identity?.Name ?? "Cliente");
        TempData[ok ? "Success" : "Error"] = ok ? "Reserva cancelada. Se liberó el cupo." : "No se pudo cancelar la reserva.";
        return RedirectToAction(nameof(BookingDetail), new { id });
    }

    [HttpGet("vehiculos")]
    public async Task<IActionResult> Vehicles()
    {
        var userId = CurrentUserId;
        var vehicles = await _db.Vehicles
            .Where(v => v.UserId == userId && v.IsActive)
            .OrderBy(v => v.Brand).ThenBy(v => v.Model)
            .ToListAsync();
        return View(vehicles);
    }

    [HttpPost("vehiculos/{id:guid}/eliminar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        var userId = CurrentUserId;
        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (v != null)
        {
            v.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Vehículo eliminado. Tu historial se conserva.";
        }
        return RedirectToAction(nameof(Vehicles));
    }

    [HttpGet("perfil")]
    public async Task<IActionResult> Profile()
    {
        var user = await _db.Users.FirstAsync(u => u.Id == CurrentUserId);
        return View(user);
    }

    [HttpPost("perfil"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string name, string? phone, string? companyName, string? ruc, string? newPassword)
    {
        var user = await _db.Users.FirstAsync(u => u.Id == CurrentUserId);
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "El nombre es obligatorio.";
            return View(user);
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (newPassword.Length < 8)
            {
                TempData["Error"] = "La contraseña debe tener al menos 8 caracteres.";
                return View(user);
            }
            user.PasswordHash = _hasher.HashPassword(user, newPassword);
        }

        user.Name = name.Trim();
        user.Phone = phone?.Trim() ?? string.Empty;
        user.CompanyName = string.IsNullOrWhiteSpace(companyName) ? null : companyName.Trim();
        user.Ruc = string.IsNullOrWhiteSpace(ruc) ? null : ruc.Trim();

        await _db.SaveChangesAsync();
        TempData["Success"] = "Perfil guardado.";
        return RedirectToAction(nameof(Profile));
    }
}
