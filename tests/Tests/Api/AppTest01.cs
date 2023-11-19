using System.Net;
using tests.Client;
using tests.Extensions;
using webapi.Models;
using webapi.Repository;
using webapi.JWT;
using webapi.Application;

namespace tests.Api;

// AppSettings like production environment.
[TestCaseOrderer(
    ordererTypeName: "tests.Xunit.AlphabeticalOrderer",
    ordererAssemblyName: "tests")]
public class AppTest01 : IClassFixture<ApiFactory01<Program>> {
    private readonly ApiFactory01<Program> _factory;
    private readonly IAppSettings? _app;
    private readonly HttpClient _client;
    private readonly ODataEntityResponse<AccessAndRefreshToken> _appResp;
    private readonly AppClient _appClient;
    private readonly ODataEntityKeyResponse<Category, int?> _catResp;
    private readonly GenericCRUDClient<Category, int?> _catCRUDClient;

    public AppTest01(ApiFactory01<Program> factory) {
        _factory = factory;
        _app = (IAppSettings?) _factory.GetService(typeof(IAppSettings));
        _client = _factory.CreateClient();
        _appResp = new ODataEntityResponse<AccessAndRefreshToken>();
        _appClient = new AppClient(_client, _appResp);
        _catResp = new ODataEntityKeyResponse<Category, int?>();
        _catCRUDClient = new GenericCRUDClient<Category, int?>(_client, _catResp);
    }

    private async Task<AccessAndRefreshToken?> EnsureLogged(
        decimal? accessTokenExpirationMinutes = null, decimal? refreshTokenExpirationMinutes = null
    ) {
        AccessAndRefreshToken? tokens = null;

        if (_appClient.Token == null)
            tokens = await _appClient.Login("test01", "pwd01"
                , accessTokenExpirationMinutes, refreshTokenExpirationMinutes);

        return tokens;
    }

    private async Task<bool> EnsureLogout() {
        bool ret = true;

        if (_appClient.Tokens != null)
            ret = await _appClient.Logout();

        return ret;
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
    [InlineData("/import")]
    [InlineData("/v1/user")]
    public async Task Test03_CanNotAcessHiddenMethods(string url)
    {
        // Act.
        var response = await _client.GetAsync(url);

        // Assert.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Login.
    [Fact]
    public async Task Test04_LoginOk()
    {
        AccessAndRefreshToken? tokens = await _appClient.Login("test01", "pwd01");

        // Assert.
        Assert.Equal(HttpStatusCode.OK, _appClient.StatusCode);
        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens.token_access ?? "");
    }

    [Fact]
    public async Task Test05_LoginWithoutParameters()
    {
        // Act
        var response = await _client.GetAsync("/login");
        AccessAndRefreshToken? tokens = _appResp.GetOdataEntity(response);

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Null(tokens);
    }

    [Fact]
    public async Task Test06_LoginWrongParameters()
    {
        AccessAndRefreshToken? tokens = await _appClient.Login("aNonExistentUser", "aNonExistentPassword");

        // Assert.
        Assert.Equal(HttpStatusCode.Unauthorized, _appClient.StatusCode);
        Assert.Null(tokens);
    }
    
    [Fact]
    public async Task Test07_RefreshOk()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        AccessAndRefreshToken? tokensRefresh = await _appClient.RefreshToken(tokens);

        // Assert.
        Assert.Equal(HttpStatusCode.OK, _appClient.StatusCode);
        Assert.NotNull(tokensRefresh);
        Assert.NotEmpty(tokensRefresh.token_access ?? "");
        Assert.NotEmpty(tokensRefresh.token_refresh ?? "");

        Assert.NotEqual(tokens.token_access, tokensRefresh.token_access);
        Assert.NotEqual(tokens.token_refresh, tokensRefresh.token_refresh);
    }

    [Fact]
    public async Task Test08_RefreshWithoutParameters()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        AccessAndRefreshToken? tokensRefresh = await _appClient.RefreshToken(null);

