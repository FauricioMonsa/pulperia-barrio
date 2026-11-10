namespace PulperiaBarrio.Models;

public class ClienteReservasViewModel
{
    public string NombreCliente { get; set; } = string.Empty;

    public IReadOnlyList<ReservaClienteDetalleViewModel> Reservas { get; set; } = [];
}

public class ReservaClienteDetalleViewModel
{
    public string Producto { get; set; } = string.Empty;

    public string Sucursal { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public DateTimeOffset FechaReserva { get; set; }

    public DateTimeOffset FechaVencimiento { get; set; }

    public string Resultado { get; set; } = string.Empty;
}

public class ArticuloConsultaViewModel
{
    public int ProductoId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public decimal Precio { get; set; }

    public string? Atributos { get; set; }

    public IReadOnlyList<ExistenciaSucursalViewModel> Existencias { get; set; } = [];
}

public class ExistenciaSucursalViewModel
{
    public string NombreSucursal { get; set; } = string.Empty;

    public int CantidadDisponible { get; set; }

    public long CantidadReservada { get; set; }
}
