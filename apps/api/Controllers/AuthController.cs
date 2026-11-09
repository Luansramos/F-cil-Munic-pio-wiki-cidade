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
[ApiController, Route("api/v1/auth")]
public class AuthController(AppDbContext db, IPasswordHasher<Usuario> passwords, IConfiguration config) : ControllerBase
{
    [HttpPost("login"), EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Usuarios.SingleOrDefaultAsync(x => x.Email == email, ct);
        if (user is null || passwords.VerifyHashedPassword(user, user.SenhaHash, request.Senha) == PasswordVerificationResult.Failed)
            return Unauthorized();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Role, user.Perfil) };
        if (user.MunicipioId is not null) claims.Add(new("municipioId", user.MunicipioId));
        var token = new JwtSecurityToken("facil-municipio", "facil-admin", claims, expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), tipo = "Bearer", expiresIn = 1800 });
    }
}
