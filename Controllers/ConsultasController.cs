using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulperiaBarrio.Data;
using PulperiaBarrio.Models;

namespace PulperiaBarrio.Controllers;

public class ConsultasController(PulperiaBarrioContext context) : Controller
{
    private static readonly TimeSpan CostaRicaOffset = TimeSpan.FromHours(-6);

    [HttpGet]
    public async Task<IActionResult> Clientes(string? busqueda)
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
    public async Task<IActionResult> DetalleReservas(int clienteId)
    {
        var cliente = await context.Clientes.AsNoTracking()
            .Where(item => item.Id == clienteId)
            .Select(item => item.Nombre)
            .SingleOrDefaultAsync();
        if (cliente is null)
        {
            return NotFound();
        }

        var reservas = await context.ReservasClientes.AsNoTracking()
            .Where(reserva => reserva.ClienteId == clienteId)
            .OrderByDescending(reserva => reserva.FechaReserva)
            .Select(reserva => new ReservaClienteDetalleViewModel
            {
                Producto = reserva.Producto,
                Sucursal = reserva.Sucursal,
                Cantidad = reserva.Cantidad,
                FechaReserva = new DateTimeOffset(
                    DateTime.SpecifyKind(reserva.FechaReserva, DateTimeKind.Utc)).ToOffset(CostaRicaOffset),
                FechaVencimiento = new DateTimeOffset(
                    DateTime.SpecifyKind(reserva.FechaVencimiento, DateTimeKind.Utc)).ToOffset(CostaRicaOffset),
                Resultado = reserva.Resultado
            })
            .ToListAsync();

        return PartialView("_DetalleReservas", new ClienteReservasViewModel
        {
            NombreCliente = cliente,
            Reservas = reservas
        });
    }

    [HttpGet]
    public async Task<IActionResult> Articulos(string? busqueda)
    {
        var articulos = context.ProductosInventario.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            articulos = articulos.Where(producto =>
                EF.Functions.ILike(producto.Producto, $"%{busqueda}%"));
        }

        var filas = await articulos
            .OrderBy(producto => producto.Producto)
            .ThenBy(producto => producto.Sucursal)
            .ToListAsync();
        var modelo = filas
            .GroupBy(producto => producto.ProductoId)
            .Select(grupo =>
            {
                var producto = grupo.First();
                return new ArticuloConsultaViewModel
                {
                    ProductoId = producto.ProductoId,
                    Nombre = producto.Producto,
                    Categoria = producto.Categoria,
                    Precio = producto.Precio,
                    Atributos = producto.Atributos,
                    Existencias = grupo.Select(fila => new ExistenciaSucursalViewModel
                    {
                        NombreSucursal = fila.Sucursal,
                        CantidadDisponible = fila.CantidadDisponible,
                        CantidadReservada = fila.CantidadReservada
                    }).ToList()
                };
            })
            .ToList();

        ViewData["Busqueda"] = busqueda;
        ViewData["FormatoColones"] = new NumberFormatInfo
        {
            NumberGroupSeparator = " ",
            NumberDecimalSeparator = ",",
            NumberGroupSizes = [3]
        };
        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Sucursales()
    {
        return View(await context.Sucursales.AsNoTracking()
            .OrderBy(sucursal => sucursal.Nombre)
            .ToListAsync());
    }
}
