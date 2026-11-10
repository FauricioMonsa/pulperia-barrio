using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PulperiaBarrio.Models;

[Table("cliente")]
public class Cliente
{
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Column("correo")]
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    public string Correo { get; set; } = string.Empty;

    [Column("telefono")]
    public string? Telefono { get; set; }

    [Column("baneado_hasta")]
    public DateTime? BaneadoHasta { get; set; }
}
