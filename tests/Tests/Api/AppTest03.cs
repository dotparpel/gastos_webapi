using System.Net;
using tests.Client;
using webapi.Models;
using webapi.JWT;

namespace tests.Api;

// AppSettings like production environment.
[TestCaseOrderer(
    ordererTypeName: "tests.Xunit.AlphabeticalOrderer",
    ordererAssemblyName: "tests")]
public class AppTest03 : IClassFixture<ApiFactory03<Program>> {
    private readonly ApiFactory03<Program> _factory;
    private readonly HttpClient _client;
    private readonly ODataEntityResponse<AccessAndRefreshToken> _appResp;
    private readonly AppClient _appClient;
    private readonly ODataEntityKeyResponse<Category, int?> _catResp;
    private readonly GenericCRUDClient<Category, int?> _catCRUDClient;

    public AppTest03(ApiFactory03<Program> factory) {
        _factory = factory;
        _client = _factory.CreateClient();
        _appResp = new ODataEntityResponse<AccessAndRefreshToken>();
        _appClient = new AppClient(_client, _appResp);
        _catResp = new ODataEntityKeyResponse<Category, int?>();
        _catCRUDClient = new GenericCRUDClient<Category, int?>(_client, _catResp);
    }

    private string? GetData(HttpResponseMessage? response) => response?.Content.ReadAsStringAsync().Result;

    private async Task<AccessAndRefreshToken?> EnsureLogged() {
        AccessAndRefreshToken? tokens = null;

        if (_appClient.Token == null)
            tokens = await _appClient.Login("test03", "pwd03");

        return tokens;
    }

    [Fact]
    public async Task Test01_RefreshWithExpiredAccessTokenAndValidRefreshToken()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        // Test any method is authorized.
        var response = await _client.GetAsync("/v1/category");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Await 5 seconds (assuming the access token expires in 3 seconds).
        await Task.Delay(5 * 1000);

        // Test any method is unauthorized.
        response = await _client.GetAsync("/v1/category");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        AccessAndRefreshToken? tokensRefresh = await _appClient.RefreshToken(tokens);
        Assert.Equal(HttpStatusCode.OK, _appClient.StatusCode);

        // Assert.
        Assert.Equal(HttpStatusCode.OK, _appClient.StatusCode);
        Assert.NotNull(tokensRefresh);
        Assert.NotEmpty(tokensRefresh.token_access ?? "");
        Assert.NotEmpty(tokensRefresh.token_refresh ?? "");

        Assert.NotEqual(tokens.token_access, tokensRefresh.token_access);
        Assert.NotEqual(tokens.token_refresh, tokensRefresh.token_refresh);

        // Test any method is authorized.
        response = await _client.GetAsync("/v1/category");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Test02_RefreshWithExpiredAccessTokenAndInvalidRefreshToken()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        // Await 5 seconds (assuming the access token expires in 3 seconds).
        await Task.Delay(5 * 1000);

        AccessAndRefreshToken? tokensRefresh = await _appClient.RefreshToken(tokens.token_access ?? "", "ThisIsAnInvalidToken");

        // Assert.
        Assert.Equal(HttpStatusCode.Unauthorized, _appClient.StatusCode);
        Assert.Null(tokensRefresh);
    }

    [Fact]
    public async Task Test03_RefreshWithExpiredRefreshToken()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        // Test any method is authorized.
        var response = await _client.GetAsync("/v1/category");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Await 10 seconds (assuming the access token expires in 3 seconds and the refresh token expires in 9 seconds).
        await Task.Delay(10 * 1000);

        // Test any method is unauthorized.
        response = await _client.GetAsync("/v1/category");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        AccessAndRefreshToken? tokensRefresh = await _appClient.RefreshToken(tokens);
        Assert.Equal(HttpStatusCode.Unauthorized, _appClient.StatusCode);
    }
}