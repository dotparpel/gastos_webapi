using Microsoft.EntityFrameworkCore;
using webapi.Application;
using webapi.Models;
using webapi.Repository;

namespace tests.Repository;

public class ExpenseRespositoryTest : GenericRepositoryTest<Expense, int?> {
    public ExpenseRespositoryTest() : base(GetRepository()) {}

    public static ExpenseRepository GetRepository() {
        AppSettings appSettings = new();
        DbContextOptions contextOptions = new DbContextOptionsBuilder<ApiContext>()
            .UseInMemoryDatabase("Expense")
            .Options;

        ApiContext ctx = new ApiContext(contextOptions, appSettings);

        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();

        ExpenseRepository rep = new ExpenseRepository(ctx);

        return rep;
    }

    internal override Expense New(string? method, params object[] param) {
        return new Expense() {
            expense_date = DateTime.Now
            , expense_desc = method
        };
    }

    internal override void Update(Expense elem)
    {
        elem.expense_amount = 1.0m;
    }

    internal override bool Equal(Expense elem1, Expense elem2) {
        return elem1.expense_date == elem2.expense_date 
            && elem1.expense_desc == elem2.expense_desc
            && elem1.expense_amount == elem2.expense_amount
            && elem1.cat_id == elem2.cat_id
        ;
    }

    internal override int? Id() => 1; 

    [Fact]
    public async Task Test08_InsertDateShouldNotBeEmpty()
    {
        // There are no errors.
        Assert.Null(_rep.LastError);

        // Insert a new category.
        Expense elemBefore = new();

        Expense? elem = await _rep.Upsert(elemBefore);

        Assert.Null(elem);
        Assert.Equal("The expense date can't be null.", _rep.LastError);
    }
}
