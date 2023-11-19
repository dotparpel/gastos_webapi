using Microsoft.EntityFrameworkCore;
using webapi.Models;

namespace tests.Api;

public class ExpenseExpandedTest : GenericSearchTest<ApiFactory01<Program>, ExpenseExpanded>
{
    public ExpenseExpandedTest(ApiFactory01<Program> factory) : base(factory) { 
        userLogin = "testExpenseExpanded";
        userPwd = "pwdExpenseExpanded";
    }

    // Method for InMemory database.
    protected override void FillData() {
        // Prepare the test dataset.
        DbContext? ctx = (DbContext?) _factory.GetService(typeof(DbContext));

        if (ctx != null) {
            IEnumerable<ExpenseExpanded>? list = new List<ExpenseExpanded>() {
                new ExpenseExpanded() { expense_date = DateTime.Parse("2022-12-31") }
                , new ExpenseExpanded() { expense_date = DateTime.Parse("2023-01-01"), expense_amount = 1.0m }
                , new ExpenseExpanded() { expense_date = DateTime.Parse("2023-02-02"), expense_amount = 2.0m, expense_desc = "readOk2" }
                , new ExpenseExpanded() { 
                    expense_date = DateTime.Parse("2023-03-03"), expense_amount = 3.0m, expense_desc = "readOk3"
                    , cat_id = 1, cat_desc = "Category1"
                }
            };

            DbSet<ExpenseExpanded> dbset = ctx.Set<ExpenseExpanded>();

            foreach(ExpenseExpanded ent in list)
                dbset.Add(ent);
            
            ctx.SaveChanges();
        }
    }

    protected override IEnumerable<string>? UseCaseQuery() => 
        new List<string>() {
            "select=expense_id,expense_desc"
            , "filter=expense_id%20eq%202"
            , "filter=contains(expense_desc, 'e')"
            , "orderby=expense_date desc&$top=2"
        }
    ;

    protected override bool entityEq(ExpenseExpanded ent1, ExpenseExpanded ent2) 
        => ent1.expense_date == ent2.expense_date 
            && ent1.expense_amount == ent2.expense_amount
            && ent1.expense_desc == ent2.expense_desc
            && ent1.cat_id == ent2.cat_id
            && ent1.cat_desc == ent2.cat_desc;
}