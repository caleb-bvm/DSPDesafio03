using EquiposReportes.Models;
using EquiposReportes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquiposReportes.Controllers;

// Solo el rol Admin puede ver los reportes (el rol viene de la cookie creada en CuentaController).
[Authorize(Roles = "Admin")]
public class ReportesController(ReportesService reportes) : Controller
{
    // GET /Reportes  (menú con los cuatro reportes)
    public IActionResult Index() => View();

    // GET /Reportes/Inventario?desde=&hasta=&categoriaId=
    public async Task<IActionResult> Inventario(FiltrosViewModel filtros)
    {
        var modelo = await CrearModelo<InventarioFilaViewModel>(filtros);
        if (modelo.Error is null)
            modelo.Filas = await reportes.InventarioAsync(filtros);
        return View(modelo);
    }

    // GET /Reportes/Vencidos?desde=&hasta=&categoriaId=
    public async Task<IActionResult> Vencidos(FiltrosViewModel filtros)
    {
        var modelo = await CrearModelo<VencidoFilaViewModel>(filtros);
        if (modelo.Error is null)
            modelo.Filas = await reportes.VencidosAsync(filtros);
        return View(modelo);
    }

    // GET /Reportes/Top?desde=&hasta=&categoriaId=
    public async Task<IActionResult> Top(FiltrosViewModel filtros)
    {
        var modelo = await CrearModelo<TopEquipoFilaViewModel>(filtros);
        if (modelo.Error is null)
            modelo.Filas = await reportes.TopEquiposAsync(filtros);
        return View(modelo);
    }

    // GET /Reportes/Historial?username=&desde=&hasta=&categoriaId=
    public async Task<IActionResult> Historial(FiltrosViewModel filtros)
    {
        var modelo = await CrearModelo<HistorialFilaViewModel>(filtros);
        if (modelo.Error is null)
            modelo.Filas = await reportes.HistorialAsync(filtros);
        return View(modelo);
    }

    // Prepara el modelo de la vista: carga las categorías del filtro y
    // revisa que el rango de fechas tenga sentido.
    private async Task<ReporteViewModel<TFila>> CrearModelo<TFila>(FiltrosViewModel filtros)
    {
        filtros.Categorias = await reportes.CategoriasAsync();
        var modelo = new ReporteViewModel<TFila> { Filtros = filtros };

        if (filtros.Desde > filtros.Hasta)
            modelo.Error = "La fecha inicial no puede ser posterior a la fecha final.";

        return modelo;
    }
}
