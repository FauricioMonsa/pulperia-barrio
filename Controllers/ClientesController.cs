using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PulperiaBarrio.Data;
using PulperiaBarrio.Models;

namespace PulperiaBarrio.Controllers;

public class ClientesController(PulperiaBarrioContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? busqueda)
    {
        var clientes = context.Clientes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            clientes = clientes.Where(cliente =>
                EF.Functions.ILike(cliente.Nombre, $"%{busqueda}%"));
        }

        ViewData["Busqueda"] = busqueda;
        return View(await clientes.OrderBy(cliente => cliente.Nombre).ToListAsync());
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View("Formulario", new Cliente());
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var cliente = await context.Clientes.FindAsync(id);
        if (cliente is null)
        {
            return NotFound();
        }

        return View("Formulario", cliente);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(
        [Bind(nameof(Cliente.Id), nameof(Cliente.Nombre), nameof(Cliente.Correo), nameof(Cliente.Telefono))] Cliente cliente)
    {
        if (!ModelState.IsValid)
        {
            return View("Formulario", cliente);
        }

        var esNuevo = cliente.Id == 0;
        Cliente clienteGuardado;
        if (esNuevo)
        {
            clienteGuardado = new Cliente
            {
                Nombre = cliente.Nombre,
                Correo = cliente.Correo,
                Telefono = cliente.Telefono
            };
            context.Clientes.Add(clienteGuardado);
        }
        else
        {
            var existente = await context.Clientes.FindAsync(cliente.Id);
            if (existente is null)
            {
                return NotFound();
            }

            existente.Nombre = cliente.Nombre;
            existente.Correo = cliente.Correo;
            existente.Telefono = cliente.Telefono;
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (GetPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError(string.Empty, "Ya existe un cliente con ese correo");
            return View("Formulario", cliente);
        }

        TempData["Mensaje"] = esNuevo
            ? "El cliente se creó correctamente."
            : "El cliente se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var cliente = await context.Clientes.FindAsync(id);
        if (cliente is null)
        {
            return NotFound(new { mensaje = "El cliente ya no existe." });
        }

        context.Clientes.Remove(cliente);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (GetPostgresException(exception)?.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Conflict(new
            {
                mensaje = "No se puede eliminar: el cliente tiene reservas relacionadas"
            });
        }

        return Json(new { mensaje = "El cliente se eliminó correctamente." });
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
