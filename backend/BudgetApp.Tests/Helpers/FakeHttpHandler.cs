using System.Net;
using System.Text;

namespace BudgetApp.Tests.Helpers;

internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly string _responseJson;
    private readonly Action<HttpRequestMessage>? _inspect;

    public FakeHttpHandler(string responseJson, Action<HttpRequestMessage>? inspect = null)
    {
        _responseJson = responseJson;
        _inspect = inspect;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _inspect?.Invoke(request);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
        });
    }
}
