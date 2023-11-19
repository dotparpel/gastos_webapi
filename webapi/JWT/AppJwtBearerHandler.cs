using System.Text.Encodings.Web;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using webapi.Models;
using webapi.Repository;

namespace webapi.JWT;

public class AppJwtBearerHandler : JwtBearerHandler {
  private readonly IJWT _jwt;
  private readonly IUserRepository _rep;

  public AppJwtBearerHandler(
    IOptionsMonitor<JwtBearerOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock
    , IJWT jwt, IUserRepository rep
  ) : base(options, logger, encoder, clock) { 
    _jwt = jwt;
    _rep = rep;
  }

  protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
    AuthenticateResult? ret = null;

    // Test there is an authorization section.
    StringValues authorizationHeaderValues = new();
    if (ret == null && !Context.Request.Headers.TryGetValue("Authorization", out authorizationHeaderValues))
        ret = AuthenticateResult.Fail("Authorization header not found.");

    // Test there is a bearer.
    string? authorizationHeader = authorizationHeaderValues.FirstOrDefault(u => u != null && u.StartsWith("Bearer "));
    if (ret == null && string.IsNullOrEmpty(authorizationHeader)) {
      ret = AuthenticateResult.Fail("Bearer token not found in Authorization header.");
    }

    // Get and validate the token.
    ClaimsPrincipal? principal = null;
    if (ret == null && !string.IsNullOrEmpty(authorizationHeader)) {
      string? token = authorizationHeader?.Substring("Bearer ".Length).Trim();

      principal = _jwt.GetPrincipalFromToken(token);

      if (principal == null)
        ret = AuthenticateResult.Fail("Token invalid.");
    }

    // Get the access key of the user.
    Guid? userAccessKey = null;
    if (ret == null && principal != null) {
      Claim? claimAccessKey = principal.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Sid);
      
      if (claimAccessKey?.Value != null)
        userAccessKey = Guid.Parse(claimAccessKey.Value);
      else
        ret = AuthenticateResult.Fail("No access key found in the token.");
    }

    // Test user exists and is active.
    User? user = null;
    if (ret == null && userAccessKey != null) {
      user = await _rep.GetFirst(u => 
        u.user_access_key == userAccessKey 
        && (
          u.user_login_expire_date == null 
          || u.user_login_expire_date >= DateTimeOffset.UtcNow
        )
      );
    }

    if (ret == null && principal != null && user != null)
      ret = AuthenticateResult.Success(new AuthenticationTicket(principal, "CustomJwtBearer"));
    else
      ret = AuthenticateResult.Fail("Invalid user");

    return ret;
  }
}
