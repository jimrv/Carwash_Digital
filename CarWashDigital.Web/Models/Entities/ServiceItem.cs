using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWashDigital.Web.Models.Entities;

public class ServiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(30)] public string Code { get; set; } = string.Empty;
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    [MaxLength(500)] public string Description { get; set; } = string.Empty;
    public List<string> Includes { get; set; } = new();
    public Dictionary<string, decimal> PricesByCategory { get; set; } = new();
    public bool IsActive { get; set; } = true;

    [NotMapped]
    public bool IsFleetService => Code.StartsWith("f", StringComparison.OrdinalIgnoreCase)
                                  || PricesByCategory.Keys.Any(k => k.StartsWith("Flota", StringComparison.OrdinalIgnoreCase));
}
