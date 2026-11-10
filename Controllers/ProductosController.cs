using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PulperiaBarrio.Data;
using PulperiaBarrio.Models;

namespace PulperiaBarrio.Controllers;

public class ProductosController(PulperiaBarrioContext context) : Controller
{
    private const string ErrorAtributos =
        "Los atributos deben ser un objeto JSON válido, por ejemplo {\"color\":\"azul\"}";

    [HttpGet]
    public async Task<IActionResult> Index(string? busqueda)
    {
        var productos = context.Productos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            productos = productos.Where(producto =>
                EF.Functions.ILike(producto.Nombre, $"%{busqueda}%"));
        }

        ViewData["Busqueda"] = busqueda;
        return View(await productos.OrderBy(producto => producto.Nombre).ToListAsync());
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View("Formulario", new ProductoFormularioViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var producto = await context.Productos.FindAsync(id);
        if (producto is null)
        {
            return NotFound();
        }

        return View("Formulario", CrearViewModel(producto));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(ProductoFormularioViewModel modelo)
    {
        if (string.IsNullOrWhiteSpace(modelo.Atributos))
        {
            modelo.Atributos = "{}";
        }
        else if (!EsObjetoJson(modelo.Atributos))
        {
            ModelState.AddModelError(nameof(modelo.Atributos), ErrorAtributos);
        }

        if (!decimal.TryParse(
                modelo.Precio,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var precio) ||
            precio <= 0)
        {
            ModelState.AddModelError(nameof(modelo.Precio), "El precio debe ser un número mayor que 0.");
        }

        if (!ModelState.IsValid)
        {
            return View("Formulario", modelo);
        }

        var esNuevo = modelo.Id == 0;
        Producto producto;
        if (esNuevo)
        {
            producto = new Producto();
            context.Productos.Add(producto);
        }
        else
        {
            var existente = await context.Productos.FindAsync(modelo.Id);
            if (existente is null)
            {
                return NotFound();
            }

            producto = existente;
        }

        producto.Nombre = modelo.Nombre;
        producto.Categoria = modelo.Categoria;
        producto.Precio = precio;
        producto.MaxCantidadReserva = modelo.MaxCantidadReserva;
        producto.MaxHorasReserva = modelo.MaxHorasReserva;
        producto.Atributos = modelo.Atributos;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (GetPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError(string.Empty, "Ya existe un producto con ese nombre");
            return View("Formulario", modelo);
        }

        TempData["Mensaje"] = esNuevo
            ? "El producto se creó correctamente."
            : "El producto se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var producto = await context.Productos.FindAsync(id);
        if (producto is null)
        {
            return NotFound(new { mensaje = "El producto ya no existe." });
        }

        context.Productos.Remove(producto);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (GetPostgresException(exception)?.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Conflict(new
            {
                mensaje = "No se puede eliminar: el producto tiene inventario o reservas relacionados"
            });
        }

        return Json(new { mensaje = "El producto se eliminó correctamente." });
    }

    private static ProductoFormularioViewModel CrearViewModel(Producto producto)
    {
        return new ProductoFormularioViewModel
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            Categoria = producto.Categoria,
            Precio = producto.Precio.ToString(CultureInfo.InvariantCulture),
            MaxCantidadReserva = producto.MaxCantidadReserva,
            MaxHorasReserva = producto.MaxHorasReserva,
            Atributos = producto.Atributos
        };
    }

    private static bool EsObjetoJson(string json)
    {
        try
        {
            using var documento = JsonDocument.Parse(json);
            return documento.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
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
