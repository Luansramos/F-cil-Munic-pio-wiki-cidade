using System.ComponentModel.DataAnnotations;
using FacilMunicipio.Api.Contracts;
using FacilMunicipio.Api.Domain;
using FacilMunicipio.Api.Security;
namespace Api.Tests;
public class DomainTests
{
    [Fact] public void RevisionChangesVersionAndRecordsReviewer()
    {
        var d = new Rascunho(); var previous = d.RevisaoId; var user = Guid.NewGuid();
        d.Revisar("{}", true, user);
        Assert.NotEqual(previous, d.RevisaoId); Assert.True(d.Aprovado); Assert.Equal(user, d.RevisadoPor);
        d.Revisar("{}", false, user); Assert.False(d.Aprovado);
    }
    [Theory] [InlineData("https://example.org", true)] [InlineData("http://example.org", false)]
    [InlineData("javascript:alert(1)", false)] [InlineData("https://user:pass@example.org", false)]
    public void SourceLinksRequireHttps(string url, bool valid) => Assert.Equal(valid, new HttpsUrlAttribute().IsValid(url));
    [Fact] public void SensitiveFieldsRemovedFromBothHistoryFields()
    {
        var history = PrivacyFilter.Clean([new("CPF 123.456.789-00", "senha=simulada teste@example.org")]);
        Assert.DoesNotContain("123.456", history[0].Pergunta); Assert.DoesNotContain("simulada", history[0].Resposta);
        Assert.DoesNotContain("@", history[0].Resposta);
    }
    [Fact] public void QuestionOverLimitRejected()
    {
        var request = new EnvioPergunta(new string('a',501), null);
        Assert.False(Validator.TryValidateObject(request, new(request), [], true));
    }
    [Fact] public void InsufficientResponseNeverInventsSources()
    {
        var r = RespostaConversa.Insuficiente(null); Assert.False(r.InformacaoSuficiente);
        Assert.Empty(r.Fontes); Assert.Empty(r.ServicosRelacionados); Assert.Null(r.CanalOficialUrl);
    }
}
