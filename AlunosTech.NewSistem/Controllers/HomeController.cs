using System.Diagnostics;
using System.Net.Mail;
using AlunosTech.NewSistem.Data;
using AlunosTech.NewSistem.Models;
using AlunosTech.NewSistem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlunosTech.NewSistem.Controllers;

public class HomeController : Controller
{
    private const string Ativa = "Ativa";
    private const string Trancada = "Trancada";
    private const string Concluida = "Concluida";
    private const string Cancelada = "Cancelada";

    private readonly AppDbContext _context;
    private readonly IAuthenticationService _authentication;
    private readonly ICertificateService _certificates;
    private readonly IExamService _exams;

    public HomeController(AppDbContext context, IAuthenticationService authentication,
        ICertificateService certificates, IExamService exams)
    {
        _context = context;
        _authentication = authentication;
        _certificates = certificates;
        _exams = exams;
    }

    private int? AlunoId => HttpContext.Session.GetInt32("AlunoId");
    private bool EhProfessor => HttpContext.Session.GetString("Perfil") == "Professor";

    private void IniciarSessao(Aluno usuario)
    {
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("AlunoId", usuario.Id);
        HttpContext.Session.SetString("AlunoNome", usuario.Nome);
        // RA é nulo para professores: SetString lança exceção com null, por isso o "?? string.Empty".
        HttpContext.Session.SetString("AlunoRA", usuario.RA ?? string.Empty);
        HttpContext.Session.SetString("Perfil", usuario.Perfil);
    }

    // ---------------------------------------------------------------- acesso

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        if (AlunoId is not null)
            return EhProfessor ? RedirectToAction("Index", "Professor") : RedirectToAction(nameof(Index));

        ViewBag.Cursos = await _context.Cursos.AsNoTracking().Include(c => c.Professor).OrderBy(c => c.Titulo).ToListAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Entrar(string? email, string? senha)
    {
        email = email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(senha))
        {
            TempData["Erro"] = "Informe e-mail e senha.";
            return RedirectToAction(nameof(Login));
        }

        var usuario = await _context.Alunos.FirstOrDefaultAsync(a => a.Email == email);
        if (usuario is null || !_authentication.VerifyPassword(usuario, senha))
        {
            TempData["Erro"] = "E-mail ou senha inválidos! (Teste com: carlos@alunostech.com / 123)";
            return RedirectToAction(nameof(Login));
        }

        IniciarSessao(usuario);
        return usuario.Perfil == "Professor" ? RedirectToAction("Index", "Professor") : RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CadastrarEMatricular(string? nome, string? email, string? senha, int cursoId)
    {
        nome = nome?.Trim();
        email = email?.Trim();

        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 3 || nome.Length > 255)
            return Falha("Informe seu nome completo.");
        if (string.IsNullOrWhiteSpace(email) || email.Length > 255 || !MailAddress.TryCreate(email, out _))
            return Falha("Informe um e-mail válido.");
        if (string.IsNullOrEmpty(senha) || senha.Length < 6)
            return Falha("A senha deve ter pelo menos 6 caracteres.");
        if (!await _context.Cursos.AnyAsync(c => c.Id == cursoId))
            return Falha("Selecione uma disciplina válida.");
        if (await _context.Alunos.AnyAsync(a => a.Email == email))
            return Falha("Este e-mail já está cadastrado no sistema!");

        string ra;
        do { ra = "2026" + Random.Shared.Next(100, 1000); }
        while (await _context.Alunos.AnyAsync(a => a.RA == ra));

        var novoAluno = new Aluno { Nome = nome, Email = email, RA = ra, Perfil = "Aluno" };
        novoAluno.SenhaHash = _authentication.HashPassword(novoAluno, senha);

        // Aluno e matrícula são gravados no mesmo SaveChanges (transação única).
        _context.Alunos.Add(novoAluno);
        _context.Matriculas.Add(new Matricula { Aluno = novoAluno, CursoId = cursoId });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Falha("Não foi possível concluir o cadastro. Tente novamente.");
        }

        IniciarSessao(novoAluno);
        return RedirectToAction(nameof(Index));

        IActionResult Falha(string mensagem)
        {
            TempData["Erro"] = mensagem;
            return RedirectToAction(nameof(Login));
        }
    }

    public IActionResult Sair()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    // ---------------------------------------------------------------- aluno: cursos

    public async Task<IActionResult> Index()
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));
        if (EhProfessor) return RedirectToAction("Index", "Professor");

        var matriculas = await _context.Matriculas
            .AsNoTracking()
            .Include(m => m.Curso).ThenInclude(c => c.Professor)
            .Where(m => m.AlunoId == AlunoId)
            .OrderBy(m => m.Curso.Titulo)
            .ToListAsync();

        var jaMatriculados = matriculas.Select(m => m.CursoId).ToHashSet();
        ViewBag.CursosDisponiveis = (await _context.Cursos.AsNoTracking().OrderBy(c => c.Titulo).ToListAsync())
            .Where(c => !jaMatriculados.Contains(c.Id))
            .ToList();

        return View(matriculas);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarCurso(int cursoId)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));
        if (EhProfessor) return RedirectToAction("Index", "Professor");

        if (!await _context.Cursos.AnyAsync(c => c.Id == cursoId))
        {
            TempData["Aviso"] = "Disciplina inválida.";
            return RedirectToAction(nameof(Index));
        }

        var existente = await _context.Matriculas.FirstOrDefaultAsync(m => m.AlunoId == AlunoId && m.CursoId == cursoId);
        if (existente is null)
        {
            _context.Matriculas.Add(new Matricula { AlunoId = AlunoId.Value, CursoId = cursoId });
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = "Matrícula realizada com sucesso!";
        }
        else if (existente.Status == Trancada)
        {
            existente.Status = Ativa;
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = "Matrícula reativada com sucesso!";
        }
        else if (existente.Status == Concluida)
        {
            TempData["Aviso"] = "Você já concluiu esta disciplina.";
        }
        else
        {
            TempData["Aviso"] = "Você já está matriculado nesta disciplina!";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarStatus(int matriculaId, string? status)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));

        var matricula = await _context.Matriculas.FirstOrDefaultAsync(m => m.Id == matriculaId && m.AlunoId == AlunoId);
        if (matricula is null) return RedirectToAction(nameof(Index));

        switch (status)
        {
            case Trancada when matricula.Status == Ativa:
                matricula.Status = Trancada;
                await _context.SaveChangesAsync();
                TempData["Sucesso"] = "Matrícula trancada com sucesso.";
                break;

            case Ativa when matricula.Status == Trancada:
                matricula.Status = Ativa;
                await _context.SaveChangesAsync();
                TempData["Sucesso"] = "Matrícula reativada com sucesso.";
                break;

            case Cancelada when matricula.Status is Ativa or Trancada:
                _context.LogsAuditoria.Add(new LogAuditoria
                {
                    TabelaAfetada = "Matriculas",
                    Acao = "DESMATRICULA",
                    Detalhes = $"Matrícula ID: {matricula.Id} (Aluno {matricula.AlunoId}, Curso {matricula.CursoId}) cancelada pelo aluno."
                });
                _context.Matriculas.Remove(matricula);
                await _context.SaveChangesAsync();
                TempData["Sucesso"] = "Desmatrícula realizada com auditoria.";
                break;

            default:
                TempData["Aviso"] = "Essa ação não está disponível para a situação atual da matrícula.";
                break;
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------- sala de aula

    public async Task<IActionResult> SalaDeAula(int id)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));

        var matricula = await _context.Matriculas
            .AsNoTracking()
            .Include(m => m.Curso)
            .FirstOrDefaultAsync(m => m.Id == id && m.AlunoId == AlunoId);
        if (matricula is null) return RedirectToAction(nameof(Index));

        if (matricula.Status == Trancada)
        {
            TempData["Aviso"] = "Esta matrícula está trancada. Reative-a para acessar a sala de aula.";
            return RedirectToAction(nameof(Index));
        }

        var conteudos = await _context.ConteudosDisciplina.AsNoTracking()
            .Where(c => c.CursoId == matricula.CursoId).ToListAsync();

        return View(new SalaDeAulaViewModel(matricula, ProgressoCalculator.Montar(conteudos, matricula.ProgressoPercentual)));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConcluirAtividade(int matriculaId, int conteudoId)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));

        var matricula = await _context.Matriculas.FirstOrDefaultAsync(m => m.Id == matriculaId && m.AlunoId == AlunoId);
        if (matricula is null) return RedirectToAction(nameof(Index));

        if (matricula.Status != Ativa)
        {
            TempData["Aviso"] = "Só é possível avançar em matrículas ativas.";
            return RedirectToAction(nameof(Index));
        }

        var conteudos = await _context.ConteudosDisciplina.AsNoTracking()
            .Where(c => c.CursoId == matricula.CursoId).ToListAsync();
        var alvo = ProgressoCalculator.Montar(conteudos, matricula.ProgressoPercentual)
            .FirstOrDefault(a => a.Conteudo.Id == conteudoId);

        if (alvo is null)
            TempData["Aviso"] = "Conteúdo não encontrado.";
        else if (alvo.Concluida)
            TempData["Aviso"] = "Este conteúdo já foi concluído.";
        else if (!alvo.Proxima)
            TempData["Aviso"] = "Conclua os conteúdos anteriores primeiro.";
        else
        {
            matricula.ProgressoPercentual = Math.Clamp(alvo.Fim, 0, 100);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(SalaDeAula), new { id = matriculaId });
    }

    // ---------------------------------------------------------------- prova e certificado

    [HttpGet]
    public async Task<IActionResult> Prova(int id)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));

        var matricula = await _context.Matriculas.AsNoTracking().Include(m => m.Curso)
            .FirstOrDefaultAsync(m => m.Id == id && m.AlunoId == AlunoId);
        if (matricula is null) return RedirectToAction(nameof(Index));

        if (matricula.Status == Concluida && matricula.CodigoCertificado is not null)
            return RedirectToAction(nameof(Certificado), new { id });

        if (matricula.Status != Ativa || matricula.ProgressoPercentual < 100)
        {
            TempData["Aviso"] = "A prova só é liberada com a matrícula ativa e 100% de progresso.";
            return RedirectToAction(nameof(Index));
        }

        return View(new ProvaViewModel(matricula, _exams.GetQuestions(matricula.Curso), _certificates.NotaMinima));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConcluirProva(int matriculaId, Dictionary<int, int>? respostas)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));

        var matricula = await _context.Matriculas.Include(m => m.Curso)
            .FirstOrDefaultAsync(m => m.Id == matriculaId && m.AlunoId == AlunoId);
        if (matricula is null) return RedirectToAction(nameof(Index));

        if (matricula.Status != Ativa || matricula.ProgressoPercentual < 100)
        {
            TempData["Aviso"] = "A prova não está disponível para esta matrícula.";
            return RedirectToAction(nameof(Index));
        }

        // A nota é calculada no servidor a partir das respostas; o aluno não informa a nota.
        var nota = _exams.Grade(matricula.Curso, respostas ?? new Dictionary<int, int>());
        var certificado = _certificates.Create(nota);
        matricula.NotaFinal = nota;

        if (!certificado.Aprovado)
        {
            await _context.SaveChangesAsync();
            TempData["Aviso"] = $"Sua nota foi {nota:0.00}. É necessário atingir {_certificates.NotaMinima:0.0} para emitir o certificado. Revise os conteúdos e tente novamente.";
            return RedirectToAction(nameof(SalaDeAula), new { id = matriculaId });
        }

        matricula.Status = Concluida;
        matricula.CodigoCertificado = certificado.Codigo;
        _context.LogsAuditoria.Add(new LogAuditoria
        {
            TabelaAfetada = "Matriculas",
            Acao = "EMISSAO_CERTIFICADO",
            Detalhes = $"Matrícula ID: {matricula.Id}; Código: {certificado.Codigo}; Nota: {nota:0.00}"
        });
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Certificado), new { id = matriculaId });
    }

    [HttpGet]
    public async Task<IActionResult> Certificado(int id)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));

        var matricula = await _context.Matriculas.AsNoTracking()
            .Include(m => m.Curso).Include(m => m.Aluno)
            .FirstOrDefaultAsync(m => m.Id == id && m.AlunoId == AlunoId);

        if (matricula is null || matricula.Status != Concluida || matricula.CodigoCertificado is null || matricula.NotaFinal is null)
        {
            TempData["Aviso"] = "Certificado indisponível para esta matrícula.";
            return RedirectToAction(nameof(Index));
        }

        var codigo = matricula.CodigoCertificado.Value;
        var padrao = $"%{codigo}%";
        var log = await _context.LogsAuditoria.AsNoTracking()
            .Where(l => l.Acao == "EMISSAO_CERTIFICADO" && EF.Functions.Like(l.Detalhes!, padrao))
            .OrderBy(l => l.DataLog)
            .FirstOrDefaultAsync();

        var certificado = _certificates.Restore(matricula.NotaFinal.Value, codigo, log?.DataLog ?? matricula.DataMatricula);
        return View(new CertificateViewModel(matricula, certificado));
    }

    // ---------------------------------------------------------------- áreas de apoio

    public IActionResult Bibliotecas() => AlunoId is null ? RedirectToAction(nameof(Login)) : View();

    public IActionResult MuralDoAluno() => AlunoId is null ? RedirectToAction(nameof(Login)) : View();

    public IActionResult Tutoriais() => AlunoId is null ? RedirectToAction(nameof(Login)) : View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
