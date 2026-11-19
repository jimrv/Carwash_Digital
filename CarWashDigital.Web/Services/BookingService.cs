using System.Data;
using CarWashDigital.Web.Data;
using CarWashDigital.Web.Models.Entities;
using CarWashDigital.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CarWashDigital.Web.Services;

public record BookingResult(bool Success, Guid? BookingId = null, string? ErrorMessage = null);

public interface IBookingService
{
    Task<BookingResult> CreateOrRescheduleAsync(Guid userId, ConfirmBookingInput input, string actorName);
    Task<bool> CancelAsync(Guid bookingId, Guid? restrictToUserId, string actorName);
    Task<bool> AdvanceStatusAsync(Guid bookingId, string actorName);
    string? GetNextStatus(string currentStatus);
}

public class BookingService : IBookingService
{
    public static readonly string[] ActiveFlow = { "Confirmada", "Ingresado", "En lavado", "En secado", "Listo para recoger", "Entregada" };

    private readonly CarWashDbContext _db;
    private readonly ISlotService _slotService;

    public BookingService(CarWashDbContext db, ISlotService slotService)
    {
        _db = db;
        _slotService = slotService;
    }

    public string? GetNextStatus(string currentStatus)
    {
        var idx = Array.IndexOf(ActiveFlow, currentStatus);
        if (idx < 0 || idx >= ActiveFlow.Length - 1) return null;
        return ActiveFlow[idx + 1];
    }

    public async Task<BookingResult> CreateOrRescheduleAsync(Guid userId, ConfirmBookingInput input, string actorName)
    {
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == input.ServiceId && s.IsActive);
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == input.VehicleId && v.UserId == userId && v.IsActive);
        if (service == null || vehicle == null)
            return new BookingResult(false, null, "Servicio o vehículo no válido.");

        var catKey = CarWashDigital.Web.Controllers.BookingController.ResolveCategoryKey(service, vehicle.Category);
        if (catKey == null || !service.PricesByCategory.TryGetValue(catKey, out var price))
            return new BookingResult(false, null, "La categoría del vehículo no aplica para este servicio.");

        var slots = await _slotService.GetSlotsAsync(input.Date, input.RescheduleId);
        var chosenSlot = slots.FirstOrDefault(s => s.Time == input.TimeSlot);
        if (chosenSlot == null || chosenSlot.IsPast || chosenSlot.FreeCapacity <= 0)
            return new BookingResult(false, null, "El horario seleccionado ya no tiene cupos disponibles.");

        try
        {
            if (input.RescheduleId.HasValue)
            {
                var existing = await _db.Bookings
                    .FirstOrDefaultAsync(b => b.Id == input.RescheduleId.Value && b.UserId == userId);

                if (existing == null)
                    return new BookingResult(false, null, "No se encontró la reserva original para reprogramar.");

                if (existing.Status != "Confirmada")
                    return new BookingResult(false, null, "Solo se pueden reprogramar reservas en estado Confirmada.");

                existing.ServiceId = service.Id;
                existing.ServiceName = service.Name;
                existing.ServiceIncludes = service.Includes.ToList();
                existing.DurationMinutes = service.DurationMinutes;
                existing.VehicleId = vehicle.Id;
                existing.VehicleBrand = vehicle.Brand;
                existing.VehicleModel = vehicle.Model;
                existing.VehicleCategory = vehicle.Category;
                existing.VehiclePlate = vehicle.Plate;
                existing.Date = input.Date;
                existing.TimeSlot = input.TimeSlot;
                existing.Price = price;

                _db.BookingHistories.Add(new BookingHistory
                {
                    BookingId = existing.Id,
                    Status = $"Reprogramada ({input.Date:dd/MM/yyyy} {input.TimeSlot})",
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = actorName
                });

                await _db.SaveChangesAsync();
                return new BookingResult(true, existing.Id, null);
            }

            var code = $"CW-{Random.Shared.Next(1000, 9999)}";
            while (await _db.Bookings.AnyAsync(b => b.BookingCode == code))
                code = $"CW-{Random.Shared.Next(1000, 9999)}";

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                BookingCode = code,
                UserId = userId,
                Date = input.Date,
                TimeSlot = input.TimeSlot,
                ServiceId = service.Id,
                ServiceName = service.Name,
                ServiceIncludes = service.Includes.ToList(),
                DurationMinutes = service.DurationMinutes,
                VehicleId = vehicle.Id,
                VehicleBrand = vehicle.Brand,
                VehicleModel = vehicle.Model,
                VehicleCategory = vehicle.Category,
                VehiclePlate = vehicle.Plate,
                Price = price,
                Status = "Confirmada",
                CreatedAt = DateTime.UtcNow
            };

            _db.Bookings.Add(booking);
            _db.BookingHistories.Add(new BookingHistory
            {
                BookingId = booking.Id,
                Status = "Confirmada",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = actorName
            });

            await _db.SaveChangesAsync();
            return new BookingResult(true, booking.Id, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new BookingResult(false, null, "No se pudo guardar el cambio porque la reserva fue modificada recientemente. Recarga e inténtalo de nuevo.");
        }
    }

    public async Task<bool> CancelAsync(Guid bookingId, Guid? restrictToUserId, string actorName)
    {
        var query = _db.Bookings.Include(b => b.History).Where(b => b.Id == bookingId);
        if (restrictToUserId.HasValue)
            query = query.Where(b => b.UserId == restrictToUserId.Value);

        var booking = await query.FirstOrDefaultAsync();
        if (booking == null || booking.Status != "Confirmada")
            return false;

        booking.Status = "Cancelada";
        booking.History.Add(new BookingHistory
        {
            Status = "Cancelada",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = actorName
        });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AdvanceStatusAsync(Guid bookingId, string actorName)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return false;

        var next = GetNextStatus(booking.Status);
        if (string.IsNullOrWhiteSpace(next)) return false;

        try
        {
            booking.Status = next;
            _db.BookingHistories.Add(new BookingHistory
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Status = next,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = actorName
            });

            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var code = new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
            if (!await _db.Bookings.AnyAsync(b => b.BookingCode == code))
                return code;
        }
    }
}
