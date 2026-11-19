using AlunosTech.NewSistem.Data;
using AlunosTech.NewSistem.Models;
using AlunosTech.NewSistem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlunosTech.NewSistem.Controllers;

public class ProfessorController(AppDbContext context, IForumService forum, IWebHostEnvironment environment) : Controller
{
    public static readonly string[] TiposMaterial = ["Plano", "Livro", "Slide", "Videoaula", "Material"];

    private bool IsProfessor => HttpContext.Session.GetString("Perfil") == "Professor";
    private int? ProfessorId => HttpContext.Session.GetInt32("AlunoId");
    private const long TamanhoMaximoPdf = 10 * 1024 * 1024;

    // Forbid() exige um esquema de autenticação configurado e causaria erro 500 neste projeto
    // (a autenticação é feita por sessão); por isso volta-se para o login.
    private IActionResult AcessoNegado() => RedirectToAction("Login", "Home");

    public async Task<IActionResult> Index()
    {
        if (!IsProfessor || ProfessorId is not { } professorId) return AcessoNegado();

        var cursos = await context.Cursos.AsNoTracking()
            .Include(c => c.Conteudos)
            .Where(c => c.ProfessorId == professorId)
            .OrderBy(c => c.Titulo)
            .ToListAsync();

        ViewBag.Duvidas = await forum.ListAsync(professorId: professorId);
        ViewBag.Tipos = TiposMaterial;
        return View(cursos);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarConteudo(int cursoId, string? titulo, string? tipoMaterial, int pesoPercentual)
    {
        if (!IsProfessor || ProfessorId is not { } professorId) return AcessoNegado();
        if (!await context.Cursos.AnyAsync(c => c.Id == cursoId && c.ProfessorId == professorId)) return AcessoNegado();

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
        if (!IsProfessor || ProfessorId is not { } professorId) return AcessoNegado();

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
        if (!IsProfessor || ProfessorId is not { } professorId) return AcessoNegado();

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
        if (!IsProfessor || ProfessorId is not { } professorId) return AcessoNegado();

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
        if (!IsProfessor || ProfessorId is not { } professorId) return AcessoNegado();

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
