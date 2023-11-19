using System.Net;
using System.Net.Http.Headers;
using tests.Extensions;
using webapi.JWT;

namespace tests.Client;

public class AppClient {
    private readonly HttpClient _client;
    protected readonly ODataEntityResponse<AccessAndRefreshToken> _oDataResp;
    public HttpStatusCode StatusCode { get; set; }
    private AccessAndRefreshToken? _tokens;

    public AccessAndRefreshToken? Tokens { 
        get => _tokens; 
        set {
            _tokens = value;

            // Assign the token to the client.
            if (_tokens != null)
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.token_access);
            else
                _client.DefaultRequestHeaders.Authorization = null;
        }
    }

    public string? Token => _tokens?.token_access;

    public string? RefreshKey => _tokens?.token_refresh;

    public AppClient(HttpClient client, ODataEntityResponse<AccessAndRefreshToken> oDataResp) {
        _client = client;
        _oDataResp = oDataResp;
    }

    public async Task<AccessAndRefreshToken?> Login(string user, string pwd
        , decimal? accessTokenExpirationMinutes = null, decimal? refreshTokenExpirationMinutes = null) 
    {
        AccessAndRefreshToken? ret = null;
        
        webapi.Models.Login login = new () {
            user = user
            , pwd = pwd
            , token_access_expiration_minutes = accessTokenExpirationMinutes
            , token_refresh_expiration_minutes = refreshTokenExpirationMinutes
        };

        HttpResponseMessage resp = await _client.PostAsync("/login", login.ToStringContent());
        StatusCode = resp.StatusCode;

        // Get the token.
        if (StatusCode == HttpStatusCode.OK)
            ret = _oDataResp.GetOdataEntity(resp);

        Tokens = ret;
        
        return ret;
    }

    public async Task<AccessAndRefreshToken?> RefreshToken(AccessAndRefreshToken? tokens) {
        AccessAndRefreshToken? ret = null;

        HttpResponseMessage resp = await _client.PostAsync("/refreshToken", tokens.ToStringContent());
        StatusCode = resp.StatusCode;

        // Get the token.
        if (StatusCode == HttpStatusCode.OK)
            ret = _oDataResp.GetOdataEntity(resp);
        
        Tokens = ret;
        
        return ret;
    }

    public async Task<AccessAndRefreshToken?> RefreshToken(string token, string refreshKey) {
        AccessAndRefreshToken? ret;

        AccessAndRefreshToken tokens = new () {
            token_access = token
            , token_refresh = refreshKey
        };

        ret = await RefreshToken(tokens);

        return ret;
    }

    public async Task<AccessAndRefreshToken?> RefreshToken() {
        AccessAndRefreshToken? ret;

        ret = await RefreshToken(_tokens);

        return ret;
    }

    public async Task<bool> Logout() {
        HttpResponseMessage resp = await _client.PostAsync("/logout", null);
        StatusCode = resp.StatusCode;
        
        bool ret = resp.StatusCode == HttpStatusCode.OK;

        if (ret)
            Tokens = null;

        return ret;
    }

    public async Task<byte[]?> Export() {
        using var result = await _client.GetAsync("/export");
        return result.IsSuccessStatusCode ? await result.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<bool> Import(byte[]? bytes) {
        bool ret = false;

        if (bytes != null) {
            HttpResponseMessage resp = await _client.PostAsync("/import", new MultipartFormDataContent {
                { bytes.ToByteArrayContent()!, "file", "import.db" }
            });

            ret = (resp.StatusCode == HttpStatusCode.OK);
        }

        return ret;
    }
}