using AlunosTech.NewSistem.Models;

namespace AlunosTech.NewSistem.Services;

/// <summary>
/// Converte os conteúdos cadastrados pelo professor (ConteudosDisciplina) em etapas
/// sequenciais cujo peso soma 100%. O progresso do aluno só avança por etapa concluída
/// no servidor, o que impede "turbinar" o progresso repetindo cliques ou adulterando o formulário.
/// </summary>
public sealed class ProgressoCalculator : IProgressoCalculator
{
    public IReadOnlyList<AtividadeViewModel> Montar(IEnumerable<ConteudoDisciplina> conteudos, int progresso)
    {
        var lista = conteudos.OrderBy(c => c.Id).ToList();
        var resultado = new List<AtividadeViewModel>(lista.Count);
        if (lista.Count == 0) return resultado;

        var total = lista.Sum(c => Math.Max(c.PesoPercentual, 0));
        var acumulado = 0;
        var fimAnterior = 0;
        var proximaMarcada = false;

        for (var i = 0; i < lista.Count; i++)
        {
            acumulado += Math.Max(lista[i].PesoPercentual, 0);

            int fim;
            if (i == lista.Count - 1) fim = 100;
            else if (total <= 0) fim = (int)Math.Round(100.0 * (i + 1) / lista.Count);
            else fim = (int)Math.Round(100.0 * acumulado / total);

            fim = Math.Clamp(Math.Max(fim, fimAnterior), 0, 100);
            var concluida = progresso >= fim;
            var proxima = !concluida && !proximaMarcada;
            if (proxima) proximaMarcada = true;

            resultado.Add(new AtividadeViewModel(lista[i], fim, fim - fimAnterior, concluida, proxima));
            fimAnterior = fim;
        }

        return resultado;
    }
}
