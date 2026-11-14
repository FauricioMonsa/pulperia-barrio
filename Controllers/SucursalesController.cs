using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PulperiaBarrio.Data;
using PulperiaBarrio.Models;

namespace PulperiaBarrio.Controllers;

public class SucursalesController(PulperiaBarrioContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? busqueda)
    {
        var sucursales = context.Sucursales.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            sucursales = sucursales.Where(sucursal =>
                EF.Functions.ILike(sucursal.Nombre, $"%{busqueda}%"));
        }

        ViewData["Busqueda"] = busqueda;
        return View(await sucursales.OrderBy(sucursal => sucursal.Nombre).ToListAsync());
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View("Formulario", new Sucursal { Activa = true });
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var sucursal = await context.Sucursales.FindAsync(id);
        if (sucursal is null)
        {
            return NotFound();
        }

        return View("Formulario", sucursal);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(Sucursal sucursal)
    {
        if (!ModelState.IsValid)
        {
            return View("Formulario", sucursal);
        }

        var esNueva = sucursal.Id == 0;
        if (esNueva)
        {
            context.Sucursales.Add(sucursal);
        }
        else
        {
            var existente = await context.Sucursales.FindAsync(sucursal.Id);
            if (existente is null)
            {
                return NotFound();
            }

            existente.Nombre = sucursal.Nombre;
            existente.Ubicacion = sucursal.Ubicacion;
            existente.Activa = sucursal.Activa;
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (GetPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError(string.Empty, "Ya existe una sucursal con ese nombre");
            return View("Formulario", sucursal);
        }

        TempData["Mensaje"] = esNueva
            ? "La sucursal se creó correctamente"
            : "La sucursal se actualizó correctamente";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var sucursal = await context.Sucursales.FindAsync(id);
        if (sucursal is null)
        {
            return NotFound(new { mensaje = "La sucursal ya no existe." });
        }

        context.Sucursales.Remove(sucursal);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (GetPostgresException(exception)?.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Conflict(new
            {
                mensaje = "No se puede eliminar: la sucursal tiene inventario o reservas relacionados"
            });
        }

        return Json(new { mensaje = "La sucursal se eliminó correctamente." });
    }

    private static PostgresException? GetPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        return null;
    }
}
