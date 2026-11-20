using AlunosTech.NewSistem.Models;

namespace AlunosTech.NewSistem.Services;

public interface IAuthenticationService
{
    string HashPassword(Aluno usuario, string senha);
    bool VerifyPassword(Aluno usuario, string senha);
    Task<Aluno?> AutenticarAsync(string email, string senha);
    Task<ResultadoCadastro> CadastrarAlunoAsync(string? nome, string? email, string? senha, int cursoId);
}

public sealed record ResultadoCadastro(Aluno? Usuario, string? Erro);
