using System.Reflection;
using webapi.Models;
using webapi.Repository;

namespace tests.Repository;

public abstract class GenericRepositoryTest<TTable, TKey> where TTable : Entity<TKey>, new()
{
    internal readonly GenericRepository<TTable, TKey> _rep;

    public GenericRepositoryTest(GenericRepository<TTable, TKey> rep) {
        _rep = rep;
    }

    internal virtual TTable New(
        string? method, params object[] param) => new TTable();

    internal virtual void Update(TTable elem) { }

    internal virtual bool Equal(TTable elem1, TTable elem2) => true;

    internal virtual TKey? Id() => default;

    [Fact]
    public void Test01_EnsureInitialized()
    {
        Assert.NotNull(_rep);

        // The tables are empty.
        int count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(0, count);
    }

    [Fact]
    public void Test02_ReadEmptyTableReturnsEmptyQry()
    {
        // The table is empty.
        IQueryable<TTable>? qry = _rep.Read();
        Assert.NotNull(qry);
        Assert.Equal(0, qry?.Count());
    }

    [Fact]
    public async Task Test03_Insert()
    {
        string? method = MethodBase.GetCurrentMethod()?.Name;
        
        // The table is empty.
        int countBefore = _rep.Read()?.Count() ?? 0;
        Assert.Equal(0, countBefore);

        // Insert a new category.
        TTable elemBefore = New(method);

        TTable? inserted = await _rep.Upsert(elemBefore);

        // Read all the categories.
        List<TTable>? list = _rep.Read()?.ToList();
        Assert.NotNull(list);

        // The number of categories has been increased.
        int countAfter = list?.Count() ?? 0;
        Assert.Equal(countAfter, countBefore + 1);

        // The category returned "is equal" to the category returned.
        TTable? elemAfter = list?.FirstOrDefault();
        Assert.NotNull(elemAfter);
        Assert.True(Equal(elemBefore, elemAfter));

        // The category has an "id".
        Assert.NotNull(elemAfter.Id);
    }

    [Fact]
    public async Task Test04_Update() {
        string? method = MethodBase.GetCurrentMethod()?.Name;

        // The table is empty.
        int countInit = _rep.Read()?.Count() ?? 0;
        Assert.Equal(0, countInit);

        // Insert a new category.
        TTable cat = New(method);

        TTable? catAfterInsert = await _rep.Upsert(cat);
        Assert.NotNull(catAfterInsert);
        Assert.NotNull(catAfterInsert.Id);

        // Test data is added.
        int countAfterInsert = _rep.Read()?.Count() ?? 0;
        Assert.Equal(countAfterInsert, countInit + 1);

        // Modify the category.
        Update(catAfterInsert);
        TTable? catAfterUpdate = await _rep.Upsert(catAfterInsert);
        Assert.NotNull(catAfterUpdate);
        Assert.True(Equal(catAfterInsert, catAfterUpdate));

        // Test no data is added.
        int countAfterUpdate = _rep.Read()?.Count() ?? 0;
        Assert.Equal(countAfterInsert, countAfterUpdate);
    }

    [Fact]
    public async Task Test05_Delete()
    {
        string? method = MethodBase.GetCurrentMethod()?.Name;

        // The table is empty.
        int count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(0, count);

        // Insert a new category.
        TTable cat = New(method);

        TTable? catAfterInsert = await _rep.Upsert(cat);
        Assert.NotNull(catAfterInsert);
        Assert.NotNull(catAfterInsert.Id);

        count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(1, count);

        // Delete the category.
        TTable? catDeleted = await _rep.Delete(catAfterInsert.Id);
        Assert.NotNull(catDeleted);

        // The table is empty again.
        count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Test06_Get()
    {
        string? method = MethodBase.GetCurrentMethod()?.Name;

        // The table is empty.
        int count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(0, count);

        // Insert a new category.
        TTable cat = New(method);

        TTable? catAfterInsert = await _rep.Upsert(cat);
        Assert.NotNull(catAfterInsert);
        Assert.NotNull(catAfterInsert.Id);

        count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(1, count);

        // Get the category.
        TTable? catGetted = await _rep.Get(catAfterInsert.Id);
        Assert.NotNull(catGetted);
        Assert.True(Equal(catAfterInsert, catGetted));
    }

    [Fact]
    public async Task Test07_GetInexistentReturnsNull()
    {
        // The table is empty.
        int count = _rep.Read()?.Count() ?? 0;
        Assert.Equal(0, count);

        // Get the category.
        TTable? catGetted = await _rep.Get(Id()!);
        Assert.Null(catGetted);
    }
}
