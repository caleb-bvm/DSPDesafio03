using EquiposMVC.Models;
using EquiposMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquiposMVC.Controllers;

// Préstamos:
//   Index  -> lista (el Estudiante solo recibe los suyos; lo decide la API).
//   Create -> registrar préstamo (Admin y Docente).
//   Edit   -> registrar la devolución (un préstamo no se modifica de otra forma).
//   Delete -> solo informa que los préstamos no se eliminan (se conserva el historial).
public class PrestamoController(PrestamoService prestamos, EquipoService equipos, AuthService auth) : AppController
{
    // GET /Prestamo?soloVencidos=true
    public async Task<IActionResult> Index(bool soloVencidos = false)
    {
        var acceso = VerificarAcceso();
        if (acceso is not null) return acceso;

        var resultado = await prestamos.Listar(soloVencidos);
        if (!resultado.Ok) return PaginaDeError(resultado);

        ViewBag.SoloVencidos = soloVencidos;
        return View(resultado.Data);
    }

    // GET /Prestamo/Create?equipoId=5
    [HttpGet]
    public async Task<IActionResult> Create(int equipoId = 0)
    {
        var acceso = VerificarAcceso("Admin", "Docente");
        if (acceso is not null) return acceso;

        await CargarListas(equipoId);
        return View(new PrestamoForm { EquipoId = equipoId });
    }

    // POST /Prestamo/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrestamoForm form)
    {
        var acceso = VerificarAcceso("Admin", "Docente");
        if (acceso is not null) return acceso;

        if (ModelState.IsValid)
        {
            var resultado = await prestamos.Crear(form);
            if (resultado.Ok)
            {
                TempData["Success"] = "Préstamo registrado.";
                return RedirectToAction(nameof(Index));
            }

            if (!resultado.EsErrorDeFormulario)
                return PaginaDeError(resultado);

            // 409 (equipo no disponible) o 400 (fecha inválida): mensaje en el formulario.
            ModelState.AddModelError("", resultado.Message);
        }

        await CargarListas(form.EquipoId);
        return View(form);
    }

    // GET /Prestamo/Edit/5  (confirmar devolución)
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var acceso = VerificarAcceso("Admin", "Docente");
        if (acceso is not null) return acceso;

        return await MostrarPrestamo(id);
    }

    // POST /Prestamo/Edit/5  (registrar devolución)
    [HttpPost, ActionName("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarDevolucion(int id)
    {
        var acceso = VerificarAcceso("Admin", "Docente");
        if (acceso is not null) return acceso;

        var resultado = await prestamos.Devolver(id);
        if (resultado.Ok)
        {
            TempData["Success"] = "Devolución registrada.";
            return RedirectToAction(nameof(Index));
        }

        // 409 (ya devuelto): se muestra el mensaje en la misma pantalla.
        if (resultado.EsErrorDeFormulario)
        {
            ModelState.AddModelError("", resultado.Message);
            return await MostrarPrestamo(id);
        }

        return PaginaDeError(resultado);
    }

    // GET /Prestamo/Delete
    [HttpGet]
    public IActionResult Delete()
    {
        return VerificarAcceso("Admin", "Docente") ?? View();
    }

    // Busca un préstamo en la lista de la API y muestra la vista Edit.
    private async Task<IActionResult> MostrarPrestamo(int id)
    {
        var resultado = await prestamos.Listar();
        if (!resultado.Ok) return PaginaDeError(resultado);

        var prestamo = resultado.Data!.FirstOrDefault(p => p.Id == id);
        return prestamo is null ? View("NotFound") : View("Edit", prestamo);
    }

    // Llena ViewBag con los equipos disponibles y los usuarios para los <select>.
    private async Task CargarListas(int equipoSeleccionado)
    {
        var listaEquipos = (await equipos.Disponibles()).Data?.Items ?? [];

        // Si el equipo elegido ya no está disponible (alguien lo prestó mientras
        // se llenaba el formulario), se agrega igual para no perder la selección.
        if (equipoSeleccionado > 0 && !listaEquipos.Any(e => e.Id == equipoSeleccionado))
        {
            var equipo = (await equipos.Obtener(equipoSeleccionado)).Data;
            if (equipo is not null) listaEquipos.Add(equipo);
        }

        ViewBag.Equipos = listaEquipos;
        ViewBag.Usuarios = (await auth.Usuarios()).Data ?? [];
    }
}
