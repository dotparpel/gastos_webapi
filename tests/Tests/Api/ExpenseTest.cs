using System.Net;
using webapi.Models;
using webapi.Repository;
using webapi.JWT;

namespace tests.Api;

public class ExpenseTest : GenericCRUDTest<ApiFactory01<Program>, Expense, int?>
{
    public ExpenseTest(ApiFactory01<Program> factory) : base(factory) { 
        userLogin = "testExpense";
        userPwd = "pwdExpense";
    }

    protected override void FillData() {
        IExpenseRepository? rep = (IExpenseRepository?) _factory.GetRepository<Expense, int?>(typeof(IExpenseRepository));

        // Prepare the test dataset.
        if (rep != null) {
            IEnumerable<Expense>? list = new List<Expense>() {
                new Expense() { expense_date = DateTime.Parse("2022-12-31"), expense_amount = 1.0m }
                , new Expense() { expense_date = DateTime.Parse("2023-04-04"), expense_amount = 2.0m, expense_desc = "readOk2" }
            };

            foreach(Expense ent in list)
                rep.Upsert(ent);
        }
    }
    
    protected override IEnumerable<string>? UseCaseQuery() => 
        new List<string>() {
            "select=expense_id,expense_desc"
            , "filter=expense_id%20eq%202"
            , "filter=contains(expense_desc, 'e')"
            , "orderby=expense_date desc&$top=2"
            , "expand=Category"
        }
    ;

    protected override IEnumerable<Expense>? UseCaseInsertOkSet1() => 
        new List<Expense>() {
            new Expense() { expense_date = DateTime.Parse("2023-01-01"), expense_amount = 3.0m }
            , new Expense() { expense_date = DateTime.Parse("2023-01-31"), expense_amount = 4.0m, expense_desc = "insertOk2" }
        };

    protected override IEnumerable<Expense>? UseCaseInsertOkSet2() =>
        new List<Expense>() {
            new Expense() { expense_date = DateTime.Parse("2023-02-01"), expense_amount = 5.0m }
            , new Expense() { expense_date = DateTime.Parse("2023-02-28"), expense_amount = 6.0m, expense_desc = "insertOk4" }
        };

    protected override IEnumerable<Expense>? UseCaseGetOkSet() =>
        new List<Expense>() {
            new Expense() { expense_date = DateTime.Parse("2023-03-01"), expense_amount = 7.0m }
            , new Expense() { expense_date = DateTime.Parse("2023-03-31"), expense_amount = 8.0m, expense_desc = "getOk2" }
        };

    protected override IEnumerable<int?>? UseCaseInvalidKeys() => 
        new List<int?>() {
            0, -1, 1000000
        };

    protected override Expense entityNew(params object[] param) {
        Expense ent = new Expense();
        
        if (param.Length > 0 && param[0] != null) {
            ent.expense_desc = (string) param[0];
            ent.expense_date = DateTime.Now;
        }

        if (param.Length > 1 && param[1] != null)
            ent.expense_amount = (int) param[1];
        
        return ent;
    }

    protected override void entityUpdate(Expense ent, params object?[] param) {
        if (param.Length > 0 && param[0] != null)
            ent.expense_desc = (string) param[0]!;

        if (param.Length > 1 && param[1] != null)
            ent.expense_amount = (int) param[1]!;
    }

    protected override bool entityEq(Expense ent1, Expense ent2) 
        => ent1.expense_date == ent2.expense_date 
            && ent1.expense_amount == ent2.expense_amount
            && ent1.expense_desc == ent2.expense_desc;

    [Fact]
    public async Task Test200_ExpenseInsertCantBeEmpty() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        int? countBefore = await _entCRUDClient.Count() ?? 0;

        Expense? ent = new Expense();
        Expense? entResp = await _entCRUDClient.InsertReturnsEntity(ent);

        Assert.Equal(HttpStatusCode.BadRequest, _entCRUDClient.StatusCode);
        Assert.Null(entResp);

        int? countAfter = await _entCRUDClient.Count() ?? 0;
        Assert.Equal(countAfter, countBefore);
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