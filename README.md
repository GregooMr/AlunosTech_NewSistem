# AlunosTech.NewSistem

Aplicação web acadêmica do AlunosTech, desenvolvida em ASP.NET Core MVC com Views, Entity Framework Core e SQL Server.

> Observação: o código atual usa MVC (`Controllers` + `Views`). Ele não é um projeto Razor Pages, embora ambos façam parte do ASP.NET Core.

## Funcionalidades

- Login de alunos e professores.
- Mural do aluno, sala de aula, provas e certificado.
- Cursos, conteúdos e progresso acadêmico.
- Fórum de dúvidas.
- Auditoria de alteração de status das matrículas.
- Seed idempotente para dados de demonstração.
- Endpoint de disponibilidade: `GET /health`.

## Tecnologias e versões

As versões abaixo são as configuradas no repositório:

| Componente | Versão/configuração |
| --- | --- |
| .NET | `net10.0.401` |
| ASP.NET Core | SDK Web do .NET 10 |
| Entity Framework Core SQL Server | `10.0.15` |
| Entity Framework Core Tools | `10.0.15` |
| SQL Server | 2022 ou superior; Docker usa `2022-latest` |
| Docker Compose | Compose Specification |
| Front-end | Bootstrap e jQuery incluídos em `wwwroot/lib` |

## Requisitos

Para executar localmente:

1. Windows, Linux ou macOS compatível com o .NET 10.
2. .NET 10 SDK.
3. SQL Server 2022 ou superior, SQL Server Express/Developer ou SQL Server no Docker.
4. Ferramenta para executar SQL, como SQL Server Management Studio, Azure Data Studio ou `sqlcmd`.
5. Visual Studio Community 2026 18.10.3 ou outro editor compatível, opcionalmente.

Para executar com Docker:

1. Docker Desktop em execução.
2. Pelo menos 4 GB de memória disponíveis para o SQL Server; 6 GB ou mais é recomendado.
3. Portas `8080` e `1433` livres.

## Pacotes NuGet

O projeto declara os seguintes pacotes em `AlunosTech.NewSistem/AlunosTech.NewSistem.csproj`:

- `Microsoft.EntityFrameworkCore.SqlServer` `10.0.15`: provider do EF Core para SQL Server.
- `Microsoft.EntityFrameworkCore.Tools` `10.0.15`: ferramentas de desenvolvimento do EF Core, com `PrivateAssets=all`.

O restante vem do SDK Web compartilhado do .NET.

## Banco de dados

O script oficial é:

```text
database/AlunosTech_Moderno.sql
```

Ele é o único script canônico do banco e pode ser executado em um banco novo ou novamente em uma base existente. Ele contém:

- criação das seis tabelas usadas pelo modelo;
- chaves estrangeiras, constraints e índices;
- correções de compatibilidade de bases antigas;
- função de aprovação;
- procedure de conclusão de prova e emissão de certificado;
- trigger de auditoria;
- dados de demonstração idempotentes.

O script não apaga tabelas nem dados. Se uma base antiga possuir matrículas duplicadas, o índice único poderá exigir correção manual antes da execução.

### Executar pelo SSMS ou Azure Data Studio

1. Inicie o SQL Server.
2. Abra `database/AlunosTech_Moderno.sql`.
3. Conecte-se com um usuário que possa criar banco, tabelas, procedures e triggers.
4. Execute o script completo.
5. Confirme se o banco `AlunosTech_Moderno` foi criado.

### Executar pelo sqlcmd

Exemplo para SQL Server local com autenticação SQL:

```powershell
sqlcmd -S localhost,1433 -U sa -P "SuaSenhaForteAqui!" -C -i .\database\AlunosTech_Moderno.sql
```

## Configuração da conexão

A aplicação lê `ConnectionStrings:DefaultConnection` usando a configuração padrão do ASP.NET Core. A configuração de desenvolvimento está em:

```text
AlunosTech.NewSistem/appsettings.Development.json
```

