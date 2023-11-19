using System.Net;
using tests.Client;
using webapi.Models;
using webapi.JWT;

namespace tests.Api;

// AppSettings like production environment.
[TestCaseOrderer(
    ordererTypeName: "tests.Xunit.AlphabeticalOrderer",
    ordererAssemblyName: "tests")]
public class AppTest04 : IClassFixture<ApiFactory04<Program>> {
    private readonly ApiFactory04<Program> _factory;
    private readonly HttpClient _client;
    private readonly ODataEntityResponse<AccessAndRefreshToken> _appResp;
    private readonly AppClient _appClient;
    private readonly ODataEntityKeyResponse<Category, int?> _catResp;
    private readonly GenericCRUDClient<Category, int?> _catCRUDClient;

    public AppTest04(ApiFactory04<Program> factory) {
        _factory = factory;
        _client = _factory.CreateClient();
        _appResp = new ODataEntityResponse<AccessAndRefreshToken>();
        _appClient = new AppClient(_client, _appResp);
        _catResp = new ODataEntityKeyResponse<Category, int?>();
        _catCRUDClient = new GenericCRUDClient<Category, int?>(_client, _catResp);
    }

    private async Task<AccessAndRefreshToken?> EnsureLogged() {
        AccessAndRefreshToken? tokens = null;

        if (_appClient.Token == null)
            tokens = await _appClient.Login("test04", "pwd04");

        return tokens;
    }

    // Public methods.
    [Theory]
    [InlineData("/version")]
    [InlineData("/")]
    public async Task Test01_CanAccessPublicMethods(string url)
    {
        // Act.
        var response = await _client.GetAsync(url);

        // Assert.
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    // Protected methods.
    [Theory]
    [InlineData("/v1/expensereport")]
    [InlineData("/v1/expenseexpanded")]
    [InlineData("/v1/expense")]
    [InlineData("/v1/category")]
    [InlineData("/export")]
    public async Task Test02_CanNotAccessAuthorizedMethods(string url)
    {
        // Act.
        var response = await _client.GetAsync(url);

        // Assert.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Hidden methods.
    [Theory]
    [InlineData("/login")]
    [InlineData("/refreshtoken")]
    [InlineData("/logout")]
    public async Task Test03_CanNotAcessHiddenMethods(string url)
    {
        // Act.
        var response = await _client.GetAsync(url);

        // Assert.
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    // Non-existent methods.
    [Theory]
    [InlineData("/import")]
    [InlineData("/v1/user")]
    public async Task Test04_CanNotAcessInexistentMethods(string url)
    {
        // Act.
        var response = await _client.GetAsync(url);

        // Assert.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}