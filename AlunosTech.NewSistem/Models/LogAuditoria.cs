using System.ComponentModel.DataAnnotations.Schema;

namespace AlunosTech.NewSistem.Models;

[Table("LogsAuditoria", Schema = "dbo")]
public class LogAuditoria
{
    public int Id { get; set; }
    public string? TabelaAfetada { get; set; }
    public string? Acao { get; set; }
    public string? Detalhes { get; set; }
    public DateTime DataLog { get; set; } = DateTime.UtcNow;
}