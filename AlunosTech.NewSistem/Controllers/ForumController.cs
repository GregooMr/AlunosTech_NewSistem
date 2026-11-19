using AlunosTech.NewSistem.Data;
using AlunosTech.NewSistem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlunosTech.NewSistem.Controllers;

public class ForumController(AppDbContext context, IForumService forum) : Controller
{
    private const int LimitePergunta = 2000;

    private int? AlunoId => HttpContext.Session.GetInt32("AlunoId");
    private bool EhProfessor => HttpContext.Session.GetString("Perfil") == "Professor";

    public async Task<IActionResult> Index(int? cursoId)
    {
        if (AlunoId is not { } alunoId) return RedirectToAction("Login", "Home");
        // Professores respondem dúvidas na própria área.
        if (EhProfessor) return RedirectToAction("Index", "Professor");

        // O aluno só vê as próprias dúvidas e só pode perguntar nas disciplinas em que está matriculado.
        ViewBag.Cursos = await context.Matriculas.AsNoTracking()
            .Where(m => m.AlunoId == alunoId)
            .Select(m => m.Curso)
            .OrderBy(c => c.Titulo)
            .ToListAsync();
        ViewBag.CursoSelecionado = cursoId;
        return View(await forum.ListAsync(cursoId, alunoId: alunoId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Perguntar(int cursoId, string? pergunta)
    {
        if (AlunoId is not { } alunoId) return RedirectToAction("Login", "Home");
        if (EhProfessor) return RedirectToAction("Index", "Professor");

        if (string.IsNullOrWhiteSpace(pergunta))
        {
            TempData["Aviso"] = "Digite uma dúvida antes de enviar.";
            return RedirectToAction(nameof(Index), new { cursoId });
        }
        if (pergunta.Length > LimitePergunta)
        {
            TempData["Aviso"] = $"A dúvida pode ter no máximo {LimitePergunta} caracteres.";
            return RedirectToAction(nameof(Index), new { cursoId });
        }

        if (await forum.AddQuestionAsync(alunoId, cursoId, pergunta))
            TempData["Sucesso"] = "Dúvida enviada ao professor.";
        else
            TempData["Aviso"] = "Você só pode enviar dúvidas das disciplinas em que está matriculado.";

        return RedirectToAction(nameof(Index), new { cursoId });
    }
}
