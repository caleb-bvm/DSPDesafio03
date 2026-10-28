using EquiposMVC.Models;
using EquiposMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquiposMVC.Controllers;

// Listado con filtros y paginación (todos los roles) y CRUD de equipos (solo Admin).
public class EquipoController(EquipoService equipos, CategoriaService categorias) : AppController
{
    // GET /Equipo?texto=&categoriaId=&estado=&page=1
    public async Task<IActionResult> Index(string? texto, int? categoriaId, string? estado, int page = 1)
    {
        var acceso = VerificarAcceso();   // Cualquier rol con sesión.
        if (acceso is not null) return acceso;

        // Páginas de 10 registros. Para el Estudiante la API devuelve solo disponibles.
        var resultado = await equipos.Listar(texto, categoriaId, estado, Math.Max(1, page));
        if (!resultado.Ok) return PaginaDeError(resultado);

        var listaCategorias = await categorias.Listar();

        return View(new EquipoIndexViewModel
        {
            Pagina = resultado.Data!,
            Categorias = listaCategorias.Data ?? [],
            Texto = texto,
            CategoriaId = categoriaId,
            Estado = estado
        });
    }

    // GET /Equipo/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;

        await CargarCategorias();
        return View(new EquipoForm());
    }

    // POST /Equipo/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EquipoForm form)
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;

        if (ModelState.IsValid)
        {
            var resultado = await equipos.Crear(form);
            if (resultado.Ok)
            {
                TempData["Success"] = "Equipo creado.";
                return RedirectToAction(nameof(Index));
            }

            if (!resultado.EsErrorDeFormulario)
                return PaginaDeError(resultado);

            // 409 (código repetido) o 400: el mensaje de la API va al formulario.
            ModelState.AddModelError("", resultado.Message);
        }

        // Se vuelve a mostrar el formulario con lo que el usuario escribió.
        await CargarCategorias();
        return View(form);
    }

    // GET /Equipo/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;

        var resultado = await equipos.Obtener(id);
        if (!resultado.Ok) return PaginaDeError(resultado);

        var equipo = resultado.Data!;
        await CargarCategorias();
        return View(new EquipoForm
        {
            CodigoInventario = equipo.CodigoInventario,
            Nombre = equipo.Nombre,
            Descripcion = equipo.Descripcion,
            CategoriaId = equipo.CategoriaId,
            Estado = equipo.Estado,
            RowVersion = equipo.RowVersion   // Versión leída ahora; se enviará al guardar.
        });
    }

    // POST /Equipo/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EquipoForm form)
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;

        if (ModelState.IsValid)
        {
            // form.RowVersion viaja a la API. Si otro usuario guardó antes, la API responde 409.
            var resultado = await equipos.Editar(id, form);
            if (resultado.Ok)
            {
                TempData["Success"] = "Equipo actualizado.";
                return RedirectToAction(nameof(Index));
            }

            if (!resultado.EsErrorDeFormulario)
                return PaginaDeError(resultado);

            // 409 de concurrencia: se muestra el mensaje SIN perder lo escrito.
            ModelState.AddModelError("", resultado.Message);
        }

        await CargarCategorias();
        return View(form);
    }

    // GET /Equipo/Delete/5  (pantalla de confirmación)
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;

        var resultado = await equipos.Obtener(id);
        if (!resultado.Ok) return PaginaDeError(resultado);

        return View(resultado.Data);
    }

    // POST /Equipo/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var acceso = VerificarAcceso("Admin");
        if (acceso is not null) return acceso;

        var resultado = await equipos.Eliminar(id);
        if (resultado.Ok)
        {
            TempData["Success"] = "Equipo eliminado.";
            return RedirectToAction(nameof(Index));
        }

        // 409 (tiene préstamos históricos): se vuelve a mostrar la confirmación con el mensaje.
        if (resultado.EsErrorDeFormulario)
        {
            ModelState.AddModelError("", resultado.Message);
            var equipo = await equipos.Obtener(id);
            return View(equipo.Data ?? new EquipoDto { Id = id });
        }

        return PaginaDeError(resultado);
    }

    // Deja la lista de categorías en ViewBag para el <select> del formulario.
    private async Task CargarCategorias()
    {
        var resultado = await categorias.Listar();
        ViewBag.Categorias = resultado.Data ?? [];
    }
}
