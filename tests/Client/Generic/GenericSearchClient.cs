using System.Net;

namespace tests.Client;

public class GenericSearchClient<TEntity>
    where TEntity: class?
{
    protected const string DEFAULT_VERSION = "v1";
    protected readonly HttpClient _client;
    protected readonly ODataEntityResponse<TEntity> _oDataResp;
    protected readonly string _version;

    public HttpStatusCode StatusCode { get; set; }

    public GenericSearchClient(
        HttpClient client, ODataEntityResponse<TEntity> oDataResp, string? version = DEFAULT_VERSION
    ) {
        _client = client;
        _oDataResp = oDataResp;
        _version = version ?? DEFAULT_VERSION;
    }

    public string Typename() => typeof(TEntity).Name.ToLower();

    public async Task<List<TEntity>?> Read(string? odataQuery = null) {
        List<TEntity>? list = null;
        
        string param = string.IsNullOrEmpty(odataQuery) ? "" : "?$" + odataQuery;
        string query = $"/{_version}/{Typename()}{param}";
        HttpResponseMessage resp = await _client.GetAsync(query);
        StatusCode = resp.StatusCode;

        if (resp.StatusCode == HttpStatusCode.OK)
            list = _oDataResp.GetODataEntityList(resp);

        return list;
    }

    public async Task<int?> Count() {
        int? ret = null;

        HttpResponseMessage resp = await _client.GetAsync($"/{_version}/{Typename()}/$count");
        StatusCode = resp.StatusCode;
        
        string? numStr = _oDataResp.GetData(resp);

        if (resp.StatusCode == HttpStatusCode.OK) {
            if (!string.IsNullOrEmpty(numStr) && int.TryParse(numStr, out int val))
                ret = val;
        }

        return ret;
    }
}