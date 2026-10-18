using EquiposMVC.Models;
using EquiposMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquiposMVC.Controllers;

public class EquipoController(EquipoService equipos, CategoriaService categorias) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? texto, int? categoriaId, string? estado, int page = 1)
    {
        var access = CheckAccess();
        if (access is not null) return access;

        var esEstudiante = HttpContext.Session.GetString(SessionKeys.Rol) == "Estudiante";
        if (esEstudiante) estado = "Disponible";
        page = Math.Max(1, page);

        var listado = await equipos.Listar(texto, categoriaId, estado, page);
        var error = HandleError(listado);
        if (error is not null) return error;

        var listaCategorias = await categorias.Listar();
        error = HandleError(listaCategorias);
        if (error is not null) return error;

        return View(new EquipoIndexViewModel
        {
            Pagina = listado.Data ?? new(),
            Categorias = listaCategorias.Data ?? [],
            Texto = texto,
            CategoriaId = categoriaId,
            Estado = estado
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;
        var error = await CargarCategorias();
        return error ?? View(new EquipoForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EquipoForm form)
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;
        if (ModelState.IsValid)
        {
            var result = await equipos.Crear(form);
            var error = HandleError(result, form: true);
            if (error is not null) return error;
            if (result.Ok)
            {
                TempData["Success"] = "Equipo creado.";
                return RedirectToAction(nameof(Index));
            }
        }

        var categoriasError = await CargarCategorias();
        return categoriasError ?? View(form);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;

        var result = await equipos.Obtener(id);
        var error = HandleError(result);
        if (error is not null) return error;
        error = await CargarCategorias();
        if (error is not null) return error;

        var equipo = result.Data!;
        return View(new EquipoForm
        {
            CodigoInventario = equipo.CodigoInventario,
            Nombre = equipo.Nombre,
            Descripcion = equipo.Descripcion,
            CategoriaId = equipo.CategoriaId,
            Estado = equipo.Estado,
            RowVersion = equipo.RowVersion
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EquipoForm form)
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;
        if (string.IsNullOrWhiteSpace(form.RowVersion))
            ModelState.AddModelError("", "Falta la versión del equipo. Vuelve a abrir la edición.");

        if (ModelState.IsValid)
        {
            var result = await equipos.Editar(id, form);
            var error = HandleError(result, form: true);
            if (error is not null) return error;
            if (result.Ok)
            {
                TempData["Success"] = "Equipo actualizado.";
                return RedirectToAction(nameof(Index));
            }
        }

        var categoriasError = await CargarCategorias();
        return categoriasError ?? View(form);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;

        var result = await equipos.Obtener(id);
        return HandleError(result) ?? View(result.Data);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;

        var result = await equipos.Eliminar(id);
        var error = HandleError(result, form: true);
        if (error is not null) return error;
        if (!result.Ok)
        {
            var equipo = await equipos.Obtener(id);
            error = HandleError(equipo);
            return error ?? View(equipo.Data);
        }

        TempData["Success"] = "Equipo eliminado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult?> CargarCategorias()
    {
        var result = await categorias.Listar();
        var error = HandleError(result);
        if (error is not null) return error;
        ViewBag.Categorias = result.Data ?? new List<CategoriaDto>();
        return null;
    }
}
