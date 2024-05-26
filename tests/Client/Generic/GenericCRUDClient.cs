using System.Net;
using tests.Extensions;

namespace tests.Client;

public class GenericCRUDClient<TEntity, TKey> : GenericListClient<TEntity, TKey> 
    where TEntity: class?
{
    protected new readonly ODataEntityKeyResponse<TEntity, TKey> _oDataResp;

    public GenericCRUDClient(
        HttpClient client, ODataEntityKeyResponse<TEntity, TKey> oDataResp, string? version = DEFAULT_VERSION
    ) : base(client, oDataResp, version) { 
        _oDataResp = oDataResp;
    }

    public async Task<TKey?> Insert(TEntity ent) {
        // It is not allowed to declare the variable "ret" as "TKey?" without declaring the type as "class" or "struct".
        dynamic? ret = null;

        HttpResponseMessage resp = await _client.PostAsync($"/{_version}/{Typename()}?returnEntity=false", ent.ToStringContent());
        StatusCode = resp.StatusCode;

        if (resp.StatusCode == HttpStatusCode.Created)
            ret = _oDataResp.GetODataKey(resp);

        return ret;
    }

    public async Task<TEntity?> InsertReturnsEntity(TEntity ent) {
        TEntity? ret = null;

        HttpResponseMessage resp = await _client.PostAsync($"/{_version}/{Typename()}?returnEntity=true", ent.ToStringContent());
        StatusCode = resp.StatusCode;

        if (resp.StatusCode == HttpStatusCode.OK)
            ret = _oDataResp.GetOdataEntity(resp);

        return ret;
    }

    public async Task<bool> Update(TKey id, TEntity ent) {
        HttpResponseMessage resp = await _client.PatchAsync($"/{_version}/{Typename()}/{id}?returnEntity=false", ent.ToStringContent());
        StatusCode = resp.StatusCode;

        return resp.StatusCode == HttpStatusCode.NoContent;
    }

    public async Task<TEntity?> UpdateEntity(TKey id, TEntity ent) {
        TEntity? ret = null;

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