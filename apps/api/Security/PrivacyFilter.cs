using System.Text.RegularExpressions;
using FacilMunicipio.Api.Contracts;
namespace FacilMunicipio.Api.Security;
// Conservative baseline, not the separate Cybersecurity discipline's taint-analysis module.
public static partial class PrivacyFilter
{
    [GeneratedRegex(@"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b|[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}|\b(?:senha|password)\s*[:=]\s*\S+", RegexOptions.IgnoreCase)]
    private static partial Regex Sensitive();
    public static string Clean(string text) => Sensitive().Replace(text, "[DADO REMOVIDO]");
    public static TrocaResumida[] Clean(TrocaResumida[]? history) => (history ?? [])
        .Select(x => new TrocaResumida(Clean(x.Pergunta), Clean(x.Resposta))).ToArray();
}
