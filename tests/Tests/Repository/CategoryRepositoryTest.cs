using System.Reflection;
using Microsoft.EntityFrameworkCore;
using webapi.Application;
using webapi.Models;
using webapi.Repository;

namespace tests.Repository;

public class CategoryRepositoryTest : GenericRepositoryTest<Category, int?> {
    public CategoryRepositoryTest() : base(GetRepository()) {}

    public static CategoryRepository GetRepository() {
        AppSettings appSettings = new();
        DbContextOptions contextOptions = new DbContextOptionsBuilder<ApiContext>()
            .UseInMemoryDatabase("Category")
            .Options;

        ApiContext ctx = new ApiContext(contextOptions, appSettings);

        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();

        ExpenseRepository repexp = new ExpenseRepository(ctx);
        CategoryRepository rep = new CategoryRepository(ctx, repexp);

        return rep;
    }

    internal override Category New(string? method, params object[] param) {
        return new Category() {
            cat_desc = method
        };
    }

    internal override void Update(Category elem)
    {
        elem.cat_order = 1;
    }

    internal override bool Equal(Category elem1, Category elem2) {
        return elem1.cat_desc == elem2.cat_desc 
            && elem1.cat_order == elem2.cat_order;
    }

    internal override int? Id() => 1; 

    [Fact]
    public async Task Test08_InsertDescriptionShouldNotBeEmpty()
    {
        // There are no errors.
        Assert.Null(_rep.LastError);

        // Insert a new category.
        Category catBefore = new();

        Category? cat = await _rep.Upsert(catBefore);

        Assert.Null(cat);
        Assert.Equal("The category description can't be empty.", _rep.LastError);
    }

    [Fact]
    public async Task Test09_InsertCantRepeatADescription()
    {
        string? method = MethodBase.GetCurrentMethod()?.Name;

        // The table is empty.
        int count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(0, count);
        
        // There are no errors.
        Assert.Null(_rep.LastError);

        // Insert a new category.
        Category catBefore = New(method);

        // First insert is fine.
        Category? cat1 = await _rep.Upsert(catBefore);

        Assert.NotNull(cat1);
        Assert.Null(_rep.LastError);

        count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(1, count);

        // Second insert can't be done.
        Category? cat2 = await _rep.Upsert(catBefore);

        Assert.Null(cat2);
        Assert.NotNull(_rep.LastError);
        Assert.Contains(" already exists.", _rep.LastError);

        count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Test10_DeleteCantBeDoneWhenInUse()
    {
        string? method = MethodBase.GetCurrentMethod()?.Name;

        // The table is empty.
        int count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(0, count);

        // Insert a new category.
        Category catBeforeInsert = New(method);

        Category? cat = await _rep.Upsert(catBeforeInsert);
        Assert.NotNull(cat);

        count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(1, count);

        // Use the category in an expense.
        Expense? expBeforeInsert = new () {
            expense_date = DateTime.Now
            , cat_id = cat.cat_id
        };

        IExpenseRepository repexp = ((CategoryRepository) _rep).ExpenseRepository;
        Expense? expense = await repexp.Upsert(expBeforeInsert);
        Assert.NotNull(expense);

        // Try to delete the category.
        Category? catDeleted = await _rep.Delete(cat.cat_id);
        Assert.Null(catDeleted);
        Assert.NotNull(_rep.LastError);
        Assert.Contains(" is being used ", _rep.LastError);

        count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(1, count);
    }
}
