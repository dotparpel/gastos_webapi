using System.Net;

using tests.Client;
using webapi.Models;
using webapi.JWT;

namespace tests.Api;

#if TEST_POSTGRES
public class ExpenseTestPostgresql : IClassFixture<ApiFactoryPostgresql<Program>> {
    protected readonly ApiFactoryPostgresql<Program> _factory;
    protected readonly HttpClient _client;
    private readonly ODataEntityResponse<AccessAndRefreshToken> _appResp;
    protected readonly AppClient _appClient;
    private readonly ODataEntityKeyResponse<Expense, int?> _entResp;
    protected readonly GenericCRUDClient<Expense, int?> _entCRUDClient;

    protected readonly ODataEntityResponse<ExpenseReport> _entSearchResp;
    protected readonly GenericSearchClient<ExpenseReport> _entSearchClient;

    protected string userLogin = "admin";
    protected string userPwd = "pwd";

    public ExpenseTestPostgresql(ApiFactoryPostgresql<Program> factory) { 
        _factory = factory;
        _client = _factory.CreateClient();
        _appResp = new ODataEntityResponse<AccessAndRefreshToken>();
        _appClient = new AppClient(_client, _appResp);
        _entResp = new ODataEntityKeyResponse<Expense, int?>();
        _entCRUDClient = new GenericCRUDClient<Expense, int?>(_client, _entResp);
        _entSearchResp = new ();
        _entSearchClient = new (_client, _entSearchResp);
    }

    protected async Task<AccessAndRefreshToken?> EnsureLogged() {
        AccessAndRefreshToken? tokens = null;

        if (_appClient.Token == null)
            tokens = await _appClient.Login(userLogin, userPwd);

        return tokens;
    }

    [Fact]
    public async Task Test001_DateWithTimeZone() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        // DateTime.Now.
        Expense? ent = new Expense() {
            expense_desc = "Test001_DateWithTimeZone"
            , expense_amount = 1m
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

    [Fact]
    public async Task Test002_ExpenseReport() {
        List<string> listtz = new() {
          "Etc/GMT+12"                   // -12:00
          , "Pacific/Midway"             // -11:00
          , "Etc/GMT+10"                 // -10:00
          , "Pacific/Gambier"            // -09:00
          , "Pacific/Marquesas"          // -09:30
          , "Pacific/Pitcairn"           // -08:00
          , "America/Hermosillo"         // -07:00
          , "Canada/Saskatchewan"        // -06:00
          , "America/Bogota"             // -05:00
          , "America/Manaus"             // -04:00
          , "America/Mendoza"            // -03:00
          , "Atlantic/South_Georgia"     // -02:00
          , "Atlantic/Cape_Verde"        // -01:00
          , "Greenwich"                  // -00:00
          , "Africa/Tunis"               // +01:00
          , "Europe/Kaliningrad"         // +02:00
          , "Africa/Nairobi"             // +03:00
          , "Asia/Tehran"                // +03:30
          , "Indian/Reunion"             // +04:00
          , "Asia/Kabul"                 // +04:30
          , "Asia/Samarkand"             // +05:00
          , "Asia/Calcutta"              // +05:30
          , "Asia/Thimbu"                // +06:00
          , "Indian/Christmas"           // +07:00
          , "Asia/Hong_Kong"             // +08:00
          , "Asia/Tokyo"                 // +09:00
          , "Antarctica/DumontDUrville"  // +10:00
          , "Pacific/Guadalcanal"        // +11:00
          , "Asia/Kamchatka"             // +12:00
        };

        // Get the zulo date.
        DateTime utcnow = DateTime.UtcNow;

        // Insert the test expense.
        Expense? ent = new Expense() {
            expense_desc = "Test002_ExpenseReport"
            , expense_amount = 2m
        };
        ent.expense_date = utcnow;

        Expense? entResp = await _entCRUDClient.InsertReturnsEntity(ent);

        Assert.NotNull(entResp);
        Assert.Equal(ent.expense_date, entResp.expense_date);

        // Test all the timezones.
        for (int tz = -12; tz <= 12; tz++) {

            // Test all hours of the day.
            for (int hour = 0; hour < 24; hour ++) {
                DateTime dt = new (utcnow.Year, utcnow.Month, utcnow.Day, hour, 0, 0);
                TimeSpan timespan = new TimeSpan(tz, 0, 0);
                DateTimeOffset dto = new (dt, timespan);

                // Update the expense.
                ent.expense_date = dto;
                entResp = await _entCRUDClient.UpdateEntity(entResp.Id, ent);

                Assert.NotNull(entResp);
                Assert.Equal(ent.expense_date, entResp.expense_date);

                // Test all timezones.
                foreach(string tzid in listtz) {
                    // Read the register as a report.
                    TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById(tzid);
                    string tzToSend = tzid.Replace("+", "%2b");

                    string query = $"filter=expense_id eq {entResp.Id}&timezone={tzToSend}";
                    List<ExpenseReport>? list = await _entSearchClient.Read(query);

                    Assert.Equal(HttpStatusCode.OK, _entSearchClient.StatusCode);
                    Assert.NotNull(list);
                    Assert.NotEmpty(list);

                    // Get the register.
                    ExpenseReport er = list.First();

                    Assert.NotNull(er.expense_date);

                    // Compare dates.
                    string sign = tz > 0 ? "-" : "+";
                    DateTime dateobj = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                      (entResp.expense_date ?? DateTime.Now).DateTime, $"Etc/GMT{sign}{Math.Abs(tz)}", tzid);
                    DateTime daterpt = er.expense_date_tz ?? DateTime.Now;

                    Assert.Equal(dateobj, daterpt);
                }
            }
        }

        // Delete the test expense.
        bool ret = await _entCRUDClient.Delete(entResp.Id);
        Assert.True(ret);
    }
}
#endif