using AlunosTech.NewSistem.Models;
using AlunosTech.NewSistem.Services;
using Microsoft.EntityFrameworkCore;

namespace AlunosTech.NewSistem.Data;

public static class SeedData
{
    private const string SenhaDemo = "123";

    public static void Initialize(AppDbContext db, IAuthenticationService auth)
    {
        CorrigirHashesDeDemonstracao(db, auth);

        GarantirProfessoresEDisciplinas(db, auth);

        GarantirMatriculasDemo(db);
        GarantirConteudos(db);
    }

    // O script SQL grava valores HASH_SEGURO_* que não são hashes do PasswordHasher.
    // Troca somente os hashes marcadores; hashes reais nunca são alterados.
    private static void CorrigirHashesDeDemonstracao(AppDbContext db, IAuthenticationService auth)
    {
        var usuarios = db.Alunos
            .Where(a => a.SenhaHash.StartsWith("HASH_SEGURO_"))
            .ToList();

        foreach (var usuario in usuarios)
            usuario.SenhaHash = auth.HashPassword(usuario, SenhaDemo);

        if (usuarios.Count > 0) db.SaveChanges();
    }

    private static void GarantirProfessoresEDisciplinas(AppDbContext db, IAuthenticationService auth)
    {
        GarantirAluno(db, auth, "Carlos Silva", "carlos@alunostech.com", "2026001");

        var professores = new[]
        {
            GarantirProfessor(db, auth, "Prof. Me. José Lorandi", "lorandi@alunostech.com"),
            GarantirProfessor(db, auth, "Profa. Dra. Mariana Costa", "mariana@alunostech.com"),
            GarantirProfessor(db, auth, "Prof. Rafael Mendes", "rafael@alunostech.com"),
            GarantirProfessor(db, auth, "Profa. Camila Oliveira", "camila@alunostech.com")
        };

        db.SaveChanges();
        var existentes = db.Cursos.ToDictionary(c => c.Codigo);
        var novos = CriarCursos(professores[0], professores[1], professores[2], professores[3]);
        foreach (var curso in novos)
        {
            if (existentes.TryGetValue(curso.Codigo, out var existente))
                existente.ProfessorId = curso.ProfessorId;
            else
                db.Cursos.Add(curso);
        }
        db.SaveChanges();
    }

    private static void GarantirMatriculasDemo(AppDbContext db)
    {
        var aluno = db.Alunos.First(a => a.Email == "carlos@alunostech.com");
        var cursos = db.Cursos
            .Where(c => c.Codigo == "8125-60" || c.Codigo == "8130-60")
            .ToDictionary(c => c.Codigo);

        var matriculas = new[]
        {
            (Codigo: "8125-60", Progresso: 60),
            (Codigo: "8130-60", Progresso: 25)
        };

        foreach (var matricula in matriculas)
        {
            if (!cursos.TryGetValue(matricula.Codigo, out var curso))
                continue;

            var existe = db.Matriculas.Any(m => m.AlunoId == aluno.Id && m.CursoId == curso.Id && m.Status != "Cancelada");
            if (!existe)
            {
                db.Matriculas.Add(new Matricula
                {
                    AlunoId = aluno.Id,
                    CursoId = curso.Id,
                    ProgressoPercentual = matricula.Progresso
                });
            }
        }

        db.SaveChanges();
    }

    private static Aluno GarantirProfessor(AppDbContext db, IAuthenticationService auth, string nome, string email)
    {
        var professor = db.Alunos.FirstOrDefault(a => a.Email == email);
        if (professor is not null) return professor;

        professor = new Aluno { Nome = nome, Email = email, Perfil = "Professor" };
        professor.SenhaHash = auth.HashPassword(professor, SenhaDemo);
        db.Alunos.Add(professor);
        return professor;
    }

    private static Aluno GarantirAluno(AppDbContext db, IAuthenticationService auth, string nome, string email, string ra)
    {
        var aluno = db.Alunos.FirstOrDefault(a => a.Email == email);
        if (aluno is not null) return aluno;

        aluno = new Aluno { Nome = nome, Email = email, RA = ra, Perfil = "Aluno" };
        aluno.SenhaHash = auth.HashPassword(aluno, SenhaDemo);
        db.Alunos.Add(aluno);
        return aluno;
    }

    private static List<Curso> CriarCursos(Aluno p1, Aluno p2, Aluno p3, Aluno p4) =>
    [
        new() { Codigo = "8125-60", Titulo = "Programação Aplicada em .NET", Descricao = "Desenvolvimento corporativo com C# e arquitetura modular.", ProfessorId = p1.Id },
        new() { Codigo = "8130-60", Titulo = "Cloud Computing e DevOps", Descricao = "Infraestrutura em nuvem, Docker, Kubernetes e CI/CD.", ProfessorId = p2.Id },
        new() { Codigo = "8126-60", Titulo = "Programação de Banco de Dados", Descricao = "Procedures, Triggers, Transações e Otimização SQL.", ProfessorId = p3.Id },
        new() { Codigo = "8127-60", Titulo = "Gerenciamento de Projetos Ágil", Descricao = "Scrum, Kanban, Sprints e Product Backlog.", ProfessorId = p4.Id },
        new() { Codigo = "8131-60", Titulo = "Engenharia de Software", Descricao = "Requisitos, modelagem, testes e qualidade de software.", ProfessorId = p1.Id },
        new() { Codigo = "8132-60", Titulo = "Segurança da Informação", Descricao = "Princípios de segurança, identidade, ameaças e proteção de dados.", ProfessorId = p2.Id },
        new() { Codigo = "8133-60", Titulo = "Estruturas de Dados", Descricao = "Listas, filas, pilhas, árvores e análise de algoritmos.", ProfessorId = p3.Id },
        new() { Codigo = "8134-60", Titulo = "UX e Design de Interfaces", Descricao = "Pesquisa, prototipação, acessibilidade e avaliação de interfaces.", ProfessorId = p4.Id }
    ];

    // Se nenhuma disciplina tem conteúdo (ex.: banco criado só pelo script), publica o conteúdo padrão.
    private static void GarantirConteudos(AppDbContext db)
    {
        foreach (var curso in db.Cursos.ToList())
        {
            if (!db.ConteudosDisciplina.Any(c => c.CursoId == curso.Id))
                db.ConteudosDisciplina.AddRange(ConteudosPadrao(curso));

            foreach (var conteudo in db.ConteudosDisciplina.Where(c => c.CursoId == curso.Id && c.Descricao == null))
                conteudo.Descricao = DescricaoMaterial(conteudo.TipoMaterial, curso.Titulo);
        }

        db.SaveChanges();
    }

    private static IEnumerable<ConteudoDisciplina> ConteudosPadrao(Curso curso) =>
    [
        new() { CursoId = curso.Id, Titulo = "Plano de Ensino", TipoMaterial = "Plano", Descricao = DescricaoMaterial("Plano", curso.Titulo), PesoPercentual = 15 },
        new() { CursoId = curso.Id, Titulo = "Livro-Texto (LT)", TipoMaterial = "Livro", Descricao = DescricaoMaterial("Livro", curso.Titulo), PesoPercentual = 25 },
        new() { CursoId = curso.Id, Titulo = "Slides de Aula", TipoMaterial = "Slide", Descricao = DescricaoMaterial("Slide", curso.Titulo), PesoPercentual = 20 },
        new() { CursoId = curso.Id, Titulo = "Videoaula 01", TipoMaterial = "Videoaula", PesoPercentual = 10 },
        new() { CursoId = curso.Id, Titulo = "Videoaula 02", TipoMaterial = "Videoaula", PesoPercentual = 10 },
        new() { CursoId = curso.Id, Titulo = "Videoaula 03", TipoMaterial = "Videoaula", PesoPercentual = 10 },
        new() { CursoId = curso.Id, Titulo = "Videoaula 04", TipoMaterial = "Videoaula", PesoPercentual = 10 },
    ];

    private static string DescricaoMaterial(string tipo, string disciplina) => tipo switch
    {
        "Plano" => $"Plano simulado de {disciplina}: objetivos, competências, cronograma de oito semanas e critérios de avaliação.",
        "Livro" => $"Livro-texto didático de {disciplina}, com conceitos essenciais, exemplos práticos e exercícios de fixação.",
        "Slide" => $"Apresentação resumida de {disciplina}, organizada em tópicos para revisão antes das atividades e avaliações.",
        _ => $"Material introdutório de apoio à disciplina {disciplina}."
    };
}
