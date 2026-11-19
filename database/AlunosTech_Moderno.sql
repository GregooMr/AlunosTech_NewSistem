-- AlunosTech - banco canônico
-- SQL Server 2022+ | Seguro para banco novo e reexecuções.
-- Não apaga dados existentes. O seed usa chaves naturais (e-mail/código).

IF DB_ID(N'AlunosTech_Moderno') IS NULL
	CREATE DATABASE AlunosTech_Moderno;
GO

USE AlunosTech_Moderno;
GO

IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Usuarios
	(
		Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
		Nome VARCHAR(255) NOT NULL,
		Email VARCHAR(255) NOT NULL,
		SenhaHash VARCHAR(255) NOT NULL,
		RA VARCHAR(50) NULL,
		Perfil VARCHAR(50) NOT NULL,
		DataCadastro DATETIME NOT NULL CONSTRAINT DF_Usuarios_DataCadastro DEFAULT GETDATE(),
		CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),
		CONSTRAINT CK_Usuarios_Perfil CHECK (Perfil IN ('Aluno', 'Professor'))
	);
END;
GO

IF OBJECT_ID(N'dbo.Cursos', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Cursos
	(
		Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cursos PRIMARY KEY,
		Codigo VARCHAR(50) NOT NULL,
		Titulo VARCHAR(255) NOT NULL,
		Descricao NVARCHAR(MAX) NULL,
		ProfessorId INT NULL,
		CONSTRAINT UQ_Cursos_Codigo UNIQUE (Codigo),
		CONSTRAINT FK_Cursos_Usuarios_Professor FOREIGN KEY (ProfessorId) REFERENCES dbo.Usuarios(Id)
	);
END;
GO

IF OBJECT_ID(N'dbo.ConteudosDisciplina', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.ConteudosDisciplina
	(
		Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConteudosDisciplina PRIMARY KEY,
		CursoId INT NOT NULL,
		Titulo VARCHAR(255) NOT NULL,
		TipoMaterial VARCHAR(50) NOT NULL,
		Descricao NVARCHAR(MAX) NULL,
		ComentarioProfessor NVARCHAR(2000) NULL,
		NomeArquivo VARCHAR(255) NULL,
		UrlArquivo VARCHAR(500) NULL,
		DataUpload DATETIME2 NULL,
		PesoPercentual INT NOT NULL,
		CONSTRAINT FK_ConteudosDisciplina_Cursos FOREIGN KEY (CursoId) REFERENCES dbo.Cursos(Id) ON DELETE CASCADE,
		CONSTRAINT CK_ConteudosDisciplina_Peso CHECK (PesoPercentual BETWEEN 0 AND 100)
	);
END;
GO

IF OBJECT_ID(N'dbo.Matriculas', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Matriculas
	(
		Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Matriculas PRIMARY KEY,
		AlunoId INT NOT NULL,
		CursoId INT NOT NULL,
		ProgressoPercentual INT NOT NULL CONSTRAINT DF_Matriculas_Progresso DEFAULT 0,
		NotaFinal DECIMAL(5,2) NULL,
		Status VARCHAR(50) NOT NULL CONSTRAINT DF_Matriculas_Status DEFAULT 'Ativa',
		CodigoCertificado UNIQUEIDENTIFIER NULL,
		DataMatricula DATETIME NOT NULL CONSTRAINT DF_Matriculas_DataMatricula DEFAULT GETDATE(),
		CONSTRAINT FK_Matriculas_Usuarios_Aluno FOREIGN KEY (AlunoId) REFERENCES dbo.Usuarios(Id),
		CONSTRAINT FK_Matriculas_Cursos FOREIGN KEY (CursoId) REFERENCES dbo.Cursos(Id),
		CONSTRAINT CK_Matriculas_Progresso CHECK (ProgressoPercentual BETWEEN 0 AND 100),
		CONSTRAINT CK_Matriculas_Status CHECK (Status IN ('Ativa', 'Trancada', 'Cancelada', 'Concluida'))
	);
END;
GO

IF OBJECT_ID(N'dbo.DuvidasForum', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.DuvidasForum
	(
		Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DuvidasForum PRIMARY KEY,
		CursoId INT NOT NULL,
		AlunoId INT NOT NULL,
		Pergunta NVARCHAR(MAX) NOT NULL,
		RespostaProfessor NVARCHAR(MAX) NULL,
		DataPergunta DATETIME NOT NULL CONSTRAINT DF_DuvidasForum_DataPergunta DEFAULT GETDATE(),
		DataResposta DATETIME NULL,
		CONSTRAINT FK_DuvidasForum_Cursos FOREIGN KEY (CursoId) REFERENCES dbo.Cursos(Id),
		CONSTRAINT FK_DuvidasForum_Usuarios_Aluno FOREIGN KEY (AlunoId) REFERENCES dbo.Usuarios(Id)
	);
END;
GO

IF OBJECT_ID(N'dbo.LogsAuditoria', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.LogsAuditoria
	(
		Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LogsAuditoria PRIMARY KEY,
		TabelaAfetada VARCHAR(100) NULL,
		Acao VARCHAR(50) NULL,
		Detalhes NVARCHAR(MAX) NULL,
		DataLog DATETIME NOT NULL CONSTRAINT DF_LogsAuditoria_DataLog DEFAULT GETDATE()
	);
END;
GO

-- Compatibilidade com bases antigas que ainda não possuem os campos de material.
IF COL_LENGTH(N'dbo.ConteudosDisciplina', N'Descricao') IS NULL ALTER TABLE dbo.ConteudosDisciplina ADD Descricao NVARCHAR(MAX) NULL;
IF COL_LENGTH(N'dbo.ConteudosDisciplina', N'ComentarioProfessor') IS NULL ALTER TABLE dbo.ConteudosDisciplina ADD ComentarioProfessor NVARCHAR(2000) NULL;
IF COL_LENGTH(N'dbo.ConteudosDisciplina', N'NomeArquivo') IS NULL ALTER TABLE dbo.ConteudosDisciplina ADD NomeArquivo VARCHAR(255) NULL;
IF COL_LENGTH(N'dbo.ConteudosDisciplina', N'UrlArquivo') IS NULL ALTER TABLE dbo.ConteudosDisciplina ADD UrlArquivo VARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.ConteudosDisciplina', N'DataUpload') IS NULL ALTER TABLE dbo.ConteudosDisciplina ADD DataUpload DATETIME2 NULL;
GO

-- Ajustes de compatibilidade da versão anterior do banco.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Cursos') AND name = N'Descricao' AND system_type_id = 35)
	ALTER TABLE dbo.Cursos ALTER COLUMN Descricao NVARCHAR(MAX) NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.DuvidasForum') AND name = N'Pergunta' AND system_type_id = 35)
	ALTER TABLE dbo.DuvidasForum ALTER COLUMN Pergunta NVARCHAR(MAX) NOT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.DuvidasForum') AND name = N'RespostaProfessor' AND system_type_id = 35)
	ALTER TABLE dbo.DuvidasForum ALTER COLUMN RespostaProfessor NVARCHAR(MAX) NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.LogsAuditoria') AND name = N'Detalhes' AND system_type_id = 35)
	ALTER TABLE dbo.LogsAuditoria ALTER COLUMN Detalhes NVARCHAR(MAX) NULL;
GO

DECLARE @statusConstraint sysname, @statusDefinition nvarchar(max), @dropConstraintSql nvarchar(max);
SELECT TOP (1) @statusConstraint = cc.name, @statusDefinition = cc.definition
FROM sys.check_constraints cc
INNER JOIN sys.tables t ON t.object_id = cc.parent_object_id
WHERE t.object_id = OBJECT_ID(N'dbo.Matriculas') AND cc.definition LIKE N'%Status%';
IF @statusConstraint IS NOT NULL AND @statusDefinition NOT LIKE N'%Cancelada%'
BEGIN
	SET @dropConstraintSql = N'ALTER TABLE dbo.Matriculas DROP CONSTRAINT ' + QUOTENAME(@statusConstraint) + N';';
	EXEC(@dropConstraintSql);
END;
IF NOT EXISTS
(
	SELECT 1 FROM sys.check_constraints cc
	WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Matriculas') AND cc.definition LIKE N'%Cancelada%'
)
	ALTER TABLE dbo.Matriculas ADD CONSTRAINT CK_Matriculas_Status CHECK (Status IN ('Ativa', 'Trancada', 'Cancelada', 'Concluida'));
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Matriculas') AND name = N'IX_Matriculas_AlunoCurso')
	CREATE UNIQUE NONCLUSTERED INDEX IX_Matriculas_AlunoCurso ON dbo.Matriculas(AlunoId, CursoId) WHERE Status <> 'Cancelada';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.DuvidasForum') AND name = N'IX_DuvidasForum_CursoId')
	CREATE NONCLUSTERED INDEX IX_DuvidasForum_CursoId ON dbo.DuvidasForum(CursoId, AlunoId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ConteudosDisciplina') AND name = N'IX_ConteudosDisciplina_CursoId')
	CREATE NONCLUSTERED INDEX IX_ConteudosDisciplina_CursoId ON dbo.ConteudosDisciplina(CursoId);
GO

CREATE OR ALTER FUNCTION dbo.fn_VerificarAprovacao (@Nota DECIMAL(5,2))
RETURNS VARCHAR(20)
AS
BEGIN
	RETURN CASE WHEN @Nota >= 7.0 THEN 'Aprovado' ELSE 'Reprovado' END;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConcluirProvaEEmitirCertificado
	@MatriculaId INT,
	@NotaFinal DECIMAL(5,2)
AS
BEGIN
	SET NOCOUNT ON;
	SET XACT_ABORT ON;

	BEGIN TRY
		BEGIN TRANSACTION;
		DECLARE @Progresso INT, @Status VARCHAR(50);
		SELECT @Progresso = ProgressoPercentual, @Status = Status
		FROM dbo.Matriculas WITH (UPDLOCK, ROWLOCK) WHERE Id = @MatriculaId;

		IF @Progresso IS NULL THROW 50000, 'Matrícula não encontrada.', 1;
		IF @Status <> 'Ativa' THROW 50002, 'Somente matrículas ativas podem realizar a prova.', 1;
		IF @Progresso < 100 THROW 50001, 'O aluno não concluiu 100% dos conteúdos para realizar a prova.', 1;
		IF @NotaFinal < 0 OR @NotaFinal > 10 THROW 50003, 'A nota deve estar entre 0 e 10.', 1;

		UPDATE dbo.Matriculas
		SET NotaFinal = @NotaFinal,
			Status = CASE WHEN @NotaFinal >= 7.0 THEN 'Concluida' ELSE Status END,
			CodigoCertificado = CASE WHEN @NotaFinal >= 7.0 THEN NEWID() ELSE NULL END
		WHERE Id = @MatriculaId;
		COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
		THROW;
	END CATCH;
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_AuditoriaMatriculaStatus
ON dbo.Matriculas
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	IF UPDATE(Status)
		INSERT INTO dbo.LogsAuditoria (TabelaAfetada, Acao, Detalhes)
		SELECT 'Matriculas', 'ALTERACAO_STATUS',
			   CONCAT('Matrícula ID: ', i.Id, ' alterada de ', d.Status, ' para o status: ', i.Status)
		FROM inserted i INNER JOIN deleted d ON d.Id = i.Id
		WHERE ISNULL(i.Status, '') <> ISNULL(d.Status, '');
END;
GO

-- Seed idempotente para uso local/Docker. A aplicação troca os marcadores por hashes reais.
INSERT INTO dbo.Usuarios (Nome, Email, SenhaHash, RA, Perfil)
SELECT v.Nome, v.Email, v.SenhaHash, v.RA, v.Perfil
FROM (VALUES
	('Prof. Me. José Lorandi', 'lorandi@alunostech.com', 'HASH_SEGURO_XYZ123', NULL, 'Professor'),
	('Profa. Dra. Mariana Costa', 'mariana@alunostech.com', 'HASH_SEGURO_MAR123', NULL, 'Professor'),
	('Prof. Rafael Mendes', 'rafael@alunostech.com', 'HASH_SEGURO_RAF123', NULL, 'Professor'),
	('Profa. Camila Oliveira', 'camila@alunostech.com', 'HASH_SEGURO_CAM123', NULL, 'Professor'),
	('Carlos Silva', 'carlos@alunostech.com', 'HASH_SEGURO_ABC789', '2026001', 'Aluno')
) v(Nome, Email, SenhaHash, RA, Perfil)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Usuarios u WHERE u.Email = v.Email);
GO

INSERT INTO dbo.Cursos (Codigo, Titulo, Descricao, ProfessorId)
SELECT v.Codigo, v.Titulo, v.Descricao, u.Id
FROM (VALUES
	('8125-60', 'Programação Aplicada em .NET', N'Desenvolvimento corporativo com C# e arquitetura modular.', 'lorandi@alunostech.com'),
	('8130-60', 'Cloud Computing e DevOps', N'Infraestrutura em nuvem, Docker, Kubernetes e CI/CD.', 'mariana@alunostech.com'),
	('8126-60', 'Programação de Banco de Dados', N'Procedures, Triggers, Transações e Otimização SQL.', 'rafael@alunostech.com'),
	('8127-60', 'Gerenciamento de Projetos Ágil', N'Scrum, Kanban, Sprints e Product Backlog.', 'camila@alunostech.com'),
	('8131-60', 'Engenharia de Software', N'Requisitos, modelagem, testes e qualidade de software.', 'lorandi@alunostech.com'),
	('8132-60', 'Segurança da Informação', N'Princípios de segurança, identidade, ameaças e proteção de dados.', 'mariana@alunostech.com'),
	('8133-60', 'Estruturas de Dados', N'Listas, filas, pilhas, árvores e análise de algoritmos.', 'rafael@alunostech.com'),
	('8134-60', 'UX e Design de Interfaces', N'Pesquisa, prototipação, acessibilidade e avaliação de interfaces.', 'camila@alunostech.com')
) v(Codigo, Titulo, Descricao, EmailProfessor)
INNER JOIN dbo.Usuarios u ON u.Email = v.EmailProfessor
WHERE NOT EXISTS (SELECT 1 FROM dbo.Cursos c WHERE c.Codigo = v.Codigo);
GO

INSERT INTO dbo.ConteudosDisciplina (CursoId, Titulo, TipoMaterial, Descricao, PesoPercentual)
SELECT c.Id, v.Titulo, v.TipoMaterial,
	   CASE WHEN v.Descricao IS NULL THEN NULL ELSE REPLACE(v.Descricao, '{DISCIPLINA}', c.Titulo) END,
	   v.Peso
FROM dbo.Cursos c
CROSS JOIN (VALUES
	('Plano de Ensino', 'Plano', N'Plano simulado de {DISCIPLINA}: objetivos, competências, cronograma de oito semanas e critérios de avaliação.', 15),
	('Livro-Texto (LT)', 'Livro', N'Livro-texto didático de {DISCIPLINA}, com conceitos essenciais, exemplos práticos e exercícios de fixação.', 25),
	('Slides de Aula', 'Slide', N'Apresentação resumida de {DISCIPLINA}, organizada em tópicos para revisão.', 20),
	('Videoaula 01', 'Videoaula', NULL, 10), ('Videoaula 02', 'Videoaula', NULL, 10),
	('Videoaula 03', 'Videoaula', NULL, 10), ('Videoaula 04', 'Videoaula', NULL, 10)
) v(Titulo, TipoMaterial, Descricao, Peso)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ConteudosDisciplina cd WHERE cd.CursoId = c.Id AND cd.Titulo = v.Titulo);
GO

INSERT INTO dbo.Matriculas (AlunoId, CursoId, ProgressoPercentual)
SELECT u.Id, c.Id, v.Progresso
FROM (VALUES ('8125-60', 100), ('8130-60', 25)) v(Codigo, Progresso)
INNER JOIN dbo.Usuarios u ON u.Email = 'carlos@alunostech.com'
INNER JOIN dbo.Cursos c ON c.Codigo = v.Codigo
WHERE NOT EXISTS (SELECT 1 FROM dbo.Matriculas m WHERE m.AlunoId = u.Id AND m.CursoId = c.Id);
GO
