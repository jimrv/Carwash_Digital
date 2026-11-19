using CarWashDigital.Web.Data;
using CarWashDigital.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CarWashDigital.Web.Services;

public interface ISlotService
{
    DateTime GetLimaNow();
    DateOnly GetLimaToday();
    Task<List<SlotOptionVm>> GetSlotsAsync(DateOnly date, Guid? ignoreBookingId = null);
    Task<int> GetFreeCapacityAsync(DateOnly date, string timeSlot, Guid? ignoreBookingId = null);
}

public class SlotService : ISlotService
{
    private readonly CarWashDbContext _db;

    public SlotService(CarWashDbContext db) => _db = db;

    public DateTime GetLimaNow()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch
        {
            return DateTime.UtcNow.AddHours(-5);
        }
    }

    public DateOnly GetLimaToday() => DateOnly.FromDateTime(GetLimaNow());

    public async Task<List<SlotOptionVm>> GetSlotsAsync(DateOnly date, Guid? ignoreBookingId = null)
    {
        var cfg = await _db.BusinessConfigs.FirstOrDefaultAsync(c => c.Id == 1);
        var today = GetLimaToday();
        var now = GetLimaNow();

        if (cfg == null || date < today)
            return new List<SlotOptionVm>();

        var dayOfWeek = (int)date.DayOfWeek; // 0=Dom ... 6=Sáb
        var dateIso = date.ToString("yyyy-MM-dd");

        if (!cfg.OpenDays.Contains(dayOfWeek) || cfg.ClosedDates.Contains(dateIso))
            return new List<SlotOptionVm>();

        var usedByTime = await _db.Bookings
            .Where(b => b.Date == date && b.Status != "Cancelada" && (!ignoreBookingId.HasValue || b.Id != ignoreBookingId.Value))
            .GroupBy(b => b.TimeSlot)
            .Select(g => new { TimeSlot = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TimeSlot, x => x.Count);

        var result = new List<SlotOptionVm>();
        for (int h = cfg.OpenHour; h < cfg.CloseHour; h++)
        {
            var slot = $"{h:00}:00";
            usedByTime.TryGetValue(slot, out var used);
            var free = Math.Max(0, cfg.CapacityPerSlot - used);
            var isPast = date == today && h <= now.Hour;
            result.Add(new SlotOptionVm(slot, free, isPast));
        }

        return result;
    }

    public async Task<int> GetFreeCapacityAsync(DateOnly date, string timeSlot, Guid? ignoreBookingId = null)
    {
        var slots = await GetSlotsAsync(date, ignoreBookingId);
        var target = slots.FirstOrDefault(s => s.Time == timeSlot && !s.IsPast);
        return target?.FreeCapacity ?? 0;
    }
}
