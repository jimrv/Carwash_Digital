using CarWashDigital.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarWashDigital.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(CarWashDbContext db)
    {
        var hasher = new PasswordHasher<User>();

        if (!await db.BusinessConfigs.AnyAsync())
        {
            db.BusinessConfigs.Add(new BusinessConfig
            {
                Id = 1,
                OpenHour = 8,
                CloseHour = 18,
                CapacityPerSlot = 2,
                OpenDays = new List<int> { 1, 2, 3, 4, 5, 6 },
                ClosedDates = new List<string>()
            });
        }

        var seedServices = new List<ServiceItem>
        {
            new()
            {
                Code = "s1",
                Name = "Lavado Express",
                DurationMinutes = 30,
                Description = "Lavado exterior rápido con espuma activa para el día a día.",
                Includes = new List<string> { "Pre-lavado a presión", "Shampoo neutro con espuma", "Secado con microfibra", "Brillo de llantas" },
                PricesByCategory = new Dictionary<string, decimal> { ["Sedán"] = 25m, ["SUV"] = 30m, ["Pick-up 4x4"] = 35m },
                IsActive = true
            },
            new()
            {
                Code = "s2",
                Name = "Lavado Completo",
                DurationMinutes = 60,
                Description = "Limpieza integral exterior e interior con aspirado profundo.",
                Includes = new List<string> { "Todo el Lavado Express", "Aspirado de asientos y alfombras", "Limpieza y acondicionador de tablero", "Limpieza de lunas interiores" },
                PricesByCategory = new Dictionary<string, decimal> { ["Sedán"] = 45m, ["SUV"] = 55m, ["Pick-up 4x4"] = 65m },
                IsActive = true
            },
            new()
            {
                Code = "s3",
                Name = "Lavado Premium",
                DurationMinutes = 90,
                Description = "Detallado profundo con cera protectora UV y limpieza de motor.",
                Includes = new List<string> { "Todo el Lavado Completo", "Cera selladora de alto brillo", "Limpieza y desengrase de motor", "Hidratación de plásticos exteriores" },
                PricesByCategory = new Dictionary<string, decimal> { ["Sedán"] = 80m, ["SUV"] = 95m, ["Pick-up 4x4"] = 110m },
                IsActive = true
            },
            new()
            {
                Code = "s4",
                Name = "Sanitización & Ozono Interior",
                DurationMinutes = 50,
                Description = "Eliminación de ácaros, bacterias y olores en cabina y ductos de aire.",
                Includes = new List<string> { "Aspirado detallado de cabina", "Generador de ozono grado automotriz", "Desinfección de timón, palanca y manijas", "Aromatización neutra" },
                PricesByCategory = new Dictionary<string, decimal> { ["Sedán"] = 55m, ["SUV"] = 65m, ["Pick-up 4x4"] = 75m },
                IsActive = true
            },
            new()
            {
                Code = "f1",
                Name = "Flota Básica (+5 autos)",
                DurationMinutes = 120,
                Description = "Tarifa preferencial por unidad para empresas con mínimo 5 vehículos por servicio.",
                Includes = new List<string> { "Lavado exterior completo por unidad", "Aspirado rápido de cabina", "Reporte digital de unidades atendidas", "Facturación consolidada mensual" },
                PricesByCategory = new Dictionary<string, decimal>
                {
                    ["Flota Sedán / Taxi (x auto)"] = 20m,
                    ["Flota SUV / Van (x auto)"] = 26m,
                    ["Flota Pick-up / Carga (x auto)"] = 30m
                },
                IsActive = true
            },
            new()
            {
                Code = "f2",
                Name = "Flota Corporativa Integral (+5 autos)",
                DurationMinutes = 180,
                Description = "Mantenimiento estético completo y sanitización para flotas ejecutivas o comerciales.",
                Includes = new List<string> { "Lavado exterior + interior profundo", "Sanitización de cabina por unidad", "Prioridad en bahías programadas", "Seguimiento en vivo por placa" },
                PricesByCategory = new Dictionary<string, decimal>
                {
                    ["Flota Sedán / Taxi (x auto)"] = 38m,
                    ["Flota SUV / Van (x auto)"] = 48m,
                    ["Flota Pick-up / Carga (x auto)"] = 56m
                },
                IsActive = true
            }
        };

        foreach (var item in seedServices)
        {
            if (!await db.Services.AnyAsync(s => s.Code == item.Code))
            {
                db.Services.Add(item);
            }
        }

        if (!await db.Users.AnyAsync(u => u.Email == "admin@demo.pe"))
        {
            var admin = new User { Name = "Administrador CarWash", Email = "admin@demo.pe", Phone = "999000111", Role = "Admin", AccountType = "Individual" };
            admin.PasswordHash = hasher.HashPassword(admin, "admin123");
            db.Users.Add(admin);
        }

        if (!await db.Users.AnyAsync(u => u.Email == "cliente@demo.pe"))
        {
            var client = new User { Name = "Cliente Demo", Email = "cliente@demo.pe", Phone = "988777666", Role = "Client", AccountType = "Individual" };
            client.PasswordHash = hasher.HashPassword(client, "demo123");
            db.Users.Add(client);
            await db.SaveChangesAsync();

            db.Vehicles.Add(new Vehicle
            {
                UserId = client.Id,
                Brand = "Toyota",
                Model = "Corolla",
                Category = "Sedán",
                Plate = "ABC-123",
                Color = "Gris",
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
    }
}
