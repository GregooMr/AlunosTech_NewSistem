using AlunosTech.NewSistem.Models;

namespace AlunosTech.NewSistem.Services;

public interface IAuthenticationService
{
    string HashPassword(Aluno usuario, string senha);
    bool VerifyPassword(Aluno usuario, string senha);
}
