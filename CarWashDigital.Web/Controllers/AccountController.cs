using System.Security.Claims;
using CarWashDigital.Web.Data;
using CarWashDigital.Web.Models.Entities;
using CarWashDigital.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarWashDigital.Web.Controllers;

public class AccountController : Controller
{
    private readonly CarWashDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();

    public AccountController(CarWashDbContext db) => _db = db;

    [HttpGet("/login")]
    public IActionResult Login(string? returnUrl = null) => View(new LoginVm { ReturnUrl = returnUrl });

    [HttpPost("/login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = (vm.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = (vm.Password ?? string.Empty).Trim();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash) ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(vm);
        }

        await SignInUserAsync(user);

        if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);

        return RedirectToAction("Index", user.Role == "Admin" ? "Admin" : "Client");
    }

    [HttpGet("/registro")]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost("/registro"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = (vm.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            ModelState.AddModelError(nameof(vm.Email), "Ese correo ya está registrado.");
            return View(vm);
        }

        var user = new User
        {
            Name = vm.Name.Trim(),
            Email = email,
            Phone = vm.Phone.Trim(),
            Role = "Client",
            AccountType = vm.AccountType == "Fleet" ? "Fleet" : "Individual",
            CompanyName = string.IsNullOrWhiteSpace(vm.CompanyName) ? null : vm.CompanyName.Trim(),
            Ruc = string.IsNullOrWhiteSpace(vm.Ruc) ? null : vm.Ruc.Trim()
        };
        user.PasswordHash = _hasher.HashPassword(user, vm.Password.Trim());

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await SignInUserAsync(user);
        TempData["Success"] = "Cuenta creada correctamente.";
        return RedirectToAction("Index", "Client");
    }

    [HttpGet("/recuperar")]
    public IActionResult ForgotPassword() => View();

    [HttpPost("/recuperar"), ValidateAntiForgeryToken]
    public IActionResult ForgotPasswordPost(string email)
    {
        TempData["Error"] = "Función pendiente: requiere servicio de correo configurado.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    [HttpGet("/acceso-denegado")]
    public IActionResult AccessDenied() => View();

    [HttpPost("/logout"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private async Task SignInUserAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }
}
