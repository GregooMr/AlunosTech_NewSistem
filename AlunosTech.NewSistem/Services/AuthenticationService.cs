using AlunosTech.NewSistem.Models;
using Microsoft.AspNetCore.Identity;

namespace AlunosTech.NewSistem.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly PasswordHasher<Aluno> _hasher = new();

    public string HashPassword(Aluno usuario, string senha) =>
        _hasher.HashPassword(usuario, senha);

    public bool VerifyPassword(Aluno usuario, string senha)
    {
        try
        {
            var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
            return resultado is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            // Hash inválido gravado no banco (ex.: valor de teste "HASH_SEGURO_*"):
            // trata como senha incorreta em vez de derrubar a requisição com erro 500.
            return false;
        }
    }
}
