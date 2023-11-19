using Microsoft.EntityFrameworkCore;
using webapi.Models;

namespace tests.Api;

public class ExpenseReportTest : GenericSearchTest<ApiFactory01<Program>, ExpenseReport>
{
    public ExpenseReportTest(ApiFactory01<Program> factory)  : base(factory) { 
        userLogin = "testExpenseReport";
        userPwd = "pwdExpenseReport";
    }

    // Method for InMemory database.
    protected override void FillData() {
        // Prepare the test dataset.
        DbContext? ctx = (DbContext?) _factory.GetService(typeof(DbContext));

        if (ctx != null) {
            IEnumerable<ExpenseReport>? list = new List<ExpenseReport>() {
                new ExpenseReport() { 
                    expense_date = DateTime.Parse("2022-12-31")
                    , year = 2022, month = 12, week = 52, day = 31
                }
                , new ExpenseReport() { 
                    expense_date = DateTime.Parse("2023-01-01"), expense_amount = 1.0m
                    , year = 2023, month = 1, week = 1, day = 1
                }
                , new ExpenseReport() {
                    expense_date = DateTime.Parse("2023-02-02"), expense_amount = 2.0m, expense_desc = "readOk2"
                    , year = 2023, month = 2, week = 5, day = 2
                }
                , new ExpenseReport() { 
                    expense_date = DateTime.Parse("2023-03-03"), expense_amount = 3.0m, expense_desc = "readOk3"
                    , year = 2023, month = 3, week = 9, day = 3
                    , cat_id = 1, cat_desc = "Category1"
                }
            };

            DbSet<ExpenseReport> dbset = ctx.Set<ExpenseReport>();

            foreach(ExpenseReport ent in list)
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
            , "apply=groupby((cat_id, cat_desc), aggregate(expense_amount with sum as import))"
            , "apply=aggregate(expense_amount with max as max)"
            , "apply=filter(year eq 2023)/aggregate(expense_id with countdistinct as num)"
        }
    ;

    protected override bool entityEq(ExpenseReport ent1, ExpenseReport ent2) 
        => ent1.expense_date == ent2.expense_date 
            && ent1.expense_amount == ent2.expense_amount
            && ent1.expense_desc == ent2.expense_desc;
}