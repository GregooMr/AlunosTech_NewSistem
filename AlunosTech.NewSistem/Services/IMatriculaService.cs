using AlunosTech.NewSistem.Models;

namespace AlunosTech.NewSistem.Services;

public sealed record ResultadoOperacao(bool Sucesso, string Mensagem);
public sealed record ResultadoSala(SalaDeAulaViewModel? ViewModel, string? Aviso);
public sealed record ResultadoPaginaProva(ProvaViewModel? ViewModel, string? Aviso, bool CertificadoEmitido);
public sealed record ResultadoConclusaoProva(bool Disponivel, bool Aprovado, decimal Nota, string? Aviso);

public interface IMatriculaService
{
    Task<IReadOnlyList<Matricula>> ListarDoAlunoAsync(int alunoId);
    Task<ResultadoOperacao> AdicionarAsync(int alunoId, int cursoId);
    Task<ResultadoOperacao> AlterarStatusAsync(int alunoId, int matriculaId, string? status);
    Task<ResultadoSala> ObterSalaAsync(int alunoId, int matriculaId);
    Task<ResultadoOperacao> ConcluirAtividadeAsync(int alunoId, int matriculaId, int conteudoId);
    Task<ResultadoPaginaProva> ObterProvaAsync(int alunoId, int matriculaId);
    Task<ResultadoConclusaoProva> ConcluirProvaAsync(int alunoId, int matriculaId, IReadOnlyDictionary<int, int> respostas);
    Task<CertificateViewModel?> ObterCertificadoAsync(int alunoId, int matriculaId);
}
