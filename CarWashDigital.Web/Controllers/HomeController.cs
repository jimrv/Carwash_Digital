using CarWashDigital.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarWashDigital.Web.Controllers;

public class HomeController : Controller
{
    private readonly CarWashDbContext _db;
    public HomeController(CarWashDbContext db) => _db = db;

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var mainCodes = new[] { "s1", "s2", "s3" };
        var services = await _db.Services
            .Where(s => s.IsActive && mainCodes.Contains(s.Code))
            .OrderBy(s => s.Code)
            .ToListAsync();
        return View(services);
    }

    [HttpGet("/servicios")]
    public async Task<IActionResult> Services()
    {
        var services = await _db.Services.Where(s => s.IsActive).OrderBy(s => s.Code).ToListAsync();
        return View(services);
    }

    [HttpGet("/servicios/{id:guid}")]
    public async Task<IActionResult> ServiceDetail(Guid id)
    {
        var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
        if (svc == null) return NotFound();
        return View(svc);
    }

    [HttpGet("/como-funciona")]
    public IActionResult HowItWorks() => View();

    [HttpGet("/Home/Error")]
    public IActionResult Error() => View();
}
