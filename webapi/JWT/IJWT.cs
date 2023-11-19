using System.Security.Claims;

namespace webapi.JWT;

public interface IJWT {
  decimal GetValidatedAccessMinutes(decimal? accessTokenExpireMinutes);
  decimal GetValidatedRefreshMinutes(decimal? refreshTokenExpirationMinutes);
  AccessAndRefreshToken? GetAccessTokenAndRefreshKey(Guid accessKey
    , decimal? accessTokenExpireMinutes = null, decimal? refreshTokenExpirationMinutes = null);
  string? GenerateJSONWebToken(string key, decimal minutes, Guid accessKey);
  ClaimsPrincipal? GetPrincipalFromToken(string? token);
  ClaimsPrincipal? GetPrincipalFromToken(string? key, string? token, bool? validateLifetime = true);
  string GetRandomKey(int length);
}