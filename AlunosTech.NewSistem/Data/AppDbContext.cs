using Microsoft.EntityFrameworkCore;
using AlunosTech.NewSistem.Models;

namespace AlunosTech.NewSistem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
    public DbSet<ConteudoDisciplina> ConteudosDisciplina => Set<ConteudoDisciplina>();
    public DbSet<DuvidaForum> DuvidasForum => Set<DuvidaForum>();
    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // varchar no banco (conforme script SQL): evita conversão implícita nvarchar->varchar nas buscas indexadas.
        modelBuilder.Entity<Aluno>().Property(a => a.Email).IsUnicode(false);
        modelBuilder.Entity<Aluno>().Property(a => a.RA).IsUnicode(false);
        modelBuilder.Entity<Curso>().Property(c => c.Codigo).IsUnicode(false);
        modelBuilder.Entity<Aluno>().HasIndex(a => a.Email).IsUnique();
        modelBuilder.Entity<Aluno>().Property(a => a.DataCadastro).HasDefaultValueSql("GETDATE()");
        modelBuilder.Entity<Curso>().HasIndex(c => c.Codigo).IsUnique();
        modelBuilder.Entity<Curso>().HasOne(c => c.Professor).WithMany().HasForeignKey(c => c.ProfessorId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ConteudoDisciplina>().Property(c => c.NomeArquivo).IsUnicode(false);
        modelBuilder.Entity<ConteudoDisciplina>().Property(c => c.UrlArquivo).IsUnicode(false);
        // O trigger de auditoria existe no script SQL. Declará-lo faz o EF usar "OUTPUT INTO" nos INSERTs
        // (sem isso, inserir em Matriculas falha com o erro 334 do SQL Server quando o trigger existe).
        modelBuilder.Entity<Matricula>().ToTable("Matriculas", "dbo", t => t.HasTrigger("trg_AuditoriaMatriculaStatus"));
        // Impede matrícula duplicada do mesmo aluno na mesma disciplina (mesma regra do script SQL).
        modelBuilder.Entity<Matricula>().HasIndex(m => new { m.AlunoId, m.CursoId }).IsUnique().HasFilter("[Status] <> 'Cancelada'");
        modelBuilder.Entity<Matricula>().HasOne(m => m.Aluno).WithMany().HasForeignKey(m => m.AlunoId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Matricula>().HasOne(m => m.Curso).WithMany().HasForeignKey(m => m.CursoId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ConteudoDisciplina>().HasOne(c => c.Curso).WithMany(c => c.Conteudos).HasForeignKey(c => c.CursoId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DuvidaForum>().HasOne(d => d.Curso).WithMany().HasForeignKey(d => d.CursoId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<DuvidaForum>().HasOne(d => d.Aluno).WithMany().HasForeignKey(d => d.AlunoId).OnDelete(DeleteBehavior.NoAction);
    }
}