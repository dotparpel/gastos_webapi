using System.Net;

using tests.Client;
using webapi.Models;
using webapi.JWT;

namespace tests.Api;

#if TEST_POSTGRES
public class ExpenseTestPostgresql : IClassFixture<ApiFactoryPostgresql<Program>> 
{
    protected readonly ApiFactoryPostgresql<Program> _factory;
    protected readonly HttpClient _client;
    private readonly ODataEntityResponse<AccessAndRefreshToken> _appResp;
    protected readonly AppClient _appClient;
    private readonly ODataEntityKeyResponse<Expense, int?> _entResp;
    protected readonly GenericCRUDClient<Expense, int?> _entCRUDClient;

    protected string userLogin = "admin";
    protected string userPwd = "pwd";

    public ExpenseTestPostgresql(ApiFactoryPostgresql<Program> factory) { 
        _factory = factory;
        _client = _factory.CreateClient();
        _appResp = new ODataEntityResponse<AccessAndRefreshToken>();
        _appClient = new AppClient(_client, _appResp);
        _entResp = new ODataEntityKeyResponse<Expense, int?>();
        _entCRUDClient = new GenericCRUDClient<Expense, int?>(_client, _entResp);
    }

    protected async Task<AccessAndRefreshToken?> EnsureLogged() {
        AccessAndRefreshToken? tokens = null;

        if (_appClient.Token == null)
            tokens = await _appClient.Login(userLogin, userPwd);

        return tokens;
    }

    [Fact]
    public async Task Test201_DateWithTimeZone() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        int? countBefore = await _entCRUDClient.Count() ?? 0;

        // DateTime.Now.
        Expense? ent = new Expense() {
            expense_desc = "Test201_DateWithTimeZone"
            , expense_amount = 201m
        };
        ent.expense_date = DateTime.Now;

        Expense? entResp = await _entCRUDClient.InsertReturnsEntity(ent);

        Assert.NotNull(entResp);
        Assert.Equal(ent.expense_date, entResp.expense_date);

        // DateTime.UtcNow.
        ent.expense_date = DateTime.UtcNow;

        entResp = await _entCRUDClient.UpdateEntity(entResp.Id, ent);
        Assert.NotNull(entResp);
        Assert.Equal(ent.expense_date, entResp.expense_date);

        // All timespans.
        DateTime now = new DateTime(DateTime.Now.Ticks);

        for (int i = -11; i <= 11; i++) {
            ent.expense_date = new DateTimeOffset(now, new TimeSpan(i, 0, 0));

            entResp = await _entCRUDClient.UpdateEntity(entResp.Id, ent);
            Assert.NotNull(entResp);
            Assert.Equal(ent.expense_date, entResp.expense_date);
        }

        // All timezones.
        var timezones = TimeZoneInfo.GetSystemTimeZones();
        DateTime utcNow = DateTime.UtcNow;

        foreach(TimeZoneInfo timezone in timezones) {
            ent.expense_date = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timezone);
            entResp = await _entCRUDClient.UpdateEntity(entResp.Id, ent);

            Assert.NotNull(entResp);
            Assert.Equal(ent.expense_date, entResp.expense_date);
        }

        // Delete the item.
        bool ret = await _entCRUDClient.Delete(entResp.Id);
        Assert.True(ret);
    }
}
#endif