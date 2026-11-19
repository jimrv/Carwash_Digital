using System.Security.Claims;
using CarWashDigital.Web.Data;
using CarWashDigital.Web.Models.Entities;
using CarWashDigital.Web.Models.ViewModels;
using CarWashDigital.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarWashDigital.Web.Controllers;

[Route("reservar")]
public class BookingController : Controller
{
    private readonly CarWashDbContext _db;
    private readonly ISlotService _slotService;
    private readonly IBookingService _bookingService;

    public BookingController(CarWashDbContext db, ISlotService slotService, IBookingService bookingService)
    {
        _db = db;
        _slotService = slotService;
        _bookingService = bookingService;
    }

    public static string? ResolveCategoryKey(ServiceItem service, string vehicleCategory)
    {
        if (service.PricesByCategory.ContainsKey(vehicleCategory))
            return vehicleCategory;

        if (service.IsFleetService)
        {
            if (vehicleCategory.Contains("Sedán", StringComparison.OrdinalIgnoreCase))
                return service.PricesByCategory.Keys.FirstOrDefault(k => k.Contains("Sedán", StringComparison.OrdinalIgnoreCase));
            if (vehicleCategory.Contains("SUV", StringComparison.OrdinalIgnoreCase))
                return service.PricesByCategory.Keys.FirstOrDefault(k => k.Contains("SUV", StringComparison.OrdinalIgnoreCase));
            if (vehicleCategory.Contains("Pick-up", StringComparison.OrdinalIgnoreCase))
                return service.PricesByCategory.Keys.FirstOrDefault(k => k.Contains("Pick-up", StringComparison.OrdinalIgnoreCase));
        }
        return null;
    }

    [HttpGet("servicio")]
    public async Task<IActionResult> ServiceStep()
    {
        var services = await _db.Services.Where(s => s.IsActive).OrderBy(s => s.Code).ToListAsync();
        return View(services);
    }

    [Authorize, HttpGet("vehiculo")]
    public async Task<IActionResult> VehicleStep(Guid serviceId, Guid? rescheduleId = null)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive);
        if (service == null)
        {
            TempData["Error"] = "Primero elige un servicio activo.";
            return RedirectToAction(nameof(ServiceStep));
        }

        var userVehicles = await _db.Vehicles
            .Where(v => v.UserId == userId && v.IsActive)
            .OrderBy(v => v.Brand).ThenBy(v => v.Model)
            .ToListAsync();

        var compatibleVehicles = userVehicles
            .Where(v => ResolveCategoryKey(service, v.Category) != null)
            .ToList();

        return View(new VehicleStepVm
        {
            Service = service,
            Vehicles = compatibleVehicles,
            RescheduleId = rescheduleId
        });
    }

    [Authorize, HttpPost("vehiculo/agregar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVehicle(AddVehicleInput input)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Completa los datos obligatorios del vehículo.";
            return input.ReturnToBooking && input.ServiceId.HasValue
                ? RedirectToAction(nameof(VehicleStep), new { serviceId = input.ServiceId })
                : RedirectToAction("Vehicles", "Client");
        }

        var vehicle = new Vehicle
        {
            UserId = userId,
            Brand = input.Brand.Trim(),
            Model = input.Model.Trim(),
            Category = input.Category,
            Plate = input.Plate.Trim().ToUpperInvariant(),
            Color = input.Color?.Trim() ?? string.Empty,
            IsActive = true
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Vehículo guardado.";

        if (input.ReturnToBooking && input.ServiceId.HasValue)
            return RedirectToAction(nameof(ScheduleStep), new { serviceId = input.ServiceId.Value, vehicleId = vehicle.Id });

        return RedirectToAction("Vehicles", "Client");
    }

    [Authorize, HttpGet("horario")]
    public async Task<IActionResult> ScheduleStep(Guid serviceId, Guid vehicleId, DateOnly? date = null, Guid? rescheduleId = null)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId);
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.UserId == userId && v.IsActive);

        if (service == null || vehicle == null)
            return RedirectToAction(nameof(ServiceStep));

        var selectedDate = date ?? _slotService.GetLimaToday();
        var slots = await _slotService.GetSlotsAsync(selectedDate, rescheduleId);

        return View(new ScheduleStepVm
        {
            Service = service,
            Vehicle = vehicle,
            SelectedDate = selectedDate,
            Slots = slots,
            RescheduleId = rescheduleId
        });
    }

    [Authorize, HttpGet("confirmacion")]
    public async Task<IActionResult> ConfirmStep(Guid serviceId, Guid vehicleId, DateOnly date, string timeSlot, Guid? rescheduleId = null)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId);
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.UserId == userId && v.IsActive);
        var customer = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (service == null || vehicle == null || customer == null)
            return RedirectToAction(nameof(ServiceStep));

        var key = ResolveCategoryKey(service, vehicle.Category);
        if (key == null || !service.PricesByCategory.TryGetValue(key, out var price))
            return RedirectToAction(nameof(ServiceStep));

        return View(new ConfirmStepVm
        {
            Service = service,
            Vehicle = vehicle,
            Customer = customer,
            Date = date,
            TimeSlot = timeSlot,
            Price = price,
            RescheduleId = rescheduleId
        });
    }

    [Authorize, HttpPost("confirmar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(ConfirmBookingInput input)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var actorName = User.Identity?.Name ?? "Cliente";

        var result = await _bookingService.CreateOrRescheduleAsync(userId, input, actorName);
        if (!result.Success || !result.BookingId.HasValue)
        {
            TempData["Error"] = result.ErrorMessage ?? "No se pudo confirmar la reserva.";
            return RedirectToAction(nameof(ScheduleStep), new
            {
                serviceId = input.ServiceId,
                vehicleId = input.VehicleId,
                date = input.Date,
                rescheduleId = input.RescheduleId
            });
        }

        TempData["Success"] = input.RescheduleId.HasValue ? "Horario reprogramado correctamente." : "Reserva confirmada.";
        return RedirectToAction("BookingDetail", "Client", new { id = result.BookingId.Value });
    }
}
