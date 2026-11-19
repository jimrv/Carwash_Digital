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
        var services = await _db.Services.Where(s => s.IsActive).OrderBy(s => s.Code).ToListAsync();
        return View(services);
    }

    [HttpGet("/servicios")]
    public async Task<IActionResult> Services()
    {
        var services = await _db.Services.Where(s => s.IsActive).OrderBy(s => s.Code).ToListAsync();
        return View(services);
    }

    [HttpGet("/como-funciona")]
    public IActionResult HowItWorks() => View();

    [HttpGet("/Home/Error")]
    public IActionResult Error() => View();
}
