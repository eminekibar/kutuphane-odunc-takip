using System.Net;
using System.Text;

namespace KutuphaneOdunc.Tests.Fakes;

// HttpClient'ın gerçek bir sunucuya gitmeden, verilen JSON'u cevap olarak döndürmesini sağlar.
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _json;

    public FakeHttpMessageHandler(string json)
    {
        _json = json;
    }

    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_json, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
