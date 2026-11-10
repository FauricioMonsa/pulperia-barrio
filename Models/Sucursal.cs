using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PulperiaBarrio.Models;

[Table("sucursal")]
public class Sucursal
{
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Column("ubicacion")]
    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    public string Ubicacion { get; set; } = string.Empty;

    [Column("activa")]
    public bool Activa { get; set; }
}
