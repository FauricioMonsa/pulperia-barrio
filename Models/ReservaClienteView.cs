namespace PulperiaBarrio.Models;

public class ReservaClienteView
{
    public int ReservaId { get; set; }

    public int ClienteId { get; set; }

    public string Cliente { get; set; } = string.Empty;

    public string Sucursal { get; set; } = string.Empty;

    public string Producto { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public DateTime FechaReserva { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public string Resultado { get; set; } = string.Empty;
}
