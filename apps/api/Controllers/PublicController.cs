using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
namespace FacilMunicipio.Api.Controllers;
[ApiController, Route("api/v1/municipios")]
public class PublicController(AppDbContext db, IKnowledgeGraph graph, ConversationService conversation) : ControllerBase
{
    /// <summary>Municípios cadastrados; cidades sem versão publicada têm lista de serviços vazia.</summary>
    [HttpGet]
    public async Task<ActionResult<MunicipioDto[]>> List(CancellationToken ct) => await db.Municipios.AsNoTracking()
        .OrderBy(x => x.Nome).Select(x => new MunicipioDto(x.Id, x.Nome, x.Uf)).ToArrayAsync(ct);
    [HttpGet("{id}/servicos")]
    public async Task<ActionResult<ServicoDto[]>> Services(string id, CancellationToken ct)
    {
        var m = await db.Municipios.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();
        if (m.VersaoAtivaId is not Guid v) return Array.Empty<ServicoDto>();
        return (await graph.ReadAsync(id, v, ct)).Select(x => new ServicoDto(x.Servico.Id, x.Servico.Nome)).ToArray();
    }
    /// <summary>Consulta pública sem persistência de pergunta ou histórico; contexto limitado a três trocas.</summary>
    [HttpPost("{id}/conversas"), EnableRateLimiting("conversation")]
    [ProducesResponseType<RespostaConversa>(200), ProducesResponseType(422), ProducesResponseType(429), ProducesResponseType(503)]
    public async Task<ActionResult<RespostaConversa>> Ask(string id, EnvioPergunta request, CancellationToken ct)
        => await conversation.AskAsync(id, request, ct);
}
