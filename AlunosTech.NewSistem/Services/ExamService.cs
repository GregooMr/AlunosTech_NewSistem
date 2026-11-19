using AlunosTech.NewSistem.Models;

namespace AlunosTech.NewSistem.Services;

public sealed record ExamQuestion(int Id, string Enunciado, IReadOnlyList<string> Alternativas, int Correta);

public interface IExamService
{
    IReadOnlyList<ExamQuestion> GetQuestions(Curso curso);

    /// <summary>Corrige a prova NO SERVIDOR e devolve a nota de 0 a 10.</summary>
    decimal Grade(Curso curso, IReadOnlyDictionary<int, int> respostas);
}

public sealed class ExamService : IExamService
{
    private static ExamQuestion Q(int id, string enunciado, int correta, params string[] alternativas) =>
        new(id, enunciado, alternativas, correta);

    private static readonly Dictionary<string, IReadOnlyList<ExamQuestion>> Banco = new()
    {
        ["8125-60"] =
        [
            Q(1, "Qual padrão arquitetural o ASP.NET Core MVC adota?", 0, "Model-View-Controller", "Microkernel", "Pipe-and-Filter", "Blackboard"),
            Q(2, "Qual método registra um serviço com ciclo de vida por requisição?", 2, "AddSingleton", "AddTransient", "AddScoped", "AddHostedService"),
            Q(3, "No Entity Framework Core, qual classe representa a sessão de acesso ao banco?", 1, "DbSet", "DbContext", "Migration", "Controller"),
            Q(4, "Qual é a forma correta de armazenar senhas?", 3, "Em texto puro", "Em Base64", "Criptografadas com chave fixa no código", "Como hash com salt (ex.: PasswordHasher)"),
            Q(5, "Onde devem ficar as regras de negócio em uma arquitetura em camadas?", 1, "Nas views", "Em serviços/domínio", "No CSS", "No arquivo de rotas"),
        ],
        ["8130-60"] =
        [
            Q(1, "O que é um container Docker?", 2, "Uma máquina virtual completa com kernel próprio", "Um banco de dados", "Um processo isolado que empacota a aplicação e suas dependências", "Um editor de código"),
            Q(2, "Para que serve o Kubernetes?", 0, "Orquestrar e escalar containers", "Compilar código C#", "Criar planilhas", "Modelar bancos de dados"),
            Q(3, "O que significa CI/CD?", 3, "Código, Infraestrutura e Dados", "Cloud, Internet e Deploy", "Compilação Interna e Distribuição", "Integração Contínua e Entrega/Implantação Contínua"),
            Q(4, "Qual modelo de nuvem entrega máquinas virtuais, rede e armazenamento sob demanda?", 1, "SaaS", "IaaS", "FaaS", "DaaS"),
            Q(5, "Qual arquivo descreve como construir uma imagem Docker?", 2, "docker.json", "compose.exe", "Dockerfile", "image.sql"),
        ],
        ["8126-60"] =
        [
            Q(1, "Na sigla ACID, o que significa a letra A?", 1, "Autenticidade", "Atomicidade", "Agilidade", "Auditoria"),
            Q(2, "O que é uma trigger?", 3, "Um tipo de índice", "Uma tabela temporária", "Um relatório", "Código executado automaticamente em resposta a INSERT, UPDATE ou DELETE"),
            Q(3, "Qual é o principal objetivo de um índice?", 0, "Acelerar consultas de leitura", "Aumentar o tamanho da tabela", "Impedir backups", "Criptografar dados"),
            Q(4, "Qual comando desfaz uma transação?", 2, "COMMIT", "SAVE", "ROLLBACK", "TRUNCATE"),
            Q(5, "O que é uma stored procedure?", 1, "Uma cópia de segurança", "Um conjunto de instruções T-SQL armazenado no servidor", "Uma view materializada", "Um tipo de dado"),
        ],
        ["8127-60"] =
        [
            Q(1, "O que é uma Sprint no Scrum?", 2, "Uma reunião diária", "Um documento de requisitos", "Um ciclo curto de duração fixa para entregar um incremento", "Um papel da equipe"),
            Q(2, "O que é o Product Backlog?", 0, "Lista ordenada e priorizada de tudo que o produto pode precisar", "Lista de bugs corrigidos", "Relatório financeiro", "Ata de reunião"),
            Q(3, "Qual é uma característica central do Kanban?", 3, "Sprints obrigatórias de 4 semanas", "Papéis fixos de Scrum Master", "Planejamento anual", "Visualizar o fluxo e limitar o trabalho em andamento (WIP)"),
            Q(4, "Quem é responsável por maximizar o valor do produto e gerir o backlog?", 1, "Scrum Master", "Product Owner", "Desenvolvedor", "Gerente de TI"),
            Q(5, "Qual é a duração máxima recomendada de uma Daily Scrum?", 2, "5 minutos", "30 minutos", "15 minutos", "60 minutos"),
        ],
    };

    private static readonly IReadOnlyList<ExamQuestion> Generico =
    [
        Q(1, "Qual é a nota mínima para aprovação e emissão do certificado?", 1, "5,0", "7,0", "8,5", "10,0"),
        Q(2, "Qual progresso é necessário para liberar a Prova Final?", 3, "25%", "50%", "75%", "100%"),
        Q(3, "Como o aluno envia dúvidas ao professor da disciplina?", 0, "Pelo canal de dúvidas (fórum) da plataforma", "Pelo mural de avisos", "Pela biblioteca digital", "Não é possível"),
        Q(4, "Como as senhas são armazenadas no sistema?", 2, "Em texto puro", "Em Base64", "Como hash", "Não são armazenadas"),
        Q(5, "Quem pode adicionar, editar e excluir conteúdos de uma disciplina?", 1, "Qualquer aluno", "O professor responsável", "Qualquer visitante", "Ninguém"),
    ];

    public IReadOnlyList<ExamQuestion> GetQuestions(Curso curso) =>
        Banco.TryGetValue(curso.Codigo, out var perguntas) ? perguntas : Generico;

    public decimal Grade(Curso curso, IReadOnlyDictionary<int, int> respostas)
    {
        var perguntas = GetQuestions(curso);
        var acertos = perguntas.Count(p => respostas.TryGetValue(p.Id, out var escolhida) && escolhida == p.Correta);
        return Math.Round(10m * acertos / perguntas.Count, 2);
    }
}
