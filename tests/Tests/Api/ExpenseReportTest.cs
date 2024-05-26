using webapi.Models;
using webapi.Repository;

namespace tests.Api;

public class ExpenseReportTest : GenericSearchTest<ApiFactory01<Program>, ExpenseReport>
{
    public ExpenseReportTest(ApiFactory01<Program> factory)  : base(factory) { 
        userLogin = "testExpenseReport";
        userPwd = "pwdExpenseReport";

        _search = (IExpenseReportSearch?) _factory.GetService(typeof(IExpenseReportSearch));
    }

    // Method for InMemory database.
    protected override void FillData() {
        // Prepare the test dataset.
        ApiContext.ExpenseReport.Add(
            new ExpenseReport() { 
                expense_id = 1
                , expense_date = DateTime.Parse("2022-12-31")
                , year = 2022, month = 12, week = 52, day = 31
            }
        );
        ApiContext.ExpenseReport.Add(
            new ExpenseReport() { 
                expense_id = 2
                , expense_date = DateTime.Parse("2023-01-01"), expense_amount = 1.0m
                , year = 2023, month = 1, week = 1, day = 1
            }
        );
        ApiContext.ExpenseReport.Add(
            new ExpenseReport() {
                expense_id = 3
                , expense_date = DateTime.Parse("2023-02-02"), expense_amount = 2.0m, expense_desc = "readOk2"
                , year = 2023, month = 2, week = 5, day = 2
            }
        );
        ApiContext.ExpenseReport.Add(
            new ExpenseReport() { 
                expense_id = 4
                , expense_date = DateTime.Parse("2023-03-03"), expense_amount = 3.0m, expense_desc = "readOk3"
                , year = 2023, month = 3, week = 9, day = 3
                , cat_id = 1, cat_desc = "Category1"
            }
        );
    }

    protected override IEnumerable<string>? UseCaseQuery() => 
        new List<string>() {
            "select=expense_id,expense_desc"
            , "filter=expense_id eq 2"
            , "filter=contains(expense_desc, '2')"
            , "orderby=expense_date desc&$top=2"
            , "apply=groupby((cat_id, cat_desc), aggregate(expense_amount with sum as import))"
            , "apply=aggregate(expense_amount with max as max)"
            , "apply=filter(year eq 2023)/aggregate(expense_id with countdistinct as num)"
            // With timezone as a parameter.
            , "select=expense_id, expense_desc&timezone=Africa/Addis_Ababa"
            , "filter=expense_id eq 2&timezone=Asia/Bangkok"
            , "filter=contains(expense_desc, '2')&timezone=America/Adak"
            , "orderby=expense_date desc&$top=2&timezone=Australia/Darwin"
            , "apply=groupby((cat_id, cat_desc), aggregate(expense_amount with sum as import))&timezone=Brazil/Acre"
            , "apply=aggregate(expense_amount with max as max)&timezone=Europe/Madrid"
            , "apply=filter(year eq 2023)/aggregate(expense_id with countdistinct as num)&timezone=Pacific/Midway"
        }
    ;

    protected override bool entityEq(ExpenseReport ent1, ExpenseReport ent2) 
        => ent1.expense_date == ent2.expense_date 
            && ent1.expense_amount == ent2.expense_amount
            && ent1.expense_desc == ent2.expense_desc;
}