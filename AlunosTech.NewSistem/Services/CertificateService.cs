namespace AlunosTech.NewSistem.Services;

public sealed record CertificateResult(bool Aprovado, Guid? Codigo, decimal Nota, DateTime DataEmissao);

public interface ICertificateService
{
    decimal NotaMinima { get; }
    CertificateResult Create(decimal nota);
    CertificateResult Restore(decimal nota, Guid? codigo, DateTime dataEmissao);
}

public sealed class CertificateService : ICertificateService
{
    public decimal NotaMinima => 7m;

    public CertificateResult Create(decimal nota)
    {
        var aprovado = nota >= NotaMinima;
        return new CertificateResult(aprovado, aprovado ? Guid.NewGuid() : null, nota, DateTime.UtcNow);
    }

    // Reconstrói um certificado já emitido (sem gerar novo código).
    public CertificateResult Restore(decimal nota, Guid? codigo, DateTime dataEmissao) =>
        new(codigo.HasValue && nota >= NotaMinima, codigo, nota, dataEmissao);
}
