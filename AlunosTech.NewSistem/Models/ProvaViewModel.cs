using AlunosTech.NewSistem.Services;

namespace AlunosTech.NewSistem.Models;

public sealed record ProvaViewModel(Matricula Matricula, IReadOnlyList<ExamQuestion> Questoes, decimal NotaMinima);
