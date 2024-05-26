using System.Net;
using webapi.Models;
using webapi.Repository;
using webapi.JWT;

namespace tests.Api;

public class UserTest : GenericCRUDTest<ApiFactory02<Program>, User, int?>
{
    public UserTest(ApiFactory02<Program> factory) : base(factory) { 
        userLogin = "testUser";
        userPwd = "pwdUser";
    }

    protected override void FillData() {
        IUserRepository? rep = (IUserRepository?) _factory.GetRepository<User, int?>(typeof(IUserRepository));

        // Prepare the test dataset.
        if (rep != null) {
            IEnumerable<User>? list = new List<User>() {
                new User() { user_login = "readOk1", user_pwd = "pwdReadOk1" }
                , new User() { user_login = "readOk2", user_pwd = "pwdReadOk2" }
            };

            foreach(User ent in list)
                rep.Upsert(ent);
        }
    }
    
    protected override IEnumerable<string>? UseCaseQuery() => 
        new List<string>() {
            "select=user_login"
            , "filter=user_id%20eq%202"
            , "filter=contains(user_login, 'e')"
            , "orderby=user_login desc&$top=2"
        }
    ;

    protected override IEnumerable<User>? UseCaseInsertOkSet1() => 
        new List<User>() {
            new User() { user_login = "insertOk1", user_pwd = "pwdInsertOk1" }
            , new User() { user_login = "insertOk2", user_pwd = "pwdInsertOk2" }
        };

    protected override IEnumerable<User>? UseCaseInsertOkSet2() =>
        new List<User>() {
            new User() { user_login = "insertOk3", user_pwd = "pwdInsertOk3" }
            , new User() { user_login = "insertOk4", user_pwd = "pwdInsertOk4" }
        };

    protected override IEnumerable<User>? UseCaseGetOkSet() =>
        new List<User>() {
            new User() { user_login = "getOk1", user_pwd = "pwdGetOk3" }
            , new User() { user_login = "getOk2", user_pwd = "pwdGetOk4" }
        };

    protected override IEnumerable<int?>? UseCaseInvalidKeys() => 
        new List<int?>() {
            0, -1, 1000000
        };

    protected override User entityNew(params object[] param) {
        User ent = new User();
        
        if (param.Length > 0 && param[0] != null) {
            ent.user_login = (string) param[0];
            ent.user_pwd = "pwd" + (string) param[0];
        }
        
        return ent;
    }

    protected override void entityUpdate(User ent, params object?[] param) {
        if (param.Length > 0 && param[0] != null)
            ent.user_login = (string) param[0]!;

        if (param.Length > 1 && param[1] != null)
            ent.user_pwd = param[1]!.ToString();
    }

    protected override bool entityEq(User ent1, User ent2) 
        => ent1.user_login == ent2.user_login 
            && ent1.user_pwd == ent2.user_pwd;

    protected override async Task Test001_ReadEmptyOk() {
        await Task.Delay(1);
    }

    [Theory]
    [MemberData(nameof(UserKO))]
    public async Task Test301_CantCreateUserWithoutLoginAndPassword(User ent) {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        int? countBefore = await _entCRUDClient.Count() ?? 0;
        int? id = await _entCRUDClient.Insert(ent);

        Assert.Equal(HttpStatusCode.BadRequest, _entCRUDClient.StatusCode);
        Assert.Null(id);

        int? countAfter = await _entCRUDClient.Count() ?? 0;
        Assert.Equal(countAfter, countBefore);
    }

    public static IEnumerable<object[]> UserKO =>
        new List<object[]>()
        {
            new object[] { new User() { user_login = "loginKO01" } }
            , new object[] { new User() { user_pwd = "pwdKO01" } }
        };

    [Fact]
    public async Task Test302_CantInsertARepetedUser() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        User? ent1 = new User() { user_login = "loginRepeated01", user_pwd = "userRepeated01"};
        User? entResp = await _entCRUDClient.InsertReturnsEntity(ent1);

        Assert.Equal(HttpStatusCode.OK, _entCRUDClient.StatusCode);
        Assert.NotNull(entResp);

        int? countBefore = await _entCRUDClient.Count() ?? 0;

        User? ent2 = new User() { user_login = "loginRepeated01", user_pwd = "userRepeated02"};
        entResp = await _entCRUDClient.InsertReturnsEntity(ent2);

        Assert.Equal(HttpStatusCode.BadRequest, _entCRUDClient.StatusCode);
        Assert.Null(entResp);

        int? countAfter = await _entCRUDClient.Count() ?? 0;
        Assert.Equal(countAfter, countBefore);
    }
}