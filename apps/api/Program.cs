using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FacilMunicipio.Api.Data;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Security;
using FacilMunicipio.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using FacilMunicipio.Api.OpenApi;
using Neo4j.Driver;

var builder = WebApplication.CreateBuilder(args);
var key = builder.Configuration["Jwt:Key"] ?? "";
if (Encoding.UTF8.GetByteCount(key) < 32) throw new InvalidOperationException("Configure Jwt__Key com pelo menos 32 bytes aleatórios.");
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 131072);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
builder.Services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = _ => new UnprocessableEntityObjectResult(
    new ProblemDetails { Status = 422, Title = "Dados inválidos. Confira os campos e limites documentados." }));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "FacilMunicipio.Api.xml"));
    o.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token JWT obtido em POST /api/v1/auth/login."
    });
    o.OperationFilter<BearerAuthOperationFilter>();
});
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(builder.Configuration["Neo4j:Uri"] ?? "bolt://localhost:7687",
    AuthTokens.Basic(builder.Configuration["Neo4j:User"] ?? "neo4j", builder.Configuration["Neo4j:Password"] ?? ""),
    c => c.WithConnectionTimeout(TimeSpan.FromSeconds(3)).WithMaxTransactionRetryTime(TimeSpan.FromSeconds(1))));
builder.Services.AddSingleton<IKnowledgeGraph, Neo4jKnowledgeGraph>();
// The demonstration mode only repeats published and reviewed text; it is NOT an LLM.
if (string.Equals(builder.Configuration["Llm:Mode"], "demo", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<ILlmWiki, DemoGroundedWiki>();
else
    builder.Services.AddScoped<ILlmWiki, LlmUnavailable>();
builder.Services.AddSingleton<SecurityAudit>();
builder.Services.AddScoped<SinkGuard>();
builder.Services.AddHttpClient<IRiskTranslator, OpenAiCompatibleRiskTranslator>(c => c.Timeout = TimeSpan.FromSeconds(8));
builder.Services.AddScoped<RiskTranslationProcessor>();
if (builder.Configuration.GetValue("RiskTranslator:WorkerEnabled", true)) builder.Services.AddHostedService<RiskTranslationWorker>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<PublicationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new()
{
    ValidateIssuer = true, ValidIssuer = "facil-municipio", ValidateAudience = true, ValidAudience = "facil-admin",
    ValidateLifetime = true, ValidateIssuerSigningKey = true, ClockSkew = TimeSpan.FromSeconds(30),
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
});
builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(
    builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:8081"])
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    foreach (var (name, limit) in new[] { ("conversation", 10), ("login", 5) })
        o.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
            { PermitLimit = limit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
});
var app = builder.Build();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (Exception e) when (e is not OperationCanceledException || !context.RequestAborted.IsCancellationRequested)
    {
        var status = e switch
        {
            ApiException apiError => apiError.Status,
            DbUpdateConcurrencyException => 409,
            DbUpdateException => 409,
            LlmUnavailableException or OperationCanceledException or Neo4jException or Npgsql.NpgsqlException => 503,
            _ => 500
        };
        // No exception message, body, question, history, SQL parameters or sensitive source text in logs.
        app.Logger.LogWarning("Requisição falhou. Status={Status} Tipo={Tipo}", status, e.GetType().Name);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status,
            Title = e is ApiException apiErrorDetail ? apiErrorDetail.Message : status == 503 ? "Serviço temporariamente indisponível." : "Não foi possível concluir a operação." });
    }
});
// Sem MapGet para /auditoria: um endpoint casado (mesmo com barra final) impede o arquivo estático.
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/auditoria") context.Response.Redirect("/auditoria/");
    else await next(context);
});
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = c =>
{
    // Página do painel é estática e pública, sem dados; os eventos vêm só das APIs autorizadas.
    c.Context.Response.Headers["Cache-Control"] = "no-store";
    c.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    c.Context.Response.Headers["Referrer-Policy"] = "no-referrer";
    c.Context.Response.Headers["X-Frame-Options"] = "DENY";
} });
app.UseCors(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
app.UseSwagger(); app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "UP" }));
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    return;
}
if (args.Contains("--bootstrap"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var email = builder.Configuration["Bootstrap:Email"]?.Trim().ToLowerInvariant();
    var password = builder.Configuration["Bootstrap:Password"] ?? "";
    if (string.IsNullOrWhiteSpace(email) || password.Length < 12) throw new InvalidOperationException("Configure Bootstrap__Email e Bootstrap__Password (mínimo 12 caracteres).");
    if (!await db.Usuarios.AnyAsync(x => x.Email == email))
    {
        var user = new Usuario { Email = email, Perfil = "OPERADOR" };
        user.SenhaHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>().HashPassword(user, password);
        db.Usuarios.Add(user); await db.SaveChangesAsync();
    }
    return;
}
app.Run();
public partial class Program { }
