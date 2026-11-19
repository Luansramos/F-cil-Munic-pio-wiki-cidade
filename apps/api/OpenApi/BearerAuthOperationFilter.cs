using FacilMunicipio.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FacilMunicipio.Api.OpenApi;

/// <summary>
/// Marca no Swagger apenas as ações que exigem autenticação administrativa.
/// As consultas do cidadão permanecem públicas.
/// </summary>
public sealed class BearerAuthOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.MethodInfo;
        var controller = method.DeclaringType;
        var anonymous = method.IsDefined(typeof(AllowAnonymousAttribute), true)
            || controller?.IsDefined(typeof(AllowAnonymousAttribute), true) == true;

        var protectedAction = method.IsDefined(typeof(AuthorizeAttribute), true)
            || controller?.IsDefined(typeof(AuthorizeAttribute), true) == true;

        if (anonymous || !protectedAction)
            return;

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", context.Document)] = []
            }
        ];

        operation.Responses ??= [];
        if (!operation.Responses.ContainsKey("401"))
            operation.Responses.Add("401", new OpenApiResponse { Description = "Não autenticado." });
        if (!operation.Responses.ContainsKey("403"))
            operation.Responses.Add("403", new OpenApiResponse { Description = "Sem permissão." });
    }
}
