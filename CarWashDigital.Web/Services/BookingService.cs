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
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var free = await _slotService.GetFreeCapacityAsync(input.Date, input.TimeSlot, input.RescheduleId);
        if (free < 1)
            return new BookingResult(false, null, "Ese horario acaba de ocuparse o no está disponible. Elige otro.");

        if (input.RescheduleId.HasValue)
        {
            var existing = await _db.Bookings.Include(b => b.History)
                .FirstOrDefaultAsync(b => b.Id == input.RescheduleId.Value && b.UserId == userId);

            if (existing == null || existing.Status != "Confirmada")
                return new BookingResult(false, null, "No es posible reprogramar esta reserva.");

            existing.Date = input.Date;
            existing.TimeSlot = input.TimeSlot;
            existing.History.Add(new BookingHistory
            {
                Status = "Reprogramada",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = actorName
            });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return new BookingResult(true, existing.Id);
        }

        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == input.ServiceId && s.IsActive);
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == input.VehicleId && v.UserId == userId && v.IsActive);

        if (service == null || vehicle == null)
            return new BookingResult(false, null, "Servicio o vehículo inválido.");

        var catKey = CarWashDigital.Web.Controllers.BookingController.ResolveCategoryKey(service, vehicle.Category);
        if (catKey == null || !service.PricesByCategory.TryGetValue(catKey, out var price))
            return new BookingResult(false, null, "No hay tarifa configurada para la categoría de tu vehículo.");

        var code = await GenerateUniqueCodeAsync();
        var booking = new Booking
        {
            BookingCode = code,
            UserId = userId,
            Date = input.Date,
            TimeSlot = input.TimeSlot,
            ServiceId = service.Id,
            ServiceName = service.Name,
            ServiceIncludes = new List<string>(service.Includes),
            DurationMinutes = service.DurationMinutes,
            VehicleId = vehicle.Id,
            VehicleBrand = vehicle.Brand,
            VehicleModel = vehicle.Model,
            VehicleCategory = vehicle.Category,
            VehiclePlate = vehicle.Plate,
            Price = price,
            Status = "Confirmada",
            CreatedAt = DateTime.UtcNow,
            History = new List<BookingHistory>
            {
                new() { Status = "Confirmada", ChangedAt = DateTime.UtcNow, ChangedBy = actorName }
            }
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new BookingResult(true, booking.Id);
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
        var booking = await _db.Bookings.Include(b => b.History).FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return false;

        var next = GetNextStatus(booking.Status);
        if (next == null) return false;

        booking.Status = next;
        booking.History.Add(new BookingHistory
        {
            Status = next,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = actorName
        });

        await _db.SaveChangesAsync();
        return true;
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
