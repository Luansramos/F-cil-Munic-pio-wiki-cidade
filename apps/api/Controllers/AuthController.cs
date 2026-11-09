using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FacilMunicipio.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    AppDbContext db,
    IPasswordHasher<Usuario> passwords,
    IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Email == email, ct);

        if (usuario is null)
            return Unauthorized();

        var verificacao = passwords.VerifyHashedPassword(
            usuario, usuario.SenhaHash, request.Senha);

        if (verificacao == PasswordVerificationResult.Failed)
            return Unauthorized();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Role, usuario.Perfil)
        };

        if (usuario.MunicipioId is not null)
            claims.Add(new Claim("municipioId", usuario.MunicipioId));

        var chave = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

        var jwt = new JwtSecurityToken(
            issuer: "facil-municipio",
            audience: "facil-admin",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(jwt),
            tipo = "Bearer",
            expiresIn = 1800
        });
    }
}
