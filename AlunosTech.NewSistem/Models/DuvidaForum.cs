using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlunosTech.NewSistem.Models;

[Table("DuvidasForum", Schema = "dbo")]
public class DuvidaForum
{
    public int Id { get; set; }
    public int CursoId { get; set; }
    public int AlunoId { get; set; }
    [Required]
    public string Pergunta { get; set; } = string.Empty;
    public string? RespostaProfessor { get; set; }
    public DateTime DataPergunta { get; set; } = DateTime.UtcNow;
    public DateTime? DataResposta { get; set; }
    public Curso Curso { get; set; } = null!;
    public Aluno Aluno { get; set; } = null!;
}