using System.Security.Claims;
using AlunosTech.NewSistem.Data;
using AlunosTech.NewSistem.Models;
using AlunosTech.NewSistem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlunosTech.NewSistem.Controllers;

[Authorize(Roles = "Professor")]
public class ProfessorController(AppDbContext context, IForumService forum, IWebHostEnvironment environment, ICursoService cursosService) : Controller
{
    public static readonly string[] TiposMaterial = ["Plano", "Livro", "Slide", "Videoaula", "Material"];

    private int ProfessorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private const long TamanhoMaximoPdf = 10 * 1024 * 1024;

    public async Task<IActionResult> Index()
    {
        var professorId = ProfessorId;

        var cursos = await cursosService.ListarDoProfessorAsync(professorId);

        ViewBag.Duvidas = await forum.ListAsync(professorId: professorId);
        ViewBag.Tipos = TiposMaterial;
        return View(cursos);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarConteudo(int cursoId, string? titulo, string? tipoMaterial, int pesoPercentual)
    {
        var professorId = ProfessorId;
        if (!await cursosService.PertenceAoProfessorAsync(cursoId, professorId)) return Forbid();

        if (!Validar(titulo, tipoMaterial, pesoPercentual, out var erro))
        {
            TempData["Aviso"] = erro;
            return RedirectToAction(nameof(Index));
        }

        context.ConteudosDisciplina.Add(new ConteudoDisciplina
        {
            CursoId = cursoId,
            Titulo = titulo!.Trim(),
            TipoMaterial = tipoMaterial!,
            PesoPercentual = pesoPercentual
        });
        await context.SaveChangesAsync();
        TempData["Sucesso"] = "Conteúdo adicionado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AnexarPdf(int id, IFormFile? arquivo)
    {
        var professorId = ProfessorId;

        var conteudo = await context.ConteudosDisciplina
            .FirstOrDefaultAsync(c => c.Id == id && c.Curso.ProfessorId == professorId);
        if (conteudo is null) return RedirectToAction(nameof(Index));

        if (arquivo is null || arquivo.Length == 0)
            TempData["Aviso"] = "Selecione um arquivo PDF.";
        else if (arquivo.Length > TamanhoMaximoPdf || !string.Equals(Path.GetExtension(arquivo.FileName), ".pdf", StringComparison.OrdinalIgnoreCase) || arquivo.ContentType != "application/pdf")
            TempData["Aviso"] = "O anexo deve ser um PDF de até 10 MB.";
        else
        {
            var pasta = Path.Combine(environment.WebRootPath, "uploads", "materiais");
            Directory.CreateDirectory(pasta);
            var nomeSeguro = $"{Guid.NewGuid():N}.pdf";
            var caminho = Path.Combine(pasta, nomeSeguro);
            await using (var stream = System.IO.File.Create(caminho))
                await arquivo.CopyToAsync(stream);

            RemoverArquivoAnterior(conteudo.UrlArquivo);
            conteudo.NomeArquivo = Path.GetFileName(arquivo.FileName);
            conteudo.UrlArquivo = $"/uploads/materiais/{nomeSeguro}";
            conteudo.DataUpload = DateTime.UtcNow;
            await context.SaveChangesAsync();
            TempData["Sucesso"] = "PDF anexado ao conteúdo.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarConteudo(int id, string? titulo, string? tipoMaterial, int pesoPercentual, string? comentarioProfessor)
    {
        var professorId = ProfessorId;

        var conteudo = await context.ConteudosDisciplina
            .FirstOrDefaultAsync(c => c.Id == id && c.Curso.ProfessorId == professorId);
        if (conteudo is null) return RedirectToAction(nameof(Index));

        if (!Validar(titulo, tipoMaterial, pesoPercentual, out var erro))
        {
            TempData["Aviso"] = erro;
            return RedirectToAction(nameof(Index));
        }
        if (comentarioProfessor?.Length > 2000)
        {
            TempData["Aviso"] = "O comentário deve ter até 2.000 caracteres.";
            return RedirectToAction(nameof(Index));
        }

        conteudo.Titulo = titulo!.Trim();
        conteudo.TipoMaterial = tipoMaterial!;
        conteudo.PesoPercentual = pesoPercentual;
        conteudo.ComentarioProfessor = string.IsNullOrWhiteSpace(comentarioProfessor) ? null : comentarioProfessor.Trim();
        await context.SaveChangesAsync();
        TempData["Sucesso"] = "Conteúdo atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirConteudo(int id)
    {
        var professorId = ProfessorId;

        var conteudo = await context.ConteudosDisciplina
            .FirstOrDefaultAsync(c => c.Id == id && c.Curso.ProfessorId == professorId);
        if (conteudo is not null)
        {
            context.ConteudosDisciplina.Remove(conteudo);
            await context.SaveChangesAsync();
            TempData["Sucesso"] = "Conteúdo excluído.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Responder(int id, string? resposta)
    {
        var professorId = ProfessorId;

        if (string.IsNullOrWhiteSpace(resposta))
            TempData["Aviso"] = "Digite uma resposta antes de enviar.";
        else if (await forum.AnswerAsync(id, professorId, resposta))
            TempData["Sucesso"] = "Resposta registrada.";
        else
            TempData["Aviso"] = "Dúvida não encontrada para as suas disciplinas.";

        return RedirectToAction(nameof(Index));
    }

    private static bool Validar(string? titulo, string? tipoMaterial, int peso, out string erro)
    {
        erro = string.Empty;
        if (string.IsNullOrWhiteSpace(titulo) || titulo.Trim().Length > 255)
            erro = "Informe um título de até 255 caracteres.";
        else if (tipoMaterial is null || !TiposMaterial.Contains(tipoMaterial))
            erro = "Tipo de material inválido.";
        else if (peso is < 0 or > 100)
            erro = "O peso deve estar entre 0 e 100.";
        return erro.Length == 0;
    }

    private void RemoverArquivoAnterior(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        var nome = Path.GetFileName(url);
        var caminho = Path.Combine(environment.WebRootPath, "uploads", "materiais", nome);
        if (System.IO.File.Exists(caminho)) System.IO.File.Delete(caminho);
    }
}
