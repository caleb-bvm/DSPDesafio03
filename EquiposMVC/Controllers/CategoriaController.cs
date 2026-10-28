using EquiposMVC.Models;
using EquiposMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquiposMVC.Controllers;

// CRUD de categorías. Todo es solo para Admin.
public class CategoriaController(CategoriaService categorias) : AppController
{
    // GET /Categoria
    public async Task<IActionResult> Index()
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;

        var resultado = await categorias.Listar();
        if (!resultado.Ok) return PaginaDeError(resultado);

        return View(resultado.Data);
    }

    // GET /Categoria/Create
    [HttpGet]
    public IActionResult Create()
    {
        return VerificarAcceso("Admin") ?? View(new CategoriaForm());
    }

    // POST /Categoria/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoriaForm form)
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;
        if (!ModelState.IsValid) return View(form);

        var resultado = await categorias.Crear(form);
        if (resultado.Ok)
        {
            TempData["Success"] = "Categoría creada.";
            return RedirectToAction(nameof(Index));
        }

        // 409 (nombre repetido): mensaje en el formulario sin perder lo escrito.
        if (resultado.EsErrorDeFormulario)
        {
            ModelState.AddModelError("", resultado.Message);
            return View(form);
        }

        return PaginaDeError(resultado);
    }

    // GET /Categoria/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;

        var resultado = await categorias.Obtener(id);
        if (!resultado.Ok) return PaginaDeError(resultado);

        return View(new CategoriaForm { Nombre = resultado.Data!.Nombre });
    }

    // POST /Categoria/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoriaForm form)
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;
        if (!ModelState.IsValid) return View(form);

        var resultado = await categorias.Editar(id, form);
        if (resultado.Ok)
        {
            TempData["Success"] = "Categoría actualizada.";
            return RedirectToAction(nameof(Index));
        }

        if (resultado.EsErrorDeFormulario)
        {
            ModelState.AddModelError("", resultado.Message);
            return View(form);
        }

        return PaginaDeError(resultado);
    }

    // GET /Categoria/Delete/5  (pantalla de confirmación)
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;

        var resultado = await categorias.Obtener(id);
        if (!resultado.Ok) return PaginaDeError(resultado);

        return View(resultado.Data);
    }

    // POST /Categoria/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;

        var resultado = await categorias.Eliminar(id);
        if (resultado.Ok)
        {
            TempData["Success"] = "Categoría eliminada.";
            return RedirectToAction(nameof(Index));
        }

        // 409 (tiene equipos): se vuelve a mostrar la confirmación con el mensaje.
        if (resultado.EsErrorDeFormulario)
        {
            ModelState.AddModelError("", resultado.Message);
            var categoria = await categorias.Obtener(id);
            return View(categoria.Data ?? new CategoriaDto { Id = id });
        }

        return PaginaDeError(resultado);
    }
}
