using EquiposReportes.Models;
using EquiposReportes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquiposReportes.Controllers;

[Authorize(Roles = "Admin")]
public class ReportesController(ReportesService reportes) : Controller
{
    public IActionResult Index() => View();

    public async Task<IActionResult> Inventario(DateOnly? desde, DateOnly? hasta, int? categoriaId)
    {
        var modelo = new PaginaReporteViewModel<InventarioFilaViewModel>
        {
            Filtros = await CrearFiltrosAsync(categoriaId, desde, hasta, null)
        };
        if (FechasValidas(desde, hasta, modelo))
            modelo.Filas = await reportes.InventarioAsync(desde, hasta, categoriaId);
        return View(modelo);
    }

    public async Task<IActionResult> Vencidos(DateOnly? desde, DateOnly? hasta, int? categoriaId)
    {
        var modelo = new PaginaReporteViewModel<VencidoFilaViewModel>
        {
            Filtros = await CrearFiltrosAsync(categoriaId, desde, hasta, null)
        };
        if (FechasValidas(desde, hasta, modelo))
            modelo.Filas = await reportes.VencidosAsync(desde, hasta, categoriaId);
        return View(modelo);
    }

    public async Task<IActionResult> Top(DateOnly? desde, DateOnly? hasta, int? categoriaId)
    {
        var modelo = new PaginaReporteViewModel<TopEquipoFilaViewModel>
        {
            Filtros = await CrearFiltrosAsync(categoriaId, desde, hasta, null)
        };
        if (FechasValidas(desde, hasta, modelo))
            modelo.Filas = await reportes.TopEquiposAsync(desde, hasta, categoriaId);
        return View(modelo);
    }

    public async Task<IActionResult> Historial(
        string? username, DateOnly? desde, DateOnly? hasta, int? categoriaId)
    {
        var modelo = new PaginaReporteViewModel<HistorialFilaViewModel>
        {
            Filtros = await CrearFiltrosAsync(categoriaId, desde, hasta, username)
        };
        if (FechasValidas(desde, hasta, modelo))
            modelo.Filas = await reportes.HistorialAsync(username, desde, hasta, categoriaId);
        return View(modelo);
    }

    private async Task<FiltroReporteViewModel> CrearFiltrosAsync(
        int? categoriaId, DateOnly? desde, DateOnly? hasta, string? username) => new()
    {
        CategoriaId = categoriaId,
        Desde = desde,
        Hasta = hasta,
        Username = username,
        Categorias = await reportes.CategoriasAsync()
    };

    private static bool FechasValidas<T>(
        DateOnly? desde, DateOnly? hasta, PaginaReporteViewModel<T> modelo)
    {
        if (desde.HasValue && hasta.HasValue && desde > hasta)
        {
            modelo.Error = "La fecha inicial no puede ser posterior a la fecha final.";
            return false;
        }
        return true;
    }
}
