using CarWashDigital.Web.Data;
using CarWashDigital.Web.Models.Entities;
using CarWashDigital.Web.Models.ViewModels;
using CarWashDigital.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarWashDigital.Web.Controllers;

[Authorize(Roles = "Admin"), Route("admin")]
public class AdminController : Controller
{
    private readonly CarWashDbContext _db;
    private readonly ISlotService _slotService;
    private readonly IBookingService _bookingService;

    public AdminController(CarWashDbContext db, ISlotService slotService, IBookingService bookingService)
    {
        _db = db;
        _slotService = slotService;
        _bookingService = bookingService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var today = _slotService.GetLimaToday();
        var nowHour = _slotService.GetLimaNow().Hour;
        var all = await _db.Bookings.Include(b => b.User).ToListAsync();
        var todaySlots = await _slotService.GetSlotsAsync(today);

        var topServices = all
            .GroupBy(b => b.ServiceName)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        var vm = new AdminDashboardVm
        {
            TodayBookingsCount = all.Count(b => b.Date == today && b.Status != "Cancelada"),
            PendingEntryCount = all.Count(b => b.Date == today && b.Status == "Confirmada"),
            InProgressCount = all.Count(b => b.Status == "Ingresado" || b.Status == "En lavado" || b.Status == "En secado"),
            ReadyForPickupCount = all.Count(b => b.Status == "Listo para recoger"),
            FreeSlotsTodayCount = todaySlots.Where(s => !s.IsPast).Sum(s => s.FreeCapacity),
            CancelledCount = all.Count(b => b.Status == "Cancelada"),
            UpcomingBookings = all.Where(b => b.Status == "Confirmada")
                .OrderBy(b => b.Date).ThenBy(b => b.TimeSlot)
                .Take(8).ToList(),
            TopServices = topServices
        };

        return View(vm);
    }

    [HttpGet("reservas")]
    public async Task<IActionResult> Bookings(string? q = null, string? status = null)
    {
        var query = _db.Bookings.Include(b => b.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(b => b.Status == status);

        var list = await query.OrderByDescending(b => b.Date).ThenByDescending(b => b.TimeSlot).ToListAsync();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            list = list.Where(b =>
                b.BookingCode.ToLowerInvariant().Contains(term) ||
                b.VehiclePlate.ToLowerInvariant().Contains(term) ||
                (b.User?.Name ?? "").ToLowerInvariant().Contains(term) ||
                (b.User?.CompanyName ?? "").ToLowerInvariant().Contains(term)
            ).ToList();
        }

        ViewBag.Query = q;
        ViewBag.StatusFilter = status;
        return View(list);
    }

    [HttpGet("reservas/{id:guid}")]
    public async Task<IActionResult> BookingDetail(Guid id)
    {
        var booking = await _db.Bookings
            .Include(b => b.User)
            .Include(b => b.History)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null) return NotFound();

