using AlunosTech.NewSistem.Data;
using AlunosTech.NewSistem.Models;
using Microsoft.EntityFrameworkCore;

namespace AlunosTech.NewSistem.Services;

public interface IForumService
{
    /// <param name="alunoId">Quando informado, lista apenas as dúvidas desse aluno.</param>
    /// <param name="professorId">Quando informado, lista apenas dúvidas das disciplinas desse professor.</param>
    Task<IReadOnlyList<DuvidaForum>> ListAsync(int? cursoId = null, int? alunoId = null, int? professorId = null);
    Task<bool> AddQuestionAsync(int alunoId, int cursoId, string pergunta);
    Task<bool> AnswerAsync(int id, int professorId, string resposta);
}

public sealed class ForumService(AppDbContext context) : IForumService
{
    public async Task<IReadOnlyList<DuvidaForum>> ListAsync(int? cursoId = null, int? alunoId = null, int? professorId = null)
    {
        var query = context.DuvidasForum
            .Include(d => d.Curso)
            .Include(d => d.Aluno)
            .AsNoTracking();

        if (cursoId.HasValue) query = query.Where(d => d.CursoId == cursoId.Value);
        if (alunoId.HasValue) query = query.Where(d => d.AlunoId == alunoId.Value);
        if (professorId.HasValue) query = query.Where(d => d.Curso.ProfessorId == professorId.Value);

        return await query.OrderByDescending(d => d.DataPergunta).ToListAsync();
    }

    public async Task<bool> AddQuestionAsync(int alunoId, int cursoId, string pergunta)
    {
        // Só quem está matriculado na disciplina pode perguntar nela.
        var matriculado = await context.Matriculas.AnyAsync(m => m.AlunoId == alunoId && m.CursoId == cursoId);
        if (!matriculado) return false;

        context.DuvidasForum.Add(new DuvidaForum { AlunoId = alunoId, CursoId = cursoId, Pergunta = pergunta.Trim() });
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AnswerAsync(int id, int professorId, string resposta)
    {
        // Só o professor responsável pela disciplina pode responder.
        var duvida = await context.DuvidasForum
            .Include(d => d.Curso)
            .FirstOrDefaultAsync(d => d.Id == id && d.Curso.ProfessorId == professorId);
        if (duvida is null) return false;

        duvida.RespostaProfessor = resposta.Trim();
        duvida.DataResposta = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }
}