Não versionar senhas reais. Para substituir a connection string sem editar arquivos, use uma variável de ambiente:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=AlunosTech_Moderno;User Id=sa;Password=SuaSenhaForteAqui!;TrustServerCertificate=True;"
```

O `Program.cs` habilita `EnableRetryOnFailure()` para falhas transitórias de rede ou do SQL Server. O seed também não encerra a aplicação quando o banco está temporariamente indisponível; a ocorrência é registrada no log.

## Executar localmente

1. Clone ou abra o repositório.
2. Inicie o SQL Server.
3. Configure `ConnectionStrings:DefaultConnection`.
4. Execute o script `database/AlunosTech_Moderno.sql`.
5. Restaure e compile:

```powershell
dotnet restore .\AlunosTech.NewSistem\AlunosTech.NewSistem.csproj
dotnet build .\AlunosTech.NewSistem\AlunosTech.NewSistem.csproj
```

6. Inicie a aplicação:

```powershell
dotnet run --project .\AlunosTech.NewSistem\AlunosTech.NewSistem.csproj
```

7. Acesse a URL exibida no terminal. Para verificar a disponibilidade:

```powershell
Invoke-WebRequest http://localhost:5000/health
```

A porta pode variar quando a aplicação é iniciada pelo Visual Studio ou pelo perfil de execução.

## Usuários de demonstração

O seed cria os usuários abaixo. A senha inicial é `123`; na primeira inicialização com os marcadores do script, a aplicação substitui o marcador por um hash real.

| E-mail | Perfil |
| --- | --- |
| `lorandi@alunostech.com` | Professor |
| `mariana@alunostech.com` | Professor |
| `rafael@alunostech.com` | Professor |
| `camila@alunostech.com` | Professor |
| `carlos@alunostech.com` | Aluno |

Essas credenciais são somente para desenvolvimento. Altere ou remova os usuários antes de qualquer ambiente real.

## Executar no Docker Desktop

O arquivo `docker-compose.yml` sobe dois serviços:

- `sqlserver`: SQL Server 2022 com volume persistente `sqlserver_data`;
- `app`: aplicação publicada pelo `Dockerfile`, exposta na porta `8080`.

A aplicação usa `Server=sqlserver,1433`, pois dentro da rede Compose o nome do serviço é o hostname do banco.

### Subir os serviços

Na raiz do repositório:

```powershell
docker compose up --build -d
```

Verifique os serviços:

```powershell
docker compose ps
docker compose logs -f app
```

Acesse:

- Aplicação: `http://localhost:8080`
- Health check: `http://localhost:8080/health`

O SQL Server pode levar alguns segundos para ficar pronto. O Compose aguarda o health check do banco antes de iniciar a aplicação, e o EF Core possui retry para falhas transitórias.

### Aplicar o script unificado no SQL Server do Compose

A aplicação cria as tabelas básicas via `EnsureCreated`, mas o script também instala a procedure, a função, a trigger e os índices oficiais. Para aplicar tudo explicitamente:

```powershell
$env:MSSQL_SA_PASSWORD = "AlunosTech#2026Senha"
Get-Content .\database\AlunosTech_Moderno.sql -Raw | docker exec -i alunostech-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $env:MSSQL_SA_PASSWORD -C -b
```

Em instalações que não possuam `sqlcmd` nesse caminho, execute o script pelo SSMS/Azure Data Studio conectando em `localhost,1433` com o usuário `sa`.

### Usar outra senha

A senha padrão existe apenas para facilitar o exemplo. Antes de subir os serviços, defina uma senha forte:

```powershell
$env:MSSQL_SA_PASSWORD = "UmaSenhaForte#2026"
docker compose up --build -d
```

Se o volume já tiver sido criado, alterar a variável não altera a senha existente. Para recriar o banco de desenvolvimento do zero, removendo os dados persistidos:

```powershell
docker compose down -v
docker compose up --build -d
```

O comando `down -v` é destrutivo para os dados do SQL Server.

### Parar os serviços

```powershell
docker compose down
```

Para remover também os dados persistidos:

```powershell
docker compose down -v
```

## Estrutura principal

```text
AlunosTech.NewSistem/
├── Controllers/              # endpoints MVC
├── Data/                     # DbContext e seed
├── Models/                   # entidades e view models
├── Services/                 # regras de negócio
├── Views/                    # páginas Razor do MVC
├── wwwroot/                  # CSS, JavaScript e bibliotecas estáticas
├── Program.cs                # composição da aplicação
└── appsettings*.json         # configuração
Dockerfile                    # imagem da aplicação
docker-compose.yml             # aplicação + SQL Server
 database/AlunosTech_Moderno.sql # banco canônico
```

## Diagnóstico rápido

### A aplicação não conecta ao banco

- Confirme se o SQL Server está em execução.
- Confira `Server`, porta, usuário e senha.
- No Docker, use `Server=sqlserver,1433` dentro da aplicação; `localhost` aponta para o próprio contêiner.
- Veja os logs com `docker compose logs sqlserver` e `docker compose logs app`.

### O banco demora para iniciar

É esperado no primeiro uso do contêiner. Aguarde o serviço ficar `healthy`:

```powershell
docker compose ps
```

### A porta já está em uso

Altere o lado esquerdo do mapeamento no `docker-compose.yml`, por exemplo `8081:8080`, e acesse `http://localhost:8081`.

### O seed não foi executado

Verifique o log da aplicação. O seed é executado na inicialização e tenta novamente em uma nova inicialização. O script SQL canônico pode ser executado manualmente para instalar também os objetos programáveis do banco.

## Segurança para produção

- Não use as credenciais demo.
- Não mantenha senhas em `appsettings.json`, no README ou no controle de versão.
- Use secrets/variáveis protegidas no ambiente de execução.
- Troque o certificado e a configuração HTTPS antes de publicar.
- Restrinja a exposição da porta `1433`; ela só é necessária para administração local.
