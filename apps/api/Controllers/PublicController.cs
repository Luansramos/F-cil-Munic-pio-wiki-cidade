using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FacilMunicipio.Api.Controllers;

[ApiController]
[Route("api/v1/municipios")]
public class PublicController(
    AppDbContext db,
    IKnowledgeGraph graph,
    ConversationService conversation) : ControllerBase
{
    /// <summary>Lista os municípios cadastrados.</summary>
    [HttpGet]
    public async Task<ActionResult<MunicipioDto[]>> List(CancellationToken ct)
    {
        return await db.Municipios
            .AsNoTracking()
            .OrderBy(m => m.Nome)
            .Select(m => new MunicipioDto(m.Id, m.Nome, m.Uf))
            .ToArrayAsync(ct);
    }

    /// <summary>Lista apenas os serviços publicados do município informado.</summary>
    [HttpGet("{id}/servicos")]
    public async Task<ActionResult<ServicoDto[]>> Services(string id, CancellationToken ct)
    {
        var municipio = await db.Municipios
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == id, ct);

        if (municipio is null)
            return NotFound();

        if (municipio.VersaoAtivaId is not Guid versao)
            return Array.Empty<ServicoDto>();

        var servicos = await graph.ReadAsync(id, versao, ct);

        return servicos
            .Select(item => new ServicoDto(item.Servico.Id, item.Servico.Nome))
            .ToArray();
    }

    /// <summary>Consulta o conhecimento publicado sem salvar a pergunta no servidor.</summary>
    [HttpPost("{id}/conversas")]
    [EnableRateLimiting("conversation")]
    [ProducesResponseType<RespostaConversa>(200)]
    [ProducesResponseType(422)]
    [ProducesResponseType(429)]
    [ProducesResponseType(503)]
    public async Task<ActionResult<RespostaConversa>> Ask(
        string id,
        EnvioPergunta request,
        CancellationToken ct)
    {
        return await conversation.AskAsync(id, request, ct);
    }
}
