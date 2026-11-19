using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlunosTech.NewSistem.Models;

[Table("Cursos", Schema = "dbo")]
public class Curso
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Titulo { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public int? ProfessorId { get; set; }
    public Aluno? Professor { get; set; }
    public ICollection<ConteudoDisciplina> Conteudos { get; set; } = [];
}
