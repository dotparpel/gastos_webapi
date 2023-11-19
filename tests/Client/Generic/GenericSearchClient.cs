using System.Net;

namespace tests.Client;

public class GenericSearchClient<Entity> {
    private const string DEFAULT_VERSION = "v1";
    protected readonly HttpClient _client;
    protected readonly ODataEntityResponse<Entity> _oDataResp;
    protected readonly string _version;

    public HttpStatusCode StatusCode { get; set; }

    public GenericSearchClient(
        HttpClient client, ODataEntityResponse<Entity> oDataResp, string? version = DEFAULT_VERSION
    ) {
        _client = client;
        _oDataResp = oDataResp;
        _version = version ?? DEFAULT_VERSION;
    }

    public string Typename() => typeof(Entity).Name.ToLower();

    public async Task<List<Entity>?> Read(string? odataQuery = null) {
        List<Entity>? list = null;
        
        string param = string.IsNullOrEmpty(odataQuery) ? "" : "?$" + odataQuery;
        HttpResponseMessage resp = await _client.GetAsync($"/{_version}/{Typename()}{param}");
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