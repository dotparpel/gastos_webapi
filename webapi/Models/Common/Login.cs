namespace webapi.Models;

public class Login {
  public string? user { get; set; }
  public string? pwd { get; set; }
  public decimal? token_access_expiration_minutes { get; set; }
  public decimal? token_refresh_expiration_minutes { get; set; }
}
