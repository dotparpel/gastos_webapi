namespace webapi.JWT;

public class AccessAndRefreshToken {
  public string? token_access { get; set; }
  public string? token_refresh { get; set; }
  public decimal? token_access_expiration_minutes { get; set; }
  public decimal? token_refresh_expiration_minutes { get; set; }
}
