namespace PulperiaBarrio.Models;

public class ProductoInventarioView
{
    public int ProductoId { get; set; }

    public string Producto { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public decimal Precio { get; set; }

    public int MaxCantidadReserva { get; set; }

    public int MaxHorasReserva { get; set; }

    public string? Atributos { get; set; }

    public int SucursalId { get; set; }

    public string Sucursal { get; set; } = string.Empty;

    public int CantidadDisponible { get; set; }

    public long CantidadReservada { get; set; }

    public int ExistenciaMinima { get; set; }
}
