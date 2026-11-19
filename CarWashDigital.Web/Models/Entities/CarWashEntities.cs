namespace CarWashDigital.Web.Models.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Client"; // "Admin" | "Client"
    public string AccountType { get; set; } = "Individual"; // "Individual" | "Fleet"
    public string? CompanyName { get; set; }
    public string? Ruc { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Category { get; set; } = "Sedán"; // "Sedán" | "SUV" | "Pick-up 4x4"
    public string Plate { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ServiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Includes { get; set; } = new();
    public int DurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public Dictionary<string, decimal> PricesByCategory { get; set; } = new();
}

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BookingCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public DateOnly Date { get; set; }
    public string TimeSlot { get; set; } = "08:00";

    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public List<string> ServiceIncludes { get; set; } = new();
    public int DurationMinutes { get; set; }

    public Guid VehicleId { get; set; }
    public string VehicleBrand { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string VehicleCategory { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public string Status { get; set; } = "Confirmada";
    public string InternalNotes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BookingHistory> History { get; set; } = new List<BookingHistory>();
}

public class BookingHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string ChangedBy { get; set; } = string.Empty;
}

public class BusinessConfig
{
    public int Id { get; set; } = 1;
    public int OpenHour { get; set; } = 8;
    public int CloseHour { get; set; } = 18;
    public int CapacityPerSlot { get; set; } = 2;
    public List<int> OpenDays { get; set; } = new() { 1, 2, 3, 4, 5, 6 };
    public List<string> ClosedDates { get; set; } = new();
}