        // Assert.
        Assert.Equal(HttpStatusCode.Unauthorized, _appClient.StatusCode);
        Assert.Null(tokensRefresh);
    }

    [Fact]
    public async Task Test09_RefreshWithoutAccessToken()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        AccessAndRefreshToken? tokensRefresh = await _appClient.RefreshToken("", tokens.token_refresh ?? "");

        // Assert.
        Assert.Equal(HttpStatusCode.Unauthorized, _appClient.StatusCode);
        Assert.Null(tokensRefresh);
    }

    [Fact]
    public async Task Test10_RefreshWithoutRefreshToken()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        AccessAndRefreshToken? tokensRefresh = await _appClient.RefreshToken(tokens.token_access ?? "", "");

        // Assert.
        Assert.Equal(HttpStatusCode.Unauthorized, _appClient.StatusCode);
        Assert.Null(tokensRefresh);
    }

    [Fact]
    public async Task Test11_RefreshWithInvalidAccessToken()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        AccessAndRefreshToken? tokensRefresh = await _appClient.RefreshToken("ThisIsAnInvalidToken", tokens.token_refresh ?? "");

        // Assert.
        Assert.Equal(HttpStatusCode.Unauthorized, _appClient.StatusCode);
        Assert.Null(tokensRefresh);
    }

    [Fact]
    public async Task Test12_RefreshWithInvalidRefreshToken()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        AccessAndRefreshToken? tokensRefresh = await _appClient.RefreshToken(tokens.token_access ?? "", "ThisIsAnInvalidToken");

        // Assert.
        Assert.Equal(HttpStatusCode.Unauthorized, _appClient.StatusCode);
        Assert.Null(tokensRefresh);
    }

    [Fact]
    public async Task Test13_RefreshWithExpiredAccessTokenAndValidRefreshToken()
    {
        // Ensure there are no connection active.
        bool ret = await EnsureLogout();
        Assert.True(ret);

        // Assure the access token expires in 3 seconds (60 * 0.05 = 3).
        AccessAndRefreshToken? tokens = await EnsureLogged(0.05m);
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
    public async Task Test14_RefreshWithExpiredAccessTokenAndInvalidRefreshToken()
    {
        // Ensure there are no connection active.
        bool ret = await EnsureLogout();
        Assert.True(ret);

        // Assure the access token expires in 3 seconds (60 * 0.05 = 3).
        AccessAndRefreshToken? tokens = await EnsureLogged(0.05m);
        Assert.NotNull(tokens);

        // Await 5 seconds (assuming the access token expires in 3 seconds).
        await Task.Delay(5 * 1000);

        AccessAndRefreshToken? tokensRefresh = await _appClient.RefreshToken(tokens.token_access ?? "", "ThisIsAnInvalidToken");

        // Assert.
        Assert.Equal(HttpStatusCode.Unauthorized, _appClient.StatusCode);
        Assert.Null(tokensRefresh);
    }

    [Fact]
    public async Task Test15_RefreshWithExpiredRefreshToken()
    {
        // Ensure there are no connection active.
        bool ret = await EnsureLogout();
        Assert.True(ret);
        
        // Assure the access token expires in 3 seconds (60 * 0.05 = 3).
        AccessAndRefreshToken? tokens = await EnsureLogged(0.05m, 0.07m);
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

    [Fact]
    public async Task Test16_DefaultExpirationTimesDefinedInAppsettings()
    {
        // Ensure there are no connection active.
        bool ret = await EnsureLogout();
        Assert.True(ret);

        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        Assert.Equal(_app?.Jwt?.AccessTokenExpirationMinutes, tokens.token_access_expiration_minutes);
        Assert.Equal(_app?.Jwt?.RefreshExpirationMinutes, tokens.token_refresh_expiration_minutes);
    }

    [Fact]
    public async Task Test17_DefaultExpirationTimesAreTheMaximumValuesToGet()
    {
        // Ensure there are no connection active.
        bool ret = await EnsureLogout();
        Assert.True(ret);

        AccessAndRefreshToken? tokens = await EnsureLogged(Decimal.MaxValue, Decimal.MaxValue);
        Assert.NotNull(tokens);
        
        Assert.Equal(_app?.Jwt?.AccessTokenExpirationMinutes, tokens.token_access_expiration_minutes);
        Assert.Equal(_app?.Jwt?.RefreshExpirationMinutes, tokens.token_refresh_expiration_minutes);
    }

    [Fact]
    public async Task Test18_CantGetZeroExpirationTime()
    {
        // Ensure there are no connection active.
        bool ret = await EnsureLogout();
        Assert.True(ret);

        // Assure the access token expires in 3 seconds (60 * 0.05 = 3).
        AccessAndRefreshToken? tokens = await EnsureLogged(0m, 0m);
        Assert.NotNull(tokens);

        Assert.Equal(_app?.Jwt?.AccessTokenExpirationMinutes, tokens.token_access_expiration_minutes);
        Assert.Equal(_app?.Jwt?.RefreshExpirationMinutes, tokens.token_refresh_expiration_minutes);
    }

    [Fact]
    public async Task Test19_ExportReturnsAnSqlite()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        byte[]? file = await _appClient.Export();

        Assert.NotNull(file);
        Assert.True(file.IsSqlite());
    }

    [Fact]
    public async Task Test20_ExportReturnsAppDbStructure()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        byte[]? file = await _appClient.Export();
        Assert.NotNull(file);

        string temp = Path.GetTempFileName();
        await File.WriteAllBytesAsync(temp, file);

        int? categoryCount = null;
        int? expenseCount = null;

        using (SqliteContext sqlite = new (temp)) {
            categoryCount = sqlite.Category?.Count();
            expenseCount = sqlite.Expense?.Count();
        }

        File.Delete(temp);

        Assert.NotNull(categoryCount);
        Assert.NotNull(expenseCount);
    }

    [Fact]
    public async Task Test21_ExportReturnsDataInserted()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        // Add data to the database.
        Category cat1 = new () { cat_desc = "exportOk1" };
        int? id1 = await _catCRUDClient.Insert(cat1);
        Assert.NotNull(id1);

        Category cat2 = new () { cat_desc = "exportOk2", cat_order = 2 };
        int? id2 = await _catCRUDClient.Insert(cat2);
        Assert.NotNull(id2);

        // Do the export.
        byte[]? file = await _appClient.Export();
        Assert.NotNull(file);

        string temp = Path.GetTempFileName();
        await File.WriteAllBytesAsync(temp, file);

        // Test.
        bool found1 = false;
        bool found2 = false;

        using (SqliteContext sqlite = new (temp)) {
            Category? catExport1 = sqlite.Category?.First(u => u.cat_id == id1);
            found1 = (catExport1?.cat_desc == cat1.cat_desc);

            Category? catExport2 = sqlite.Category?.First(u => u.cat_id == id2);
            found2 = (catExport2?.cat_desc == cat2.cat_desc);
        }

        File.Delete(temp);

        Assert.True(found1);
        Assert.True(found2);
    }
}