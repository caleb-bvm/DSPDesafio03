using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using EquiposReportes.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquiposReportes.Controllers;

public class CuentaController(IHttpClientFactory clientes) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Reportes");

        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            var cliente = clientes.CreateClient("EquiposAPI");
            var respuesta = await cliente.PostAsJsonAsync("api/auth/login", new
            {
                modelo.Username,
                modelo.Password
            });

            if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            {
                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                return View(modelo);
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "No se pudo iniciar sesión. Inténtalo de nuevo.");
                return View(modelo);
            }

            var datos = await respuesta.Content.ReadFromJsonAsync<LoginRespuesta>();
            if (datos is null || datos.Rol != "Admin" || datos.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Los reportes son exclusivos para administradores.");
                return View(modelo);
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, datos.Username),
                new Claim(ClaimTypes.Role, datos.Rol)
            };
            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identidad);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { ExpiresUtc = datos.ExpiresAt, IsPersistent = false });

            return RedirectToAction("Index", "Reportes");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo conectar con la API.");
            return View(modelo);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();
}
