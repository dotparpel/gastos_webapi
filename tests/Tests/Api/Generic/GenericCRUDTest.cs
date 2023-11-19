using System.Net;

using tests.Client;
using webapi.Models;
using webapi.JWT;

namespace tests.Api;

public abstract class GenericCRUDTest<TFixture, TEntity, TKey> : GenericSearchTest<TFixture, TEntity>
    where TEntity : Entity<TKey>, new()
    where TFixture : ApiFactory<Program>
{
    private readonly ODataEntityKeyResponse<TEntity, TKey> _entResp;
    protected readonly GenericCRUDClient<TEntity, TKey> _entCRUDClient;
    
    public GenericCRUDTest(TFixture factory) : base(factory) {
        _entResp = new ODataEntityKeyResponse<TEntity, TKey>();
        _entCRUDClient = new GenericCRUDClient<TEntity, TKey>(_client, _entResp);
    }

    protected abstract IEnumerable<TEntity>? UseCaseInsertOkSet1();

    [Fact]
    public async Task Test101_InsertReturnsEntityOk() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        IEnumerable<TEntity>? list = UseCaseInsertOkSet1();

        Assert.NotNull(list);
        Assert.NotEmpty(list);

        foreach (TEntity ent in list) {
            await InsertReturnsEntityOk(ent);
        }
    }

    private async Task InsertReturnsEntityOk(TEntity ent) {
        List<TEntity>? listBefore = await _entSearchClient.Read();

        TEntity? inserted = await _entCRUDClient.InsertReturnsEntity(ent);

        Assert.Equal(HttpStatusCode.OK, _entCRUDClient.StatusCode);
        Assert.NotNull(inserted);
        Assert.NotNull(inserted.Id);

        List<TEntity>? listAfter = await _entSearchClient.Read();

        Assert.NotNull(listAfter);
        Assert.NotEmpty(listAfter);

        Assert.True(listBefore == null || listBefore.Where(u => inserted.Id.Equals(u.Id)).Count() == 0);
        Assert.True(listAfter.Where(u => inserted.Id.Equals(u.Id)).Count() == 1);
    }

    protected abstract IEnumerable<TEntity>? UseCaseInsertOkSet2();

    [Fact]
    public async Task Test102_InsertOk() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        IEnumerable<TEntity>? list = UseCaseInsertOkSet2();

        Assert.NotNull(list);
        Assert.NotEmpty(list);

        foreach (TEntity ent in list) {
            List<TEntity>? listBefore = await _entSearchClient.Read();

            TKey? id = await _entCRUDClient.Insert(ent);

            Assert.Equal(HttpStatusCode.Created, _entCRUDClient.StatusCode);
            Assert.NotNull(id);

            List<TEntity>? listAfter = await _entSearchClient.Read();

            Assert.NotNull(listAfter);
            Assert.NotEmpty(listAfter);

            Assert.True(listBefore == null || listBefore.Where(u => id.Equals(u.Id)).Count() == 0);
            Assert.True(listAfter.Where(u => id.Equals(u.Id)).Count() == 1);
        }
    }

    protected abstract TEntity? entityNew(params object[] param);

    [Fact]
    public async Task Test103_InsertEmptyKo() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        TEntity? ent = entityNew();

        Assert.NotNull(ent);

        TKey? id = await _entCRUDClient.Insert(ent);

        Assert.Equal(HttpStatusCode.BadRequest, _entCRUDClient.StatusCode);
        Assert.Null(id);
    }

    protected abstract IEnumerable<TEntity>? UseCaseGetOkSet();

    [Fact]
    public async Task Test104_GetOk() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        IEnumerable<TEntity>? list = UseCaseGetOkSet();

        Assert.NotNull(list);
        Assert.NotEmpty(list);

        foreach (TEntity ent in list) {
            TKey? id = await _entCRUDClient.Insert(ent);

            Assert.Equal(HttpStatusCode.Created, _entCRUDClient.StatusCode);
            Assert.NotNull(id);

            TEntity? getted = await _entCRUDClient.Get(id);

            Assert.NotNull(getted);
            Assert.Equal(id, getted.Id);
            Assert.True(entityEq(ent, getted));
        }
    }

    protected abstract IEnumerable<TKey>? UseCaseInvalidKeys();

    [Fact]
    public async Task Test105_GetInvalidKeysKo() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        IEnumerable<TKey>? list = UseCaseInvalidKeys();

        Assert.NotNull(list);
        Assert.NotEmpty(list);

        foreach (TKey key in list) {
            TEntity? ent = await _entCRUDClient.Get(key);

            Assert.Equal(HttpStatusCode.NotFound, _entCRUDClient.StatusCode);
            Assert.Null(ent);
        }
    }

    protected abstract void entityUpdate(TEntity ent, params object?[] param);

    [Fact]
    public async Task Test106_UpdateReturnsEntityOk() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        TEntity? ent = entityNew("updateOk1");
        Assert.NotNull(ent);

        TEntity? inserted = await _entCRUDClient.InsertReturnsEntity(ent);

        Assert.NotNull(inserted);
        Assert.NotNull(inserted.Id);
        Assert.True(entityEq(ent, inserted));

        List<TEntity>? listBefore = await _entSearchClient.Read();

        entityUpdate(inserted, null, 106);

        TEntity? updated = await _entCRUDClient.UpdateEntity(inserted.Id, inserted);

        Assert.NotNull(updated);
        Assert.True(entityEq(inserted, updated));
    }

    [Fact]
    public async Task Test107_UpdateOk() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        TEntity? ent = entityNew("updateOk2");
        Assert.NotNull(ent);

        TEntity? inserted = await _entCRUDClient.InsertReturnsEntity(ent);

        Assert.NotNull(inserted);
        Assert.NotNull(inserted.Id);
        Assert.True(entityEq(ent, inserted));

        entityUpdate(inserted, null, 107);

        bool updated = await _entCRUDClient.Update(inserted.Id, inserted);

        Assert.True(updated);

        TEntity? getted = await _entCRUDClient.Get(inserted.Id);

        Assert.NotNull(getted);
        Assert.Equal(inserted.Id, getted.Id);
        Assert.True(entityEq(inserted, getted));
    }

    [Fact]
    public async Task Test108_UpdateInvalidKeysKo() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        IEnumerable<TKey>? list = UseCaseInvalidKeys();

        Assert.NotNull(list);
        Assert.NotEmpty(list);

        foreach (TKey key in list) {
            TEntity? ent = entityNew("updateKo1");
            Assert.NotNull(ent);

            bool updated = await _entCRUDClient.Update(key, ent);

            Assert.Equal(HttpStatusCode.NotFound, _entCRUDClient.StatusCode);
            Assert.False(updated);
        }
    }

    [Fact]
    public async Task Test109_DeleteOk() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        TEntity? ent = entityNew("deleteOk");
        Assert.NotNull(ent);

        TEntity? inserted = await _entCRUDClient.InsertReturnsEntity(ent);

        Assert.NotNull(inserted);
        Assert.NotNull(inserted.Id);

        List<TEntity>? listBefore = await _entSearchClient.Read();

        Assert.NotNull(listBefore);
        Assert.NotEmpty(listBefore);
        Assert.True(listBefore.Where(u => inserted.Id.Equals(u.Id)).Count() == 1);

        bool deleted = await _entCRUDClient.Delete(inserted.Id);

        Assert.True(deleted);

        List<TEntity>? listAfter = await _entSearchClient.Read();

        Assert.NotNull(listAfter);
        Assert.True(listAfter == null || listAfter.Where(u => inserted.Id.Equals(u.Id)).Count() == 0);
    }

    [Fact]
    public async Task Test110_DeleteInvalidKeysKo() {
        AccessAndRefreshToken? tokens = await EnsureLogged();
        Assert.NotNull(tokens);

        IEnumerable<TKey>? list = UseCaseInvalidKeys();

        Assert.NotNull(list);
        Assert.NotEmpty(list);

        foreach (TKey key in list) {
            bool ret = await _entCRUDClient.Delete(key);

            Assert.Equal(HttpStatusCode.NotFound, _entCRUDClient.StatusCode);
            Assert.False(ret);
        }
    }
}