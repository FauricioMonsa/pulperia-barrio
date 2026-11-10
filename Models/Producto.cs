using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PulperiaBarrio.Models;

[Table("producto")]
public class Producto
{
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Column("categoria")]
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public string Categoria { get; set; } = string.Empty;

    [Column("precio")]
    [Range(typeof(decimal), "0.0000000000000000000000000001", "79228162514264337593543950335",
        ErrorMessage = "El precio debe ser mayor que 0.")]
    public decimal Precio { get; set; }

    [Column("max_cantidad_reserva")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad máxima por cliente debe ser mayor que 0.")]
    public int MaxCantidadReserva { get; set; }

    [Column("max_horas_reserva")]
    [Range(1, int.MaxValue, ErrorMessage = "Las horas máximas de reserva deben ser mayores que 0.")]
    public int MaxHorasReserva { get; set; }

    [Column("atributos")]
    public string Atributos { get; set; } = "{}";
}
