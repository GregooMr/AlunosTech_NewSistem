using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlunosTech.NewSistem.Models;

[Table("ConteudosDisciplina", Schema = "dbo")]
public class ConteudoDisciplina
{
    public int Id { get; set; }
    public int CursoId { get; set; }
    [Required, MaxLength(255)]
    public string Titulo { get; set; } = string.Empty;
    [Required, MaxLength(50)]
    public string TipoMaterial { get; set; } = "Material";
    public string? Descricao { get; set; }
    [MaxLength(2000)]
    public string? ComentarioProfessor { get; set; }
    [MaxLength(255)]
    public string? NomeArquivo { get; set; }
    [MaxLength(500)]
    public string? UrlArquivo { get; set; }
    public DateTime? DataUpload { get; set; }
    public int PesoPercentual { get; set; }
    public Curso Curso { get; set; } = null!;
}