using System.Net;

namespace tests.Client;

public class GenericListClient<TEntity, TKey> : GenericSearchClient<TEntity>
    where TEntity: class?
{
    public GenericListClient(
        HttpClient client, ODataEntityResponse<TEntity> oDataResp, string? version = DEFAULT_VERSION
    ) : base(client, oDataResp, version) { }

    public async Task<TEntity?> Get(TKey id) {
        TEntity? ret = null;

        HttpResponseMessage resp = await _client.GetAsync($"/{_version}/{Typename()}/{id}");
        StatusCode = resp.StatusCode;

        if (resp.StatusCode == HttpStatusCode.OK)
            ret = _oDataResp.GetOdataEntity(resp);

        return ret;
    }
}