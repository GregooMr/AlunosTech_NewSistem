using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlunosTech.NewSistem.Models;

[Table("Usuarios", Schema = "dbo")]
public class Aluno
{
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string SenhaHash { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? RA { get; set; }

    [Required, MaxLength(50)]
    public string Perfil { get; set; } = "Aluno";

    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
}
