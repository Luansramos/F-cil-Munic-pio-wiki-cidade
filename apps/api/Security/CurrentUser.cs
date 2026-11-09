using System.Security.Claims;
using FacilMunicipio.Api.Services;
namespace FacilMunicipio.Api.Security;
public class CurrentUser(IHttpContextAccessor accessor)
{
    private ClaimsPrincipal User => accessor.HttpContext!.User;
    public Guid Id => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public void RequireMunicipio(string municipio)
    {
        if (!User.IsInRole("OPERADOR") && User.FindFirstValue("municipioId") != municipio)
            throw new ApiException(403, "Acesso não permitido a este município.");
    }
}
