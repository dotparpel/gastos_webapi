using System.Net;
using webapi.Models;
using webapi.Repository;
using webapi.JWT;

namespace tests.Api;

public class CategoryTest : GenericCRUDTest<ApiFactory01<Program>, Category, int?>
{
    public CategoryTest(ApiFactory01<Program> factory) : base(factory) { 
        userLogin = "testCategory";
        userPwd = "pwdCategory";
    }

    protected override void FillData() {
        ICategoryRepository? rep = (ICategoryRepository?) _factory.GetRepository<Category, int?>(typeof(ICategoryRepository));

        // Prepare the test dataset.
        if (rep != null) {
            IEnumerable<Category>? list = new List<Category>() {
                new Category() { cat_desc = "readOk1" }
                , new Category() { cat_desc = "readOk2", cat_order = 1 }
            };

            foreach(Category ent in list)
                rep.Upsert(ent);
        }
    }

    protected override IEnumerable<string>? UseCaseQuery() => 
        new List<string>() {
            "select=cat_id,cat_desc"
            , "filter=cat_id%20eq%202"
            , "filter=contains(cat_desc, 'e')"
            , "orderby=cat_order&$top=2"
            , "expand=Expense"
        }
    ;

    protected override IEnumerable<Category>? UseCaseInsertOkSet1() => 
        new List<Category>() {
            new Category() { cat_desc = "insertOk1" }
            , new Category() { cat_desc = "insertOk2", cat_order = 1 }
        };

    protected override IEnumerable<Category>? UseCaseInsertOkSet2() =>
        new List<Category>() {
            new Category() { cat_desc = "insertOk3" } 
            , new Category() { cat_desc = "insertOk4", cat_order = 1 } 
        };

    protected override IEnumerable<Category>? UseCaseGetOkSet() =>
        new List<Category>() {
            new Category() { cat_desc = "getOk1" } 
            , new Category() { cat_desc = "getOk2", cat_order = 2 }
        };

    protected override IEnumerable<int?>? UseCaseInvalidKeys() => 
        new List<int?>() {
            0, -1, 1000000
        };

    protected override Category entityNew(params object[] param) {
        Category cat = new Category();
        
        if (param.Length > 0 && param[0] != null)
            cat.cat_desc = (string) param[0];

        if (param.Length > 1 && param[1] != null)
            cat.cat_order = (int) param[1];
        
        return cat;
    }

    protected override void entityUpdate(Category cat,  params object?[] param) {
        if (param.Length > 0 && param[0] != null)
            cat.cat_desc = (string) param[0]!;

        if (param.Length > 1 && param[1] != null)
            cat.cat_order = (int) param[1]!;
    }

    protected override bool entityEq(Category ent1, Category ent2) 
        => ent1.cat_desc == ent2.cat_desc 
            && ent1.cat_order == ent2.cat_order;

    [Fact]
    public async Task Test200_CategoryInsertCantBeEmpty() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        int? countBefore = await _entCRUDClient.Count() ?? 0;

        Category? cat = new Category();
        Category? catResp = await _entCRUDClient.InsertReturnsEntity(cat);

        Assert.Equal(HttpStatusCode.BadRequest, _entCRUDClient.StatusCode);
        Assert.Null(catResp);

        int? countAfter = await _entCRUDClient.Count() ?? 0;
        Assert.Equal(countAfter, countBefore);
    }

    [Fact]
    public async Task Test201_CategoryInsertCantBeRepeated() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        Category? cat = new Category() { cat_desc = "testCantBeRepeated"};
        Category? catResp = await _entCRUDClient.InsertReturnsEntity(cat);

        Assert.Equal(HttpStatusCode.OK, _entCRUDClient.StatusCode);
        Assert.NotNull(catResp);

        int? countBefore = await _entCRUDClient.Count() ?? 0;
        catResp = await _entCRUDClient.InsertReturnsEntity(cat);

        Assert.Equal(HttpStatusCode.BadRequest, _entCRUDClient.StatusCode);
        Assert.Null(catResp);

        int? countAfter = await _entCRUDClient.Count() ?? 0;
        Assert.Equal(countAfter, countBefore);
    }
}