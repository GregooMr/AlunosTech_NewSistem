using System.Security.Claims;
using AlunosTech.NewSistem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlunosTech.NewSistem.Controllers;

[Authorize(Roles = "Aluno")]
public class ForumController(ICursoService cursos, IForumService forum) : Controller
{
    private const int LimitePergunta = 2000;

    private int AlunoId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(int? cursoId)
    {
        var alunoId = AlunoId;

        // O aluno só vê as próprias dúvidas e só pode perguntar nas disciplinas em que está matriculado.
        ViewBag.Cursos = await cursos.ListarMatriculadosAsync(alunoId);
        ViewBag.CursoSelecionado = cursoId;
        return View(await forum.ListAsync(cursoId, alunoId: alunoId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Perguntar(int cursoId, string? pergunta)
    {
        var alunoId = AlunoId;

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
