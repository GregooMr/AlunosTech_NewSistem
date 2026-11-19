namespace AlunosTech.NewSistem.Models;

/// <param name="Fim">Percentual acumulado ao concluir esta etapa.</param>
/// <param name="Ganho">Quanto esta etapa soma ao progresso.</param>
public sealed record AtividadeViewModel(ConteudoDisciplina Conteudo, int Fim, int Ganho, bool Concluida, bool Proxima);

public sealed record SalaDeAulaViewModel(Matricula Matricula, IReadOnlyList<AtividadeViewModel> Atividades);
