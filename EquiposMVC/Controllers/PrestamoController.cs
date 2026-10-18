using EquiposMVC.Models;
using EquiposMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquiposMVC.Controllers;

public class PrestamoController(PrestamoService prestamos, EquipoService equipos, AuthService auth) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(bool soloVencidos = false)
    {
        var access = CheckAccess();
        if (access is not null) return access;

        var result = await prestamos.Listar(soloVencidos);
        ViewBag.SoloVencidos = soloVencidos;
        return HandleError(result) ?? View(result.Data ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? equipoId)
    {
        var access = CheckAccess("Admin", "Docente");
        if (access is not null) return access;
        var error = await CargarOpciones(equipoId);
        return error ?? View(new PrestamoForm { EquipoId = equipoId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrestamoForm form)
    {
        var access = CheckAccess("Admin", "Docente");
        if (access is not null) return access;
        if (ModelState.IsValid)
        {
            var result = await prestamos.Crear(form);
            var error = HandleError(result, form: true);
            if (error is not null) return error;
            if (result.Ok)
            {
                TempData["Success"] = "Préstamo registrado.";
                return RedirectToAction(nameof(Index));
            }
        }

        var opcionesError = await CargarOpciones(form.EquipoId);
        return opcionesError ?? View(form);
    }

    // La API no permite modificar un préstamo. Edit confirma su devolución.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var access = CheckAccess("Admin", "Docente");
        if (access is not null) return access;

        var result = await prestamos.Listar();
        var error = HandleError(result);
        if (error is not null) return error;
        var prestamo = result.Data?.FirstOrDefault(p => p.Id == id);
        return prestamo is null ? View("NotFound") : View(prestamo);
    }

    [HttpPost, ActionName("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarDevolucion(int id)
    {
        var access = CheckAccess("Admin", "Docente");
        if (access is not null) return access;

        var result = await prestamos.Devolver(id);
        var error = HandleError(result, form: true);
        if (error is not null) return error;
        if (!result.Ok)
        {
            var listado = await prestamos.Listar();
            error = HandleError(listado);
            if (error is not null) return error;
            var prestamo = listado.Data?.FirstOrDefault(p => p.Id == id);
            return prestamo is null ? View("NotFound") : View(prestamo);
        }

        TempData["Success"] = "Devolución registrada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Delete()
    {
        var access = CheckAccess("Admin", "Docente");
        return access ?? View();
    }

    private async Task<IActionResult?> CargarOpciones(int? equipoSeleccionado)
    {
        var equiposResult = await equipos.Disponibles();
        var error = HandleError(equiposResult);
        if (error is not null) return error;

        var listaEquipos = equiposResult.Data ?? [];
        if (equipoSeleccionado > 0 && listaEquipos.All(e => e.Id != equipoSeleccionado))
        {
            // Si otro operador lo prestó mientras se llenaba el formulario,
            // mantenemos visible la selección para que el usuario entienda el 409.
            var anterior = await equipos.Obtener(equipoSeleccionado.Value);
            listaEquipos.Add(anterior.Data ?? new EquipoDto
            {
                Id = equipoSeleccionado.Value,
                Nombre = $"Equipo #{equipoSeleccionado}"
            });
        }

        var usuariosResult = await auth.Usuarios();
        error = HandleError(usuariosResult);
        if (error is not null) return error;

        ViewBag.Equipos = listaEquipos;
        ViewBag.Usuarios = usuariosResult.Data ?? new List<UsuarioDto>();
        return null;
    }
}
