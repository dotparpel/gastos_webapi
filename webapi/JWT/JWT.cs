using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using webapi.Application;

namespace webapi.JWT;

public class JWT : IJWT {
  private const string DEFAULT_ACCESS_KEY = "";
  private const int DEFAULT_ACCESS_EXPIRATION_MINUTES = 1;
  private const int DEFAULT_REFRESH_KEY_LENGHT = 64;
  public const decimal DEFAULT_REFRESH_EXPIRATION_MINUTES = 1440;

  private readonly IAppSettings _appSettings;

  public JWT(IAppSettings appSettings) {
    _appSettings = appSettings;
  }

  public decimal GetValidatedAccessMinutes(decimal? accessTokenExpireMinutes) {
    decimal ret = _appSettings?.Jwt?.AccessTokenExpirationMinutes ?? DEFAULT_ACCESS_EXPIRATION_MINUTES;

    // Can't request for more expiration minutes than the specifiend in "appsettings.json".
    if (accessTokenExpireMinutes != null 
      && accessTokenExpireMinutes > 0.0m && accessTokenExpireMinutes < ret
    )
      ret = accessTokenExpireMinutes ?? 0.0m;

    return ret;
  }

  public decimal GetValidatedRefreshMinutes(decimal? refreshTokenExpirationMinutes) {
    decimal ret = _appSettings.Jwt?.RefreshExpirationMinutes ?? DEFAULT_REFRESH_EXPIRATION_MINUTES;

    // Can't request for more expiration minutes than the specifiend in "appsettings.json".
    if (refreshTokenExpirationMinutes != null 
      && refreshTokenExpirationMinutes > 0.0m && refreshTokenExpirationMinutes < ret
    )
      ret = refreshTokenExpirationMinutes ?? 0.0m;

    return ret;
  }

  public AccessAndRefreshToken? GetAccessTokenAndRefreshKey(Guid accessKey
    , decimal? accessTokenExpireMinutes = null, decimal? refreshTokenExpirationMinutes = null
  ) {
    AccessAndRefreshToken? ret = null;
    string? accessToken;
    string? refreshKey;

    string key = _appSettings.Jwt?.Key ?? DEFAULT_ACCESS_KEY;

    // Access and refresh expiration minutes.
    decimal accessMinutes = GetValidatedAccessMinutes(accessTokenExpireMinutes);
    decimal refreshMinutes = GetValidatedRefreshMinutes(refreshTokenExpirationMinutes);

    // Generate access token and refresh key.
    accessToken = GenerateJSONWebToken(key, accessMinutes, accessKey);
    refreshKey = GetRandomKey(_appSettings.Jwt?.RefreshKeyLength ?? DEFAULT_REFRESH_KEY_LENGHT);

    if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshKey)) {
      ret = new AccessAndRefreshToken() {
        token_access = accessToken
        , token_refresh = refreshKey
        , token_access_expiration_minutes = accessMinutes
        , token_refresh_expiration_minutes = refreshMinutes
      };
    }

    return ret;
  }

  public string? GenerateJSONWebToken(string key, decimal minutes, Guid accessKey) {
    string? ret = null;
    JwtSecurityToken? token;

    // Unique identifier per JWT token.
    List<Claim> claims = new()
    {
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        // We want to identify the user given a token.
        new Claim(ClaimTypes.Sid, accessKey.ToString())
    };

    SymmetricSecurityKey securityKey = new (Encoding.UTF8.GetBytes(key));
    SigningCredentials credentials = new (securityKey, SecurityAlgorithms.HmacSha256);

    token = new JwtSecurityToken(
      _appSettings.Jwt?.Issuer
      , _appSettings.Jwt?.Audience
      , claims: claims
      , expires: DateTime.Now.AddMinutes((double) minutes)
      , signingCredentials: credentials
    );
    
    if (token != null)
      ret = new JwtSecurityTokenHandler().WriteToken(token);

    return ret;
  }
  
  public ClaimsPrincipal? GetPrincipalFromToken(string? token) {
    string? key = _appSettings.Jwt?.Key;

    return GetPrincipalFromToken(token, key);
  }

  public ClaimsPrincipal? GetPrincipalFromToken(string? token, string? key, bool? validateLifetime = true) {
    ClaimsPrincipal? ret = null;

    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(token)) {
      SymmetricSecurityKey securityKey = new (Encoding.UTF8.GetBytes(key));

      TokenValidationParameters validationParameters  = new()
      {
        ClockSkew = TimeSpan.Zero
        , ValidateIssuer = true
        , ValidIssuer = _appSettings.Jwt?.Issuer
        , ValidateAudience = true
        , ValidAudience = _appSettings.Jwt?.Audience
        , ValidateLifetime = validateLifetime ?? true
        , ValidateIssuerSigningKey = true
        , IssuerSigningKey = securityKey
      };

      JwtSecurityTokenHandler tokenHandler = new();
      try
      {
          ret = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
      }
      catch (Exception) { }
    }

    return ret;
  }

  public string GetRandomKey(int length) {
    byte[] randomNumber = RandomNumberGenerator.GetBytes(length);

    return Convert.ToBase64String(randomNumber);
  }
}