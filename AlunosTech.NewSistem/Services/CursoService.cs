using AlunosTech.NewSistem.Data;
using AlunosTech.NewSistem.Models;
using Microsoft.EntityFrameworkCore;

namespace AlunosTech.NewSistem.Services;

public sealed class CursoService(AppDbContext context) : ICursoService
{
    public async Task<IReadOnlyList<Curso>> ListarTodosAsync() =>
        await context.Cursos.AsNoTracking().Include(c => c.Professor).OrderBy(c => c.Titulo).ToListAsync();

    public async Task<IReadOnlyList<Curso>> ListarDisponiveisAsync(int alunoId)
    {
        var matriculados = context.Matriculas.Where(m => m.AlunoId == alunoId && m.Status != "Cancelada").Select(m => m.CursoId);
        return await context.Cursos.AsNoTracking().Where(c => !matriculados.Contains(c.Id)).OrderBy(c => c.Titulo).ToListAsync();
    }

    public async Task<IReadOnlyList<Curso>> ListarMatriculadosAsync(int alunoId) =>
        await context.Matriculas.AsNoTracking()
            .Where(m => m.AlunoId == alunoId && m.Status != "Cancelada")
            .Select(m => m.Curso)
            .OrderBy(c => c.Titulo)
            .ToListAsync();

    public async Task<IReadOnlyList<Curso>> ListarDoProfessorAsync(int professorId) =>
        await context.Cursos.AsNoTracking().Include(c => c.Conteudos)
            .Where(c => c.ProfessorId == professorId)
            .OrderBy(c => c.Titulo)
            .ToListAsync();

    public Task<bool> ExisteAsync(int cursoId) => context.Cursos.AnyAsync(c => c.Id == cursoId);

    public Task<bool> PertenceAoProfessorAsync(int cursoId, int professorId) =>
        context.Cursos.AnyAsync(c => c.Id == cursoId && c.ProfessorId == professorId);
}
