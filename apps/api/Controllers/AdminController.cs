using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Security;
using FacilMunicipio.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace FacilMunicipio.Api.Controllers;
[ApiController, Route("api/v1"), Authorize(Roles = "ADMIN,OPERADOR")]
public class AdminController(AppDbContext db, CurrentUser user, IPasswordHasher<Usuario> passwords,
    ILlmWiki llm, PublicationService publisher) : ControllerBase
{
    [HttpPost("municipios"), Authorize(Roles = "OPERADOR")]
    public async Task<IActionResult> CreateMunicipio(CriarMunicipio request, CancellationToken ct)
    {
        if (await db.Municipios.AnyAsync(x => x.Id == request.Id, ct)) return Conflict();
        var m = new Municipio { Id = request.Id, Nome = request.Nome.Trim(), Uf = request.Uf, CanalOficialUrl = request.CanalOficialUrl };
        db.Municipios.Add(m); await db.SaveChangesAsync(ct);
        return Created($"/api/v1/municipios/{m.Id}/servicos", new MunicipioDto(m.Id, m.Nome, m.Uf));
    }
    [HttpPost("usuarios"), Authorize(Roles = "OPERADOR")]
    public async Task<IActionResult> CreateUser(CriarUsuario request, CancellationToken ct)
    {
        if (!await db.Municipios.AnyAsync(x => x.Id == request.MunicipioId, ct)) return NotFound();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(x => x.Email == email, ct)) return Conflict();
        var u = new Usuario { Email = email, MunicipioId = request.MunicipioId };
        u.SenhaHash = passwords.HashPassword(u, request.Senha); db.Usuarios.Add(u); await db.SaveChangesAsync(ct);
        return StatusCode(201, new { u.Id, u.Email, u.Perfil, u.MunicipioId });
    }
    [HttpPost("municipios/{id}/fontes")]
    public async Task<IActionResult> CreateSource(string id, CriarFonte request, CancellationToken ct)
    {
        user.RequireMunicipio(id);
        if (!await db.Municipios.AnyAsync(x => x.Id == id, ct)) return NotFound();
        if (request.DataConsulta == default || request.DataConsulta > DateOnly.FromDateTime(DateTime.UtcNow)) return UnprocessableEntity();
        var f = new Fonte { MunicipioId = id, Titulo = request.Titulo, Url = request.Url, Texto = request.Texto, DataConsulta = request.DataConsulta };
        db.Fontes.Add(f); await db.SaveChangesAsync(ct); return StatusCode(201, f);
    }
    [HttpGet("municipios/{id}/fontes")]
    public async Task<IActionResult> Sources(string id, CancellationToken ct)
    {
        user.RequireMunicipio(id);
        return Ok(await db.Fontes.AsNoTracking().Where(x => x.MunicipioId == id).OrderBy(x => x.Titulo).ToArrayAsync(ct));
    }
    [HttpPost("fontes/{id:guid}/rascunhos")]
    public async Task<IActionResult> ManualDraft(Guid id, CriarRascunho request, CancellationToken ct)
        => await SaveDraft(id, request.Servico, ct);
    [HttpPost("fontes/{id:guid}/extracoes")]
    public async Task<IActionResult> Extract(Guid id, CancellationToken ct)
    {
        var f = await Source(id, ct);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(TimeSpan.FromSeconds(12));
        var proposal = await llm.ExtrairAsync(PrivacyFilter.Clean(f.Texto), budget.Token);
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(proposal, new(proposal), errors, true)) return StatusCode(503);
        return await SaveDraft(id, proposal, ct);
    }
    private async Task<Fonte> Source(Guid id, CancellationToken ct)
    {
        var f = await db.Fontes.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ApiException(404, "Fonte não encontrada.");
        user.RequireMunicipio(f.MunicipioId); return f;
    }
    private async Task<IActionResult> SaveDraft(Guid id, ServicoRevisado service, CancellationToken ct)
    {
        var f = await Source(id, ct);
        var draft = new Rascunho { FonteId = id, MunicipioId = f.MunicipioId };
        draft.Revisar(JsonSerializer.Serialize(service), false, user.Id);
        db.Rascunhos.Add(draft); await db.SaveChangesAsync(ct);
        return Created($"/api/v1/rascunhos/{draft.Id}", View(draft));
    }
    private static object View(Rascunho d) => new { d.Id, d.FonteId, d.MunicipioId, d.RevisaoId, d.Aprovado, d.RevisadoPor,
        d.RevisadoEm, servico = JsonSerializer.Deserialize<ServicoRevisado>(d.ConteudoJson) };
    [HttpGet("rascunhos/{id:guid}")]
    public async Task<IActionResult> Draft(Guid id, CancellationToken ct)
    {
        var d = await db.Rascunhos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (d is null) return NotFound(); user.RequireMunicipio(d.MunicipioId); return Ok(View(d));
    }
    [HttpPut("rascunhos/{id:guid}")]
    public async Task<IActionResult> Review(Guid id, RevisarRascunho request, CancellationToken ct)
    {
        var city = await db.Rascunhos.AsNoTracking().Where(x => x.Id == id).Select(x => x.MunicipioId).SingleOrDefaultAsync(ct);
        if (city is null) return NotFound(); user.RequireMunicipio(city);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({city}))", ct);
        var d = await db.Rascunhos.SingleAsync(x => x.Id == id, ct);
        if (d.RevisaoId != request.RevisaoEsperada) return Conflict(new { mensagem = "O rascunho mudou. Recarregue antes de salvar." });
        d.Revisar(JsonSerializer.Serialize(request.Servico), request.Aprovado, user.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(View(d));
    }
    [HttpPost("rascunhos/{id:guid}/publicacao")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct) => Ok(await publisher.PublishAsync(id, ct));
    [HttpGet("municipios/{id}/versoes")]
    public async Task<IActionResult> Versions(string id, CancellationToken ct)
    {
        user.RequireMunicipio(id);
        return Ok(await db.Versoes.AsNoTracking().Where(x => x.MunicipioId == id).OrderByDescending(x => x.CriadaEm).ToArrayAsync(ct));
    }
}
