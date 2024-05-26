using System.Net;

using tests.Client;
using webapi.Models;
using webapi.Repository;
using webapi.JWT;

namespace tests.Api;

[TestCaseOrderer(
    ordererTypeName: "tests.Xunit.AlphabeticalOrderer",
    ordererAssemblyName: "tests")]
public abstract class GenericListTest<TFixture, TEntity, TKey> : GenericSearchTest<TFixture, TEntity>
    where TEntity : EntityNullableId<TKey>
    where TFixture : ApiFactory<Program>
{
    protected readonly GenericListClient<TEntity, TKey> _entListClient;
    protected new IGenericList<TEntity, TKey>? _search;

    public GenericListTest(TFixture factory) : base(factory) {
        _entListClient = new GenericListClient<TEntity, TKey>(_client, _entResp);
        _search = _factory.GetList<TEntity, TKey>();
    }

    [Fact]
    public async Task Test101_GetOk()
    {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        EnsureCreatedData();

        // Act.
        List<TEntity>? data = _search?.Read()?.ToList();

        Assert.NotNull(data);
        Assert.True(data.Count > 0);

        TEntity first = data.First();
        TKey? id = first.Id;

        Assert.NotNull(id);

        TEntity? getted = await _entListClient.Get(id);

        Assert.NotNull(getted);

        Assert.True(entityEq(first, getted));
    }
}