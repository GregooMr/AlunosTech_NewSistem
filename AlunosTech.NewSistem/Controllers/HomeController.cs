using System.Diagnostics;
using System.Security.Claims;
using AlunosTech.NewSistem.Models;
using AlunosTech.NewSistem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ILoginService = AlunosTech.NewSistem.Services.IAuthenticationService;

namespace AlunosTech.NewSistem.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILoginService _authentication;
    private readonly ICursoService _cursos;
    private readonly IMatriculaService _matriculas;

    public HomeController(ILoginService authentication, ICursoService cursos, IMatriculaService matriculas)
    {
        _authentication = authentication;
        _cursos = cursos;
        _matriculas = matriculas;
    }

    private int? AlunoId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private bool EhProfessor => User.IsInRole("Professor");

    private async Task IniciarSessaoAsync(Aluno usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Role, usuario.Perfil)
        };
        if (!string.IsNullOrWhiteSpace(usuario.RA)) claims.Add(new Claim("RA", usuario.RA));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    // ---------------------------------------------------------------- acesso

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Login()
    {
        if (AlunoId is not null)
            return EhProfessor ? RedirectToAction("Index", "Professor") : RedirectToAction(nameof(Index));

        ViewBag.Cursos = await _cursos.ListarTodosAsync();
        return View();
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("login")]
    public async Task<IActionResult> Entrar(string? email, string? senha)
    {
        email = email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(senha))
        {
            TempData["Erro"] = "Informe e-mail e senha.";
            return RedirectToAction(nameof(Login));
        }

        var usuario = await _authentication.AutenticarAsync(email, senha);
        if (usuario is null)
        {
            TempData["Erro"] = "E-mail ou senha inválidos.";
            return RedirectToAction(nameof(Login));
        }

        await IniciarSessaoAsync(usuario);
        return usuario.Perfil == "Professor" ? RedirectToAction("Index", "Professor") : RedirectToAction(nameof(Index));
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CadastrarEMatricular(string? nome, string? email, string? senha, int cursoId)
    {
        var resultado = await _authentication.CadastrarAlunoAsync(nome, email, senha, cursoId);
        if (resultado.Usuario is null)
        {
            TempData["Erro"] = resultado.Erro;
            return RedirectToAction(nameof(Login));
        }

        await IniciarSessaoAsync(resultado.Usuario);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult AcessoNegado() => StatusCode(StatusCodes.Status403Forbidden);

    // ---------------------------------------------------------------- aluno: cursos

    [Authorize(Roles = "Aluno")]
    public async Task<IActionResult> Index()
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));
        if (EhProfessor) return RedirectToAction("Index", "Professor");

        var matriculas = await _matriculas.ListarDoAlunoAsync(AlunoId.Value);
        ViewBag.CursosDisponiveis = await _cursos.ListarDisponiveisAsync(AlunoId.Value);

        return View(matriculas);
    }

    [Authorize(Roles = "Aluno"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarCurso(int cursoId)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));
        if (EhProfessor) return RedirectToAction("Index", "Professor");

        var resultado = await _matriculas.AdicionarAsync(AlunoId.Value, cursoId);
        TempData[resultado.Sucesso ? "Sucesso" : "Aviso"] = resultado.Mensagem;

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Aluno"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarStatus(int matriculaId, string? status)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));

        var resultado = await _matriculas.AlterarStatusAsync(AlunoId.Value, matriculaId, status);
        TempData[resultado.Sucesso ? "Sucesso" : "Aviso"] = resultado.Mensagem;

        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------- sala de aula

    [Authorize(Roles = "Aluno")]
    public async Task<IActionResult> SalaDeAula(int id)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));

        var resultado = await _matriculas.ObterSalaAsync(AlunoId.Value, id);
        if (resultado.ViewModel is null)
        {
            if (resultado.Aviso is not null) TempData["Aviso"] = resultado.Aviso;
            return RedirectToAction(nameof(Index));
        }
        return View(resultado.ViewModel);
    }

    [Authorize(Roles = "Aluno"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConcluirAtividade(int matriculaId, int conteudoId)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));

        var resultado = await _matriculas.ConcluirAtividadeAsync(AlunoId.Value, matriculaId, conteudoId);
        if (!resultado.Sucesso) TempData["Aviso"] = resultado.Mensagem;

        return RedirectToAction(nameof(SalaDeAula), new { id = matriculaId });
    }

    // ---------------------------------------------------------------- prova e certificado

    [Authorize(Roles = "Aluno"), HttpGet]
    public async Task<IActionResult> Prova(int id)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));

        var resultado = await _matriculas.ObterProvaAsync(AlunoId.Value, id);
        if (resultado.CertificadoEmitido)
            return RedirectToAction(nameof(Certificado), new { id });
        if (resultado.ViewModel is null)
        {
            if (resultado.Aviso is not null) TempData["Aviso"] = resultado.Aviso;
            return RedirectToAction(nameof(Index));
        }
        return View(resultado.ViewModel);
    }

    [Authorize(Roles = "Aluno"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConcluirProva(int matriculaId, Dictionary<int, int>? respostas)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));

        var resultado = await _matriculas.ConcluirProvaAsync(AlunoId.Value, matriculaId, respostas ?? new Dictionary<int, int>());
        if (resultado.Aprovado) return RedirectToAction(nameof(Certificado), new { id = matriculaId });
        if (resultado.Aviso is not null) TempData["Aviso"] = resultado.Aviso;
        return RedirectToAction(resultado.Disponivel ? nameof(SalaDeAula) : nameof(Index),
            resultado.Disponivel ? new { id = matriculaId } : null);
    }

    [Authorize(Roles = "Aluno"), HttpGet]
    public async Task<IActionResult> Certificado(int id)
    {
        if (AlunoId is null) return RedirectToAction(nameof(Login));

        var certificado = await _matriculas.ObterCertificadoAsync(AlunoId.Value, id);
        if (certificado is null)
        {
            TempData["Aviso"] = "Certificado indisponível para esta matrícula.";
            return RedirectToAction(nameof(Index));
        }
        return View(certificado);
    }

    // ---------------------------------------------------------------- áreas de apoio

    [Authorize(Roles = "Aluno")]
    public IActionResult Bibliotecas() => View();

    [Authorize(Roles = "Aluno")]
    public IActionResult MuralDoAluno() => View();

    [Authorize(Roles = "Aluno")]
    public IActionResult Tutoriais() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
