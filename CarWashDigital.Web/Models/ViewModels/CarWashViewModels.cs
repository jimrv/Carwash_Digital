using System.ComponentModel.DataAnnotations;
using CarWashDigital.Web.Models.Entities;

namespace CarWashDigital.Web.Models.ViewModels;

public class LoginVm
{
    [Required(ErrorMessage = "Ingresa tu correo."), EmailAddress(ErrorMessage = "Correo inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterVm
{
    [Required(ErrorMessage = "Ingresa tu nombre completo.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu correo."), EmailAddress(ErrorMessage = "Correo inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu celular.")]
    public string Phone { get; set; } = string.Empty;

    public string AccountType { get; set; } = "Individual"; // Individual | Fleet
    public string? CompanyName { get; set; }
    public string? Ruc { get; set; }

    [Required, MinLength(8, ErrorMessage = "Mínimo 8 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Debes aceptar los términos y condiciones.")]
    public bool AcceptTerms { get; set; }
}

public record SlotOptionVm(string Time, int FreeCapacity, bool IsPast);

public class VehicleStepVm
{
    public ServiceItem Service { get; set; } = null!;
    public List<Vehicle> Vehicles { get; set; } = new();
    public Guid? RescheduleId { get; set; }
}

public class AddVehicleInput
{
    [Required] public string Brand { get; set; } = string.Empty;
    [Required] public string Model { get; set; } = string.Empty;
    [Required] public string Category { get; set; } = "Sedán";
    [Required] public string Plate { get; set; } = string.Empty;
    public string? Color { get; set; }
    public Guid? ServiceId { get; set; }
    public bool ReturnToBooking { get; set; }
}

public class ScheduleStepVm
{
    public ServiceItem Service { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public DateOnly SelectedDate { get; set; }
    public List<SlotOptionVm> Slots { get; set; } = new();
    public Guid? RescheduleId { get; set; }
}

public class ConfirmStepVm
{
    public ServiceItem Service { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public User Customer { get; set; } = null!;
    public DateOnly Date { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid? RescheduleId { get; set; }
}

public class ConfirmBookingInput
{
    public Guid ServiceId { get; set; }
    public Guid VehicleId { get; set; }
    public DateOnly Date { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public Guid? RescheduleId { get; set; }
}

public record BookingOptionVm(string Code, DateOnly Date, string TimeSlot);

public class TrackingVm
{
    public string? SearchCode { get; set; }
    public Booking? FoundBooking { get; set; }
    public List<BookingOptionVm> MyBookings { get; set; } = new();
}

public class ClientDashboardVm
{
    public User User { get; set; } = null!;
    public Booking? NextBooking { get; set; }
    public List<Booking> CompletedBookings { get; set; } = new();
}

public class AdminDashboardVm
{
    public int TodayBookingsCount { get; set; }
    public int PendingEntryCount { get; set; }
    public int InProgressCount { get; set; }
    public int ReadyForPickupCount { get; set; }
    public int FreeSlotsTodayCount { get; set; }
    public int CancelledCount { get; set; }
    public List<Booking> UpcomingBookings { get; set; } = new();
    public Dictionary<string, int> TopServices { get; set; } = new();
}

public class AdminCalendarVm
{
    public DateOnly SelectedDate { get; set; }
    public int CapacityPerSlot { get; set; }
    public List<CalendarSlotRowVm> Rows { get; set; } = new();
}

public record CalendarSlotRowVm(string TimeSlot, int Used, int Free, List<Booking> Bookings);

public class AdminClientRowVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Individual";
    public string? CompanyName { get; set; }
    public int VehiclesCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
}
