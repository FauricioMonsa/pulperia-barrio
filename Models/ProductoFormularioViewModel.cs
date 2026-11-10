using System.ComponentModel.DataAnnotations;

namespace PulperiaBarrio.Models;

public class ProductoFormularioViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public string Categoria { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [RegularExpression(@"^\d+(?:\.\d+)?$", ErrorMessage = "Ingresa un precio válido usando punto decimal.")]
    public string Precio { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad máxima por cliente debe ser mayor que 0.")]
    public int MaxCantidadReserva { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Las horas máximas de reserva deben ser mayores que 0.")]
    public int MaxHorasReserva { get; set; }

    public string Atributos { get; set; } = "{}";
}
