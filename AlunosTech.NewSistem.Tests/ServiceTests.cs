using AlunosTech.NewSistem.Data;
using AlunosTech.NewSistem.Models;
using AlunosTech.NewSistem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlunosTech.NewSistem.Tests;

public sealed class ProgressoCalculatorTests
{
    private readonly ProgressoCalculator _calculator = new();

    [Fact]
    public void Montar_SomaPesosEOrdemSequencial()
    {
        var itens = _calculator.Montar([Conteudo(1, 20), Conteudo(2, 30), Conteudo(3, 50)], 20);

        Assert.Equal(new[] { 20, 50, 100 }, itens.Select(i => i.Fim));
        Assert.True(itens[0].Concluida);
        Assert.True(itens[1].Proxima);
        Assert.False(itens[2].Concluida);
    }

    [Fact]
    public void Montar_GaranteQueUltimaEtapaTermineEmCemPorCento()
    {
        var itens = _calculator.Montar([Conteudo(1, 40), Conteudo(2, 40), Conteudo(3, 40)], 0);

        Assert.Equal(100, itens[^1].Fim);
    }

    [Fact]
    public void Montar_DistribuiEtapasComPesosZerados()
    {
        var itens = _calculator.Montar([Conteudo(1, 0), Conteudo(2, 0), Conteudo(3, 0)], 0);

        Assert.Equal(new[] { 33, 67, 100 }, itens.Select(i => i.Fim));
    }

    private static ConteudoDisciplina Conteudo(int id, int peso) => new() { Id = id, PesoPercentual = peso };
}

public sealed class ExamServiceTests
{
    private readonly ExamService _service = new();
    private readonly Curso _curso = new() { Codigo = "8125-60" };

    [Fact]
    public void Grade_TodasCorretas_RetornaDez() =>
        Assert.Equal(10m, _service.Grade(_curso, new Dictionary<int, int> { [1] = 0, [2] = 2, [3] = 1, [4] = 3, [5] = 1 }));

    [Fact]
    public void Grade_TodasErradas_RetornaZero() =>
        Assert.Equal(0m, _service.Grade(_curso, new Dictionary<int, int> { [1] = 3, [2] = 0, [3] = 0, [4] = 0, [5] = 0 }));

    [Fact]
    public void Grade_RespostasAusentesContamComoErradas() =>
        Assert.Equal(0m, _service.Grade(_curso, new Dictionary<int, int>()));
}

public sealed class CertificateServiceTests
{
    private readonly CertificateService _service = new();

    [Fact]
    public void Create_AprovacaoNoLimite_GeraCodigo() =>
        Assert.True(_service.Create(7m).Aprovado);

    [Fact]
    public void Create_NotaAbaixoDoLimite_NaoGeraCodigo()
    {
        var resultado = _service.Create(6.99m);
        Assert.False(resultado.Aprovado);
        Assert.Null(resultado.Codigo);
    }
}

public sealed class AuthenticationServiceTests
{
    [Fact]
    public void HashPassword_GeraHashVerificavelERejeitaSenhaIncorreta()
    {
        using var context = CriarContexto();
        var service = new AuthenticationService(context);
        var aluno = new Aluno { Nome = "Aluno Teste", Email = "aluno@example.test" };

        aluno.SenhaHash = service.HashPassword(aluno, "Senha!123");

        Assert.NotEqual("Senha!123", aluno.SenhaHash);
        Assert.True(service.VerifyPassword(aluno, "Senha!123"));
        Assert.False(service.VerifyPassword(aluno, "SenhaErrada!123"));
    }

    [Fact]
    public void VerifyPassword_HashInvalido_RetornaFalse()
    {
        using var context = CriarContexto();
        var service = new AuthenticationService(context);
        var aluno = new Aluno { SenhaHash = "hash-invalido" };

        Assert.False(service.VerifyPassword(aluno, "Senha!123"));
    }

    private static AppDbContext CriarContexto() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
}

public sealed class MatriculaServiceTests
{
    [Fact]
    public async Task AdicionarAsync_BloqueiaMatriculaAtivaDuplicada()
    {
        await using var context = await CriarContextoAsync();
        var (service, aluno, curso) = Preparar(context);

        Assert.True((await service.AdicionarAsync(aluno.Id, curso.Id)).Sucesso);
        var repetida = await service.AdicionarAsync(aluno.Id, curso.Id);

        Assert.False(repetida.Sucesso);
        Assert.Single(await service.ListarDoAlunoAsync(aluno.Id));
    }

    [Fact]
    public async Task AlterarStatusAsync_CancelarPermiteNovaMatricula()
    {
        await using var context = await CriarContextoAsync();
        var (service, aluno, curso) = Preparar(context);
        await service.AdicionarAsync(aluno.Id, curso.Id);
        var original = await context.Matriculas.SingleAsync();

        var cancelamento = await service.AlterarStatusAsync(aluno.Id, original.Id, "Cancelada");
        var rematricula = await service.AdicionarAsync(aluno.Id, curso.Id);

        Assert.True(cancelamento.Sucesso);
        Assert.True(rematricula.Sucesso);
        Assert.Equal(2, await context.Matriculas.CountAsync());
        Assert.Single(await service.ListarDoAlunoAsync(aluno.Id));
    }

    [Theory]
    [InlineData(99, false)]
    [InlineData(100, true)]
    public async Task ObterProvaAsync_ExigeProgressoTotal(int progresso, bool liberada)
    {
        await using var context = await CriarContextoAsync();
        var (service, aluno, curso) = Preparar(context);
        var matricula = new Matricula { AlunoId = aluno.Id, CursoId = curso.Id, ProgressoPercentual = progresso, Status = "Ativa" };
        context.Matriculas.Add(matricula);
        await context.SaveChangesAsync();

        var resultado = await service.ObterProvaAsync(aluno.Id, matricula.Id);

        Assert.Equal(liberada, resultado.ViewModel is not null);
    }

    private static (MatriculaService Service, Aluno Aluno, Curso Curso) Preparar(AppDbContext context)
    {
        var aluno = new Aluno { Nome = "Aluno Teste", Email = "aluno@example.test", Perfil = "Aluno", DataCadastro = DateTime.UtcNow };
        var curso = new Curso { Codigo = "TESTE", Titulo = "Curso Teste" };
        context.AddRange(aluno, curso);
        context.SaveChanges();
        var service = new MatriculaService(context, new ProgressoCalculator(), new ExamService(), new CertificateService());
        return (service, aluno, curso);
    }

    private static async Task<AppDbContext> CriarContextoAsync()
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}
