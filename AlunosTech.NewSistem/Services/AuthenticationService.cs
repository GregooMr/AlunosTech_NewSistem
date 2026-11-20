using System.Net.Mail;
using AlunosTech.NewSistem.Data;
using AlunosTech.NewSistem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AlunosTech.NewSistem.Services;

public sealed class AuthenticationService(AppDbContext context) : IAuthenticationService
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

    public async Task<Aluno?> AutenticarAsync(string email, string senha)
    {
        var usuario = await context.Alunos.FirstOrDefaultAsync(a => a.Email == email.Trim());
        return usuario is not null && VerifyPassword(usuario, senha) ? usuario : null;
    }

    public async Task<ResultadoCadastro> CadastrarAlunoAsync(string? nome, string? email, string? senha, int cursoId)
    {
        nome = nome?.Trim();
        email = email?.Trim();

        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 3 || nome.Length > 255)
            return new(null, "Informe seu nome completo.");
        if (string.IsNullOrWhiteSpace(email) || email.Length > 255 || !MailAddress.TryCreate(email, out _))
            return new(null, "Informe um e-mail válido.");
        if (string.IsNullOrEmpty(senha) || senha.Length < 6)
            return new(null, "A senha deve ter pelo menos 6 caracteres.");
        if (!await context.Cursos.AnyAsync(c => c.Id == cursoId))
            return new(null, "Selecione uma disciplina válida.");
        if (await context.Alunos.AnyAsync(a => a.Email == email))
            return new(null, "Este e-mail já está cadastrado no sistema!");

        string ra;
        do { ra = "2026" + Random.Shared.Next(100, 1000); }
        while (await context.Alunos.AnyAsync(a => a.RA == ra));

        var usuario = new Aluno { Nome = nome, Email = email, RA = ra, Perfil = "Aluno" };
        usuario.SenhaHash = HashPassword(usuario, senha);
        context.Alunos.Add(usuario);
        context.Matriculas.Add(new Matricula { Aluno = usuario, CursoId = cursoId });

        try
        {
            await context.SaveChangesAsync();
            return new(usuario, null);
        }
        catch (DbUpdateException)
        {
            return new(null, "Não foi possível concluir o cadastro. Tente novamente.");
        }
    }
}
