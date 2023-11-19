using System.Net;
using tests.Extensions;

namespace tests.Client;

public class GenericCRUDClient<Entity, TKey> : GenericSearchClient<Entity> {
    private const string DEFAULT_VERSION = "v1";
    protected new readonly ODataEntityKeyResponse<Entity, TKey> _oDataResp;

    public GenericCRUDClient(
        HttpClient client, ODataEntityKeyResponse<Entity, TKey> oDataResp, string? version = DEFAULT_VERSION
    ) : base(client, oDataResp, version) { 
        _oDataResp = oDataResp;
    }

    public async Task<Entity?> Get(TKey id) {
        Entity? ret = default;

        HttpResponseMessage resp = await _client.GetAsync($"/{_version}/{Typename()}/{id}");
        StatusCode = resp.StatusCode;

        if (resp.StatusCode == HttpStatusCode.OK)
            ret = _oDataResp.GetOdataEntity(resp);

        return ret;
    }

    public async Task<TKey?> Insert(Entity ent) {
        TKey? ret = default;

        HttpResponseMessage resp = await _client.PostAsync($"/{_version}/{Typename()}?returnEntity=false", ent.ToStringContent());
        StatusCode = resp.StatusCode;

        if (resp.StatusCode == HttpStatusCode.Created)
            ret = _oDataResp.GetODataKey(resp);

        return ret;
    }

    public async Task<Entity?> InsertReturnsEntity(Entity ent) {
        Entity? ret = default;

        HttpResponseMessage resp = await _client.PostAsync($"/{_version}/{Typename()}?returnEntity=true", ent.ToStringContent());
        StatusCode = resp.StatusCode;

        if (resp.StatusCode == HttpStatusCode.OK)
            ret = _oDataResp.GetOdataEntity(resp);

        return ret;
    }

    public async Task<bool> Update(TKey id, Entity ent) {
        HttpResponseMessage resp = await _client.PatchAsync($"/{_version}/{Typename()}/{id}?returnEntity=false", ent.ToStringContent());
        StatusCode = resp.StatusCode;

        return resp.StatusCode == HttpStatusCode.NoContent;
    }

    public async Task<Entity?> UpdateEntity(TKey id, Entity ent) {
        Entity? ret = default;

        HttpResponseMessage resp = await _client.PatchAsync($"/{_version}/{Typename()}/{id}?returnEntity=true", ent.ToStringContent());
        StatusCode = resp.StatusCode;

        if (resp.StatusCode == HttpStatusCode.OK)
            ret = _oDataResp.GetOdataEntity(resp);

        return ret;
    }

    public async Task<bool> Delete(TKey id) {
        HttpResponseMessage resp = await _client.DeleteAsync($"/{_version}/{Typename()}/{id}");
        StatusCode = resp.StatusCode;

        return resp.StatusCode == HttpStatusCode.NoContent;
    }
}