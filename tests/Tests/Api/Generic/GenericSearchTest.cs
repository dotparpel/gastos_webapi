using System.Net;

using tests.Client;
using webapi.Repository;
using webapi.JWT;

namespace tests.Api;

[TestCaseOrderer(
    ordererTypeName: "tests.Xunit.AlphabeticalOrderer",
    ordererAssemblyName: "tests")]
public abstract class GenericSearchTest<TFixture, TEntity> : IClassFixture<TFixture>
    where TFixture : ApiFactory<Program>
    where TEntity : class
{
    protected readonly TFixture _factory;
    protected readonly HttpClient _client;
    protected readonly ODataEntityResponse<AccessAndRefreshToken> _appResp;
    protected readonly AppClient _appClient;
    protected readonly ODataEntityResponse<TEntity> _entResp;
    protected readonly GenericSearchClient<TEntity> _entSearchClient;
    protected IGenericSearch<TEntity>? _search;

    protected string userLogin = "admin";
    protected string userPwd = "pwd";

    public GenericSearchTest(TFixture factory) {
        _factory = factory;
        _client = _factory.CreateClient();
        _appResp = new ODataEntityResponse<AccessAndRefreshToken>();
        _appClient = new AppClient(_client, _appResp);
        _entResp = new ODataEntityResponse<TEntity>();
        _entSearchClient = new GenericSearchClient<TEntity>(_client, _entResp);
        _search = _factory.GetSearch<TEntity>();
    }

    protected async Task<AccessAndRefreshToken?> EnsureLogged() {
        AccessAndRefreshToken? tokens = null;

        if (_appClient.Token == null)
            tokens = await _appClient.Login(userLogin, userPwd);

        return tokens;
    }

    // Obtain the data to do the tests.
    protected abstract void FillData();

    protected virtual void EnsureCreatedData() {
        Assert.NotNull(_search);

        List<TEntity>? data = _search?.Read()?.ToList();

        if (data?.Count == 0)
            FillData();
    }

    [Fact]
    protected virtual async Task Test001_ReadEmptyOk()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        // Act.
        List<TEntity>? list = await _entSearchClient.Read();

        // Assert.
        Assert.Equal(HttpStatusCode.OK, _entSearchClient.StatusCode);
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    // Give a criteria to determine equality.
    protected abstract bool entityEq(TEntity ent1, TEntity ent2);

    [Fact]
    public async Task Test002_ReadOk()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        EnsureCreatedData();

        // Act.
        List<TEntity>? list = await _entSearchClient.Read();
        List<TEntity>? data = _search?.Read()?.ToList();

        // Check that both lists have the same data.
        Assert.Equal(HttpStatusCode.OK, _entSearchClient.StatusCode);
        Assert.NotNull(list);
        Assert.NotEmpty(list);
        Assert.NotNull(data);
        Assert.NotEmpty(data);
        Assert.Equal(list.Count, data.Count);

        // Test the data returned (assuming the same order in both lists).
        int pos = 0;
        foreach (TEntity t in list) {
            TEntity? u = data?[pos++];

            Assert.NotNull(u);
            Assert.True(entityEq(t, u));
        }
    }

    [Fact]
    public async Task Test003_Count() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        EnsureCreatedData();

        int? count = await _entSearchClient.Count();
        List<TEntity>? data = _search?.Read()?.ToList();

        Assert.Equal(HttpStatusCode.OK, _entSearchClient.StatusCode);
        Assert.NotNull(count);
        Assert.Equal(count, data?.Count);
    }

    protected abstract IEnumerable<string>? UseCaseQuery();

    // Test concrete queries.
    [Fact]
    public async Task Test004_QueryOk() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        EnsureCreatedData();

        IEnumerable<string>? queries = UseCaseQuery();

        Assert.NotNull(queries);
        Assert.NotEmpty(queries);

        foreach (string query in queries) {
            List<TEntity>? list = await _entSearchClient.Read(query);

            Assert.Equal(HttpStatusCode.OK, _entSearchClient.StatusCode);
        }
    }
}