using EquiposMVC.Models;
using EquiposMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquiposMVC.Controllers;

public class CategoriaController(CategoriaService categorias) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;

        var result = await categorias.Listar();
        return HandleError(result) ?? View(result.Data ?? []);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var access = CheckAccess("Admin");
        return access ?? View(new CategoriaForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoriaForm form)
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;
        if (!ModelState.IsValid) return View(form);

        var result = await categorias.Crear(form);
        var error = HandleError(result, form: true);
        if (error is not null) return error;
        if (!result.Ok) return View(form);

        TempData["Success"] = "Categoría creada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;

        var result = await categorias.Obtener(id);
        return HandleError(result) ?? View(new CategoriaForm { Nombre = result.Data!.Nombre });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoriaForm form)
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;
        if (!ModelState.IsValid) return View(form);

        var result = await categorias.Editar(id, form);
        var error = HandleError(result, form: true);
        if (error is not null) return error;
        if (!result.Ok) return View(form);

        TempData["Success"] = "Categoría actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;

        var result = await categorias.Obtener(id);
        return HandleError(result) ?? View(result.Data);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var access = CheckAccess("Admin");
        if (access is not null) return access;

        var result = await categorias.Eliminar(id);
        var error = HandleError(result, form: true);
        if (error is not null) return error;
        if (!result.Ok)
        {
            var categoria = await categorias.Obtener(id);
            return View(categoria.Data ?? new CategoriaDto { Id = id });
        }

        TempData["Success"] = "Categoría eliminada.";
        return RedirectToAction(nameof(Index));
    }
}
