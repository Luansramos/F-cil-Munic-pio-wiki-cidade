using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Security;
using FacilMunicipio.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace FacilMunicipio.Api.Controllers;

[ApiController, Route("api/v1/municipios/{id}/seguranca"), Authorize(Roles = "ADMIN,OPERADOR")]
public class SecurityController(AppDbContext db, CurrentUser user) : ControllerBase
{
    /// <summary>Painel de auditoria de privacidade do município: indicador Risco Cidadão, contagens e últimos eventos.</summary>
    /// <param name="id">Município. ADMIN só acessa o próprio; OPERADOR segue a política administrativa existente.</param>
    /// <param name="horas">Período de 1 a 168 horas (padrão 24).</param>
    /// <param name="ct">Cancelamento da requisição.</param>
    [HttpGet("painel")]
    [ProducesResponseType<PainelSeguranca>(200)]
    public async Task<ActionResult<PainelSeguranca>> Panel(string id, [FromQuery] int horas = 24, CancellationToken ct = default)
    {
        user.RequireMunicipio(id);
        if (horas is < 1 or > 168) return UnprocessableEntity(new ProblemDetails { Status = 422, Title = "O período deve ter de 1 a 168 horas." });
        if (!await db.Municipios.AnyAsync(x => x.Id == id, ct)) return NotFound();
        var since = DateTimeOffset.UtcNow.AddHours(-horas);
        var window = db.EventosSeguranca.AsNoTracking().Where(e => e.MunicipioId == id && e.OcorridoEm >= since);
        var totals = await window.GroupBy(e => new { e.Acao, e.Gravidade, e.TraducaoEstado })
            .Select(g => new { g.Key.Acao, g.Key.Gravidade, g.Key.TraducaoEstado, N = g.Count() }).ToListAsync(ct);
        var summary = new ResumoSeguranca(totals.Sum(t => t.N),
            totals.Where(t => t.Acao == AcaoProtecao.Sanitizado).Sum(t => t.N),
            totals.Where(t => t.Acao == AcaoProtecao.Bloqueado).Sum(t => t.N),
            totals.Where(t => t.Acao == AcaoProtecao.Pendente).Sum(t => t.N),
            totals.Where(t => t.TraducaoEstado != TraducaoEstado.Traduzida).Sum(t => t.N));
        var indicator = RiskRules.Indicador(totals.Select(t => (t.Acao, t.Gravidade)).Distinct().ToArray(), horas);
        var events = (await window.OrderByDescending(e => e.OcorridoEm).Take(50).ToListAsync(ct)).Select(e =>
        {
            var ai = e.TraducaoEstado == TraducaoEstado.Traduzida && !string.IsNullOrWhiteSpace(e.TraducaoTexto);
            return new EventoSegurancaDto(e.Id, e.OcorridoEm, e.Categoria.ToString(), e.Origem, e.Destino.ToString(), e.Acao.ToString(),
                e.Gravidade.ToString(), e.Codigo, e.TraducaoEstado.ToString(),
                ai ? e.TraducaoTexto! : RiskRules.MensagemPadrao(e.Acao), ai ? "IA" : "PADRAO", ai ? e.TraducaoModelo : null);
        }).ToArray();
        return new PainelSeguranca(horas, DateTimeOffset.UtcNow, indicator, summary, events);
    }
}
