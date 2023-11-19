using System.Net;
using tests.Client;
using webapi.Models;
using webapi.Repository;
using webapi.JWT;

namespace tests.Api;

// AppSettings publishing all the functionality of the Api. 
[TestCaseOrderer(
    ordererTypeName: "tests.Xunit.AlphabeticalOrderer",
    ordererAssemblyName: "tests")]
public class AppTest02 : IClassFixture<ApiFactory02<Program>> {
    private readonly ApiFactory02<Program> _factory;
    private readonly HttpClient _client;
    private readonly ODataEntityResponse<AccessAndRefreshToken> _appResp;
    private readonly AppClient _appClient;
    private readonly ODataEntityKeyResponse<User, int?> _userResp;
    private readonly GenericCRUDClient<User, int?> _userCRUDClient;

    public AppTest02(ApiFactory02<Program> factory) {
        _factory = factory;
        _client = _factory.CreateClient();
        _appResp = new ODataEntityResponse<AccessAndRefreshToken>();
        _appClient = new AppClient(_client, _appResp);
        _userResp = new ODataEntityKeyResponse<User, int?>();
        _userCRUDClient = new GenericCRUDClient<User, int?>(_client, _userResp);
    }

    private async Task<AccessAndRefreshToken?> EnsureLogged() {
        AccessAndRefreshToken? tokens = null;

        if (_appClient.Token == null)
            tokens = await _appClient.Login("test02", "pwd02");

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

    // Protected methods (GET).
    [Theory]
    [InlineData("/v1/expensereport")]
    [InlineData("/v1/expenseexpanded")]
    [InlineData("/v1/expense")]
    [InlineData("/v1/category")]
    [InlineData("/export")]
    [InlineData("/v1/user")]
    public async Task Test02_CanNotAccessAuthorizedMethodsGET(string url)
    {
        // Act.
        var response = await _client.GetAsync(url);

        // Assert.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Protected methods (POST).
    [Fact]
    public async Task Test03_ImportEmptyDB()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        string temp = Path.GetTempFileName();

        byte[]? file;
        using (SqliteContext sqlite = new(temp)) {
            await sqlite.Database.EnsureCreatedAsync();
        };

        file = await File.ReadAllBytesAsync(temp);
        Assert.NotNull(file);
        Assert.True(file.Length > 0);

        bool ret = await _appClient.Import(file);
        Assert.True(ret);
    }

    [Fact]
    public async Task Test04_CanNotLoginWithExpiredDate()
    {
        // Add data to the database.
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        User user1 = new () { 
            user_login = "expired01User", user_pwd = "expired01Pwd"
            , user_login_expire_date = DateTimeOffset.UtcNow 
        };
        int? id1 = await _userCRUDClient.Insert(user1);
        Assert.NotNull(id1);

        // Act
        tokens = await _appClient.Login("expired01User", "expired01Pwd");

        // Assert.
        Assert.Equal(HttpStatusCode.Unauthorized, _appClient.StatusCode);
        Assert.Null(tokens);
    }

    [Fact]
    public async Task Test05_CanLoginWithExpiredDateNotReached()
    {
        // Add data to the database.
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        User user1 = new () { 
            user_login = "noExpired01User", user_pwd = "noExpired01Pwd"
            , user_login_expire_date = DateTimeOffset.UtcNow.AddMinutes(30)
        };
        int? id1 = await _userCRUDClient.Insert(user1);
        Assert.NotNull(id1);

        // Act
        tokens = await _appClient.Login("noExpired01User", "noExpired01Pwd");

        // Assert.
        Assert.Equal(HttpStatusCode.OK, _appClient.StatusCode);
        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens.token_access ?? "");
        Assert.NotEmpty(tokens.token_refresh ?? "");
    }

    [Fact]
    public async Task Test06_LogoutOk()
    {
        // Add data to the database.
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        User user1 = new () { 
            user_login = "noExpired02User", user_pwd = "noExpired02Pwd"
            , user_login_expire_date = DateTimeOffset.UtcNow.AddMinutes(30)
        };
        int? id1 = await _userCRUDClient.Insert(user1);
        Assert.NotNull(id1);

        tokens = await _appClient.Login("noExpired02User", "noExpired02Pwd");

        // Assert.
        Assert.Equal(HttpStatusCode.OK, _appClient.StatusCode);
        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens.token_access ?? "");
        Assert.NotEmpty(tokens.token_refresh ?? "");

        // Read the user.
        User? userReaded = await _userCRUDClient.Get(id1);

        Assert.NotNull(userReaded);
        Assert.NotNull(userReaded.user_access_key);
        Assert.NotNull(userReaded.user_refresh_key);
        Assert.NotNull(userReaded.user_refresh_expire_date);

        // Act
        bool ret = await _appClient.Logout();

        Assert.Equal(HttpStatusCode.OK, _appClient.StatusCode);
        Assert.True(ret);

        // Login as "admin".
        tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        // Read the user.
        userReaded = await _userCRUDClient.Get(id1);

        Assert.NotNull(userReaded);
        Assert.Null(userReaded.user_access_key);
        Assert.Null(userReaded.user_refresh_key);
        Assert.Null(userReaded.user_refresh_expire_date);
    }

    [Fact]
    public async Task Test07_LogoutWithoutLogin()
    {
        // Add data to the database.
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        User user1 = new () { 
            user_login = "noExpired03User", user_pwd = "noExpired03Pwd"
            , user_login_expire_date = DateTimeOffset.UtcNow.AddMinutes(30)
        };
        int? id1 = await _userCRUDClient.Insert(user1);
        Assert.NotNull(id1);

        tokens = await _appClient.Login("noExpired03User", "noExpired03Pwd");

        // Assert.
        Assert.Equal(HttpStatusCode.OK, _appClient.StatusCode);
        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens.token_access ?? "");
        Assert.NotEmpty(tokens.token_refresh ?? "");

        // Act
        bool ret = await _appClient.Logout();

        Assert.Equal(HttpStatusCode.OK, _appClient.StatusCode);
        Assert.True(ret);

        ret = await _appClient.Logout();
        Assert.Equal(HttpStatusCode.Unauthorized, _appClient.StatusCode);
        Assert.False(ret);
    }
}