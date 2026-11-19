using System.ComponentModel.DataAnnotations.Schema;

namespace AlunosTech.NewSistem.Models;

[Table("Matriculas", Schema = "dbo")]
public class Matricula
{
    public int Id { get; set; }
    public int AlunoId { get; set; }
    public int CursoId { get; set; }
    public int ProgressoPercentual { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal? NotaFinal { get; set; }
    [Column(TypeName = "varchar(50)")]
    public string Status { get; set; } = "Ativa";
    public Guid? CodigoCertificado { get; set; }
    public DateTime DataMatricula { get; set; } = DateTime.UtcNow;
    public Aluno Aluno { get; set; } = null!;
    public Curso Curso { get; set; } = null!;
}
