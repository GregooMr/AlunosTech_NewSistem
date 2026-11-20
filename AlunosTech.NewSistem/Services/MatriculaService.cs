using AlunosTech.NewSistem.Data;
using AlunosTech.NewSistem.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AlunosTech.NewSistem.Services;

public sealed class MatriculaService(
    AppDbContext context,
    IProgressoCalculator progresso,
    IExamService exams,
    ICertificateService certificates) : IMatriculaService
{
    private const string Ativa = "Ativa";
    private const string Trancada = "Trancada";
    private const string Concluida = "Concluida";
    private const string Cancelada = "Cancelada";

    public async Task<IReadOnlyList<Matricula>> ListarDoAlunoAsync(int alunoId) =>
        await context.Matriculas.AsNoTracking()
            .Include(m => m.Curso).ThenInclude(c => c.Professor)
            .Where(m => m.AlunoId == alunoId && m.Status != Cancelada)
            .OrderBy(m => m.Curso.Titulo)
            .ToListAsync();

    public async Task<ResultadoOperacao> AdicionarAsync(int alunoId, int cursoId)
    {
        if (!await context.Cursos.AnyAsync(c => c.Id == cursoId))
            return new(false, "Disciplina inválida.");

        var existente = await context.Matriculas.FirstOrDefaultAsync(m => m.AlunoId == alunoId && m.CursoId == cursoId && m.Status != Cancelada);
        if (existente is null)
        {
            context.Matriculas.Add(new Matricula { AlunoId = alunoId, CursoId = cursoId });
            await context.SaveChangesAsync();
            return new(true, "Matrícula realizada com sucesso!");
        }
        if (existente.Status == Trancada)
        {
            existente.Status = Ativa;
            await context.SaveChangesAsync();
            return new(true, "Matrícula reativada com sucesso!");
        }
        return new(false, existente.Status == Concluida
            ? "Você já concluiu esta disciplina."
            : "Você já está matriculado nesta disciplina!");
    }

    public async Task<ResultadoOperacao> AlterarStatusAsync(int alunoId, int matriculaId, string? status)
    {
        var matricula = await context.Matriculas.FirstOrDefaultAsync(m => m.Id == matriculaId && m.AlunoId == alunoId && m.Status != Cancelada);
        if (matricula is null) return new(false, "Matrícula não encontrada.");

        if (status == Trancada && matricula.Status == Ativa)
            matricula.Status = Trancada;
        else if (status == Ativa && matricula.Status == Trancada)
            matricula.Status = Ativa;
        else if (status == Cancelada && matricula.Status is Ativa or Trancada)
        {
            context.LogsAuditoria.Add(new LogAuditoria
            {
                TabelaAfetada = "Matriculas",
                Acao = "DESMATRICULA",
                Detalhes = $"Matrícula ID: {matricula.Id} (Aluno {matricula.AlunoId}, Curso {matricula.CursoId}) cancelada pelo aluno."
            });
            matricula.Status = Cancelada;
        }
        else
            return new(false, "Essa ação não está disponível para a situação atual da matrícula.");

        await context.SaveChangesAsync();
        return new(true, status switch
        {
            Trancada => "Matrícula trancada com sucesso.",
            Ativa => "Matrícula reativada com sucesso.",
            _ => "Desmatrícula realizada com auditoria."
        });
    }

    public async Task<ResultadoSala> ObterSalaAsync(int alunoId, int matriculaId)
    {
        var matricula = await context.Matriculas.AsNoTracking().Include(m => m.Curso)
            .FirstOrDefaultAsync(m => m.Id == matriculaId && m.AlunoId == alunoId && m.Status != Cancelada);
        if (matricula is null) return new(null, null);
        if (matricula.Status == Trancada)
            return new(null, "Esta matrícula está trancada. Reative-a para acessar a sala de aula.");

        var conteudos = await context.ConteudosDisciplina.AsNoTracking()
            .Where(c => c.CursoId == matricula.CursoId).ToListAsync();
        return new(new SalaDeAulaViewModel(matricula, progresso.Montar(conteudos, matricula.ProgressoPercentual)), null);
    }

    public async Task<ResultadoOperacao> ConcluirAtividadeAsync(int alunoId, int matriculaId, int conteudoId)
    {
        var matricula = await context.Matriculas.FirstOrDefaultAsync(m => m.Id == matriculaId && m.AlunoId == alunoId && m.Status != Cancelada);
        if (matricula is null) return new(false, "Matrícula não encontrada.");
        if (matricula.Status != Ativa) return new(false, "Só é possível avançar em matrículas ativas.");

        var conteudos = await context.ConteudosDisciplina.AsNoTracking()
            .Where(c => c.CursoId == matricula.CursoId).ToListAsync();
        var alvo = progresso.Montar(conteudos, matricula.ProgressoPercentual)
            .FirstOrDefault(a => a.Conteudo.Id == conteudoId);
        if (alvo is null) return new(false, "Conteúdo não encontrado.");
        if (alvo.Concluida) return new(false, "Este conteúdo já foi concluído.");
        if (!alvo.Proxima) return new(false, "Conclua os conteúdos anteriores primeiro.");

        matricula.ProgressoPercentual = Math.Clamp(alvo.Fim, 0, 100);
        await context.SaveChangesAsync();
        return new(true, string.Empty);
    }

    public async Task<ResultadoPaginaProva> ObterProvaAsync(int alunoId, int matriculaId)
    {
        var matricula = await context.Matriculas.AsNoTracking().Include(m => m.Curso)
            .FirstOrDefaultAsync(m => m.Id == matriculaId && m.AlunoId == alunoId && m.Status != Cancelada);
        if (matricula is null) return new(null, null, false);
        if (matricula.Status == Concluida && matricula.CodigoCertificado is not null)
            return new(null, null, true);
        if (matricula.Status != Ativa || matricula.ProgressoPercentual < 100)
            return new(null, "A prova só é liberada com a matrícula ativa e 100% de progresso.", false);

        return new(new ProvaViewModel(matricula, exams.GetQuestions(matricula.Curso), certificates.NotaMinima), null, false);
    }

    public async Task<ResultadoConclusaoProva> ConcluirProvaAsync(int alunoId, int matriculaId, IReadOnlyDictionary<int, int> respostas)
    {
        var matricula = await context.Matriculas.AsNoTracking().Include(m => m.Curso)
            .FirstOrDefaultAsync(m => m.Id == matriculaId && m.AlunoId == alunoId && m.Status != Cancelada);
        if (matricula is null || matricula.Status != Ativa || matricula.ProgressoPercentual < 100)
            return new(false, false, 0, "A prova não está disponível para esta matrícula.");

        var nota = exams.Grade(matricula.Curso, respostas);
        try
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp_ConcluirProvaEEmitirCertificado @MatriculaId = {matriculaId}, @NotaFinal = {nota}");
        }
        catch (SqlException ex) when (ex.Number is 50000 or 50001 or 50002 or 50003)
        {
            var aviso = ex.Number switch
            {
                50000 => "Matrícula não encontrada.",
                50001 => "Conclua 100% dos conteúdos antes de realizar a prova.",
                50002 => "A prova só está disponível para matrículas ativas.",
                _ => "A nota calculada está fora do intervalo permitido."
            };
            return new(false, false, nota, aviso);
        }

        var atualizada = await context.Matriculas.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == matriculaId && m.AlunoId == alunoId && m.Status != Cancelada);
        if (atualizada?.Status == Concluida && atualizada.CodigoCertificado is { } codigo)
        {
            context.LogsAuditoria.Add(new LogAuditoria
            {
                TabelaAfetada = "Matriculas",
                Acao = "EMISSAO_CERTIFICADO",
                Detalhes = $"Matrícula ID: {atualizada.Id}; Código: {codigo}; Nota: {atualizada.NotaFinal:0.00}"
            });
            await context.SaveChangesAsync();
            return new(true, true, nota, null);
        }

        return new(true, false, nota,
            $"Sua nota foi {nota:0.00}. É necessário atingir {certificates.NotaMinima:0.0} para emitir o certificado. Revise os conteúdos e tente novamente.");
    }

    public async Task<CertificateViewModel?> ObterCertificadoAsync(int alunoId, int matriculaId)
    {
        var matricula = await context.Matriculas.AsNoTracking()
            .Include(m => m.Curso).Include(m => m.Aluno)
            .FirstOrDefaultAsync(m => m.Id == matriculaId && m.AlunoId == alunoId && m.Status != Cancelada);
        if (matricula is null || matricula.Status != Concluida || matricula.CodigoCertificado is null || matricula.NotaFinal is null)
            return null;

        var codigo = matricula.CodigoCertificado.Value;
        var log = await context.LogsAuditoria.AsNoTracking()
            .Where(l => l.Acao == "EMISSAO_CERTIFICADO" && EF.Functions.Like(l.Detalhes!, $"%{codigo}%"))
            .OrderBy(l => l.DataLog)
            .FirstOrDefaultAsync();
        var certificate = certificates.Restore(matricula.NotaFinal.Value, codigo, log?.DataLog ?? matricula.DataMatricula);
        return new CertificateViewModel(matricula, certificate);
    }
}
