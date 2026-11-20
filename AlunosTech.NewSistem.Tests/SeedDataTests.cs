using AlunosTech.NewSistem.Data;
using AlunosTech.NewSistem.Models;
using AlunosTech.NewSistem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlunosTech.NewSistem.Tests;

public sealed class SeedDataTests
{
    [Fact]
    public void Initialize_ConverteMarcadoresDeSenhaDeTodosOsUsuariosDemo()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var auth = new AuthenticationService(context);
        var emails = new[]
        {
            "lorandi@alunostech.com",
            "mariana@alunostech.com",
            "rafael@alunostech.com",
            "camila@alunostech.com",
            "carlos@alunostech.com"
        };

        foreach (var email in emails)
        {
            context.Alunos.Add(new Aluno
            {
                Nome = email,
                Email = email,
                Perfil = email == "carlos@alunostech.com" ? "Aluno" : "Professor",
                SenhaHash = $"HASH_SEGURO_{Guid.NewGuid():N}"
            });
        }
        context.SaveChanges();

        SeedData.Initialize(context, auth);

        foreach (var email in emails)
        {
            var usuario = context.Alunos.Single(aluno => aluno.Email == email);
            Assert.True(auth.VerifyPassword(usuario, "123"), $"A senha demo não foi preparada para {email}.");
        }
    }
}
