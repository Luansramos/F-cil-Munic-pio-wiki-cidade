namespace FacilMunicipio.Api.Domain;

public class Municipio
{
    public string Id { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Uf { get; set; } = "";
    public string? CanalOficialUrl { get; set; }
    public Guid? VersaoAtivaId { get; set; }
}
public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string SenhaHash { get; set; } = "";
    public string Perfil { get; set; } = "ADMIN";
    public string? MunicipioId { get; set; }
}
public class Fonte
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MunicipioId { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Url { get; set; } = "";
    public string Texto { get; set; } = "";
    public DateOnly DataConsulta { get; set; }
}
public class Rascunho
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FonteId { get; set; }
    public string MunicipioId { get; set; } = "";
    public string ConteudoJson { get; private set; } = "{}";
    public Guid RevisaoId { get; private set; } = Guid.NewGuid();
    public bool Aprovado { get; private set; }
    public Guid? RevisadoPor { get; private set; }
    public DateTimeOffset? RevisadoEm { get; private set; }
    public void Revisar(string conteudoJson, bool aprovado, Guid usuarioId)
    {
        ConteudoJson = conteudoJson;
        Aprovado = aprovado;
        RevisadoPor = usuarioId;
        RevisadoEm = DateTimeOffset.UtcNow;
        RevisaoId = Guid.NewGuid();
    }
}
public class Versao
{
    public Guid Id { get; set; }
    public string MunicipioId { get; set; } = "";
    public Guid RascunhoId { get; set; }
    public DateTimeOffset CriadaEm { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; private set; } = "PENDENTE";
    public void Publicar() => Status = "PUBLICADA";
}
