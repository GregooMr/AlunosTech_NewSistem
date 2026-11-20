using AlunosTech.NewSistem.Models;

namespace AlunosTech.NewSistem.Services;

public interface ICursoService
{
    Task<IReadOnlyList<Curso>> ListarTodosAsync();
    Task<IReadOnlyList<Curso>> ListarDisponiveisAsync(int alunoId);
    Task<IReadOnlyList<Curso>> ListarMatriculadosAsync(int alunoId);
    Task<IReadOnlyList<Curso>> ListarDoProfessorAsync(int professorId);
    Task<bool> ExisteAsync(int cursoId);
    Task<bool> PertenceAoProfessorAsync(int cursoId, int professorId);
}
