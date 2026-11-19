using CarWashDigital.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarWashDigital.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(CarWashDbContext db)
    {
        await db.Database.MigrateAsync();

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

        if (!await db.Users.AnyAsync())
        {
            var hasher = new PasswordHasher<User>();
            var admin = new User
            {
                Name = "Admin Demo",
                Email = "admin@demo.pe",
                Phone = "",
                Role = "Admin",
                AccountType = "Individual"
            };
            admin.PasswordHash = hasher.HashPassword(admin, "admin123");

            var client = new User
            {
                Name = "Cliente Demo",
                Email = "cliente@demo.pe",
                Phone = "999000111",
                Role = "Client",
                AccountType = "Fleet",
                CompanyName = "Flota Demo Lima"
            };
            client.PasswordHash = hasher.HashPassword(client, "demo123");

            db.Users.AddRange(admin, client);
        }

        if (!await db.Services.AnyAsync())
        {
            db.Services.AddRange(
                new ServiceItem
                {
                    Code = "s1",
                    Name = "Lavado Express",
                    Description = "Lavado exterior rápido para el día a día.",
                    Includes = new List<string> { "Lavado exterior", "Secado", "Brillo de llantas" },
                    DurationMinutes = 30,
                    IsActive = true,
                    PricesByCategory = new Dictionary<string, decimal>
                    {
                        ["Sedán"] = 25m,
                        ["SUV"] = 30m,
                        ["Pick-up 4x4"] = 35m
                    }
                },
                new ServiceItem
                {
                    Code = "s2",
                    Name = "Lavado Completo",
                    Description = "Exterior e interior con aspirado.",
                    Includes = new List<string> { "Lavado exterior", "Aspirado interior", "Limpieza de tablero", "Secado" },
                    DurationMinutes = 60,
                    IsActive = true,
                    PricesByCategory = new Dictionary<string, decimal>
                    {
                        ["Sedán"] = 45m,
                        ["SUV"] = 55m,
                        ["Pick-up 4x4"] = 65m
                    }
                },
                new ServiceItem
                {
                    Code = "s3",
                    Name = "Lavado Premium",
                    Description = "Limpieza profunda con cera protectora.",
                    Includes = new List<string> { "Todo el Lavado Completo", "Cera protectora", "Limpieza de motor" },
                    DurationMinutes = 90,
                    IsActive = true,
                    PricesByCategory = new Dictionary<string, decimal>
                    {
                        ["Sedán"] = 80m,
                        ["SUV"] = 95m,
                        ["Pick-up 4x4"] = 110m
                    }
                }
            );
        }

        await db.SaveChangesAsync();
    }
}
