using System.Text.Json;
using FacilMunicipio.Api.Contracts;
using Neo4j.Driver;
namespace FacilMunicipio.Api.Services;
public class Neo4jKnowledgeGraph(IDriver driver) : IKnowledgeGraph
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task WriteAsync(string municipioId, Guid versao, IReadOnlyList<Conhecimento> servicos, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await using var session = driver.AsyncSession();
        // Stable compound keys prevent duplicates on retry; each write is one graph transaction.
        await session.ExecuteWriteAsync(async tx =>
        {
            foreach (var k in servicos)
            {
                ct.ThrowIfCancellationRequested();
                var key = $"{municipioId}:{versao}:{k.Servico.Id}";
                var cursor = await tx.RunAsync("""
                    MERGE (s:Servico {key: $key})
                    SET s.municipioId=$municipio, s.versao=$versao, s.id=$id, s.nome=$nome, s.payload=$payload
                    MERGE (f:Fonte {key: $key}) SET f.titulo=$titulo, f.url=$url
                    MERGE (s)-[:TEM_FONTE]->(f)
                    """, new { key, municipio = municipioId, versao = versao.ToString(), id = k.Servico.Id,
                        nome = k.Servico.Nome, payload = JsonSerializer.Serialize(k, Json), titulo = k.Fonte.Titulo, url = k.Fonte.Url });
                await cursor.ConsumeAsync();
                // Components inherit municipality/version through the unique service-version key.
                foreach (var (label, relation, values) in new[] {
                    ("Etapa", "POSSUI_ETAPA", k.Servico.Etapas),
                    ("Requisito", "POSSUI_REQUISITO", k.Servico.Requisitos),
                    ("Orgao", "TEM_RESPONSAVEL", k.Servico.Orgao is null ? Array.Empty<string>() : new[] { k.Servico.Orgao }),
                    ("Canal", "USA_CANAL", k.Servico.CanalUrl is null ? Array.Empty<string>() : new[] { k.Servico.CanalUrl }) })
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        // Labels are internal constants, never supplied by the LLM/client.
                        var c = await tx.RunAsync($"MATCH (s:Servico {{key:$key}})-[:TEM_FONTE]->(f) MERGE (n:{label} {{key:$componentKey}}) SET n.texto=$texto MERGE (s)-[:{relation}]->(n) MERGE (n)-[:TEM_FONTE]->(f)",
                            new { key, componentKey = $"{key}:{label}:{i}", texto = values[i] });
                        await c.ConsumeAsync();
                    }
                }
            }
        }, config => config.WithTimeout(TimeSpan.FromSeconds(8)));
    }
    public async Task<IReadOnlyList<Conhecimento>> ReadAsync(string municipioId, Guid versao, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await using var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
        var records = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync("MATCH (s:Servico) WHERE s.municipioId=$municipio AND s.versao=$versao RETURN s.payload AS payload ORDER BY s.id",
                new { municipio = municipioId, versao = versao.ToString() });
            return await cursor.ToListAsync(r => JsonSerializer.Deserialize<Conhecimento>(r["payload"].As<string>(), Json)!);
        }, config => config.WithTimeout(TimeSpan.FromSeconds(5)));
        ct.ThrowIfCancellationRequested();
        return records;
    }
}