        ViewBag.NextStatus = _bookingService.GetNextStatus(booking.Status);
        return View(booking);
    }

    [HttpPost("reservas/{id:guid}/avanzar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AdvanceStatus(Guid id)
    {
        var ok = await _bookingService.AdvanceStatusAsync(id, User.Identity?.Name ?? "Admin");
        TempData[ok ? "Success" : "Error"] = ok ? "Estado actualizado." : "No se pudo avanzar el estado.";
        return RedirectToAction(nameof(BookingDetail), new { id });
    }

    [HttpPost("reservas/{id:guid}/observacion"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveNotes(Guid id, string? internalNotes)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id);
        if (booking != null)
        {
            booking.InternalNotes = internalNotes ?? string.Empty;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Observación guardada.";
        }
        return RedirectToAction(nameof(BookingDetail), new { id });
    }

    [HttpGet("calendario")]
    public async Task<IActionResult> Calendar(DateOnly? date = null)
    {
        var selected = date ?? _slotService.GetLimaToday();
        var cfg = await _db.BusinessConfigs.FirstAsync(c => c.Id == 1);
        var slots = await _slotService.GetSlotsAsync(selected);
        var dayBookings = await _db.Bookings
            .Include(b => b.User)
            .Where(b => b.Date == selected && b.Status != "Cancelada")
            .ToListAsync();

        var rows = slots.Select(s =>
        {
            var slotBk = dayBookings.Where(b => b.TimeSlot == s.Time).ToList();
            var used = slotBk.Count;
            return new CalendarSlotRowVm(s.Time, used, Math.Max(0, cfg.CapacityPerSlot - used), slotBk);
        }).ToList();

        return View(new AdminCalendarVm
        {
            SelectedDate = selected,
            CapacityPerSlot = cfg.CapacityPerSlot,
            Rows = rows
        });
    }

    [HttpGet("operaciones")]
    public async Task<IActionResult> Operations()
    {
        var today = _slotService.GetLimaToday();
        var bookings = await _db.Bookings
            .Where(b => b.Status != "Cancelada" && b.Status != "Entregada" && (b.Status != "Confirmada" || b.Date <= today))
            .OrderBy(b => b.Date).ThenBy(b => b.TimeSlot)
            .ToListAsync();
        return View(bookings);
    }

    [HttpPost("operaciones/{id:guid}/avanzar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AdvanceFromBoard(Guid id)
    {
        var ok = await _bookingService.AdvanceStatusAsync(id, User.Identity?.Name ?? "Admin");
        TempData[ok ? "Success" : "Error"] = ok ? "Vehículo actualizado en tablero." : "No se pudo actualizar.";
        return RedirectToAction(nameof(Operations));
    }

    [HttpGet("servicios")]
    public async Task<IActionResult> Services()
    {
        var list = await _db.Services.OrderBy(s => s.Code).ToListAsync();
        return View(list);
    }

    [HttpPost("servicios/guardar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveService(Guid id, string name, string description, string includesText, int durationMinutes, decimal? sedanPrice, decimal? suvPrice, decimal? pickupPrice)
    {
        var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == id);
        if (svc == null) return NotFound();

        svc.Name = name.Trim();
        svc.Description = description.Trim();
        svc.Includes = includesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        svc.DurationMinutes = Math.Max(10, durationMinutes);

        var prices = new Dictionary<string, decimal>();
        if (sedanPrice.HasValue && sedanPrice.Value >= 0) prices["Sedán"] = sedanPrice.Value;
        if (suvPrice.HasValue && suvPrice.Value >= 0) prices["SUV"] = suvPrice.Value;
        if (pickupPrice.HasValue && pickupPrice.Value >= 0) prices["Pick-up 4x4"] = pickupPrice.Value;
        svc.PricesByCategory = prices;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Servicio guardado.";
        return RedirectToAction(nameof(Services));
    }

    [HttpPost("servicios/{id:guid}/toggle"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleService(Guid id)
    {
        var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == id);
        if (svc != null)
        {
            svc.IsActive = !svc.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = svc.IsActive ? "Servicio activado." : "Servicio desactivado.";
        }
        return RedirectToAction(nameof(Services));
    }

    [HttpPost("servicios/agregar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AddService()
    {
        var code = "s" + Guid.NewGuid().ToString("N")[..6];
        _db.Services.Add(new ServiceItem
        {
            Code = code,
            Name = "Nuevo servicio",
            Description = "Descripción",
            Includes = new List<string> { "Lavado" },
            DurationMinutes = 30,
            IsActive = false,
            PricesByCategory = new Dictionary<string, decimal>()
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Nuevo servicio creado en borrador.";
        return RedirectToAction(nameof(Services));
    }

    [HttpGet("clientes")]
    public async Task<IActionResult> Clients()
    {
        var clients = await _db.Users.Where(u => u.Role == "Client").OrderBy(u => u.Name).ToListAsync();
        var vehicles = await _db.Vehicles.Where(v => v.IsActive).ToListAsync();
        var bookings = await _db.Bookings.ToListAsync();

        var rows = clients.Select(u => new AdminClientRowVm
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Phone = u.Phone,
            AccountType = u.AccountType,
            CompanyName = u.CompanyName,
            VehiclesCount = vehicles.Count(v => v.UserId == u.Id),
            CompletedCount = bookings.Count(b => b.UserId == u.Id && b.Status == "Entregada"),
            CancelledCount = bookings.Count(b => b.UserId == u.Id && b.Status == "Cancelada")
        }).ToList();

        return View(rows);
    }

    [HttpGet("config")]
    public async Task<IActionResult> Config()
    {
        var cfg = await _db.BusinessConfigs.FirstAsync(c => c.Id == 1);
        return View(cfg);
    }

    [HttpPost("config"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveConfig(int openHour, int closeHour, int capacityPerSlot, int[]? openDays, string? closedDatesText)
    {
        var cfg = await _db.BusinessConfigs.FirstAsync(c => c.Id == 1);
        if (closeHour <= openHour)
        {
            TempData["Error"] = "El cierre debe ser posterior a la apertura.";
            return View("Config", cfg);
        }

        cfg.OpenHour = Math.Clamp(openHour, 0, 23);
        cfg.CloseHour = Math.Clamp(closeHour, 1, 24);
        cfg.CapacityPerSlot = Math.Max(1, capacityPerSlot);
        cfg.OpenDays = (openDays ?? Array.Empty<int>()).Distinct().OrderBy(x => x).ToList();
        cfg.ClosedDates = (closedDatesText ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();

        await _db.SaveChangesAsync();
        TempData["Success"] = "Configuración guardada.";
        return RedirectToAction(nameof(Config));
    }
}
