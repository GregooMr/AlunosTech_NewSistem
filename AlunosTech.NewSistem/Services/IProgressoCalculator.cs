using AlunosTech.NewSistem.Models;

namespace AlunosTech.NewSistem.Services;

public interface IProgressoCalculator
{
    IReadOnlyList<AtividadeViewModel> Montar(IEnumerable<ConteudoDisciplina> conteudos, int progresso);
}
