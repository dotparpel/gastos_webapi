using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using webapi.Application;
using webapi.Extensions;
using webapi.JWT;
using webapi.Models;
using webapi.Repository;

namespace webapi.Controllers;

public class AppController : Controller {
  private readonly IAppSettings _appSettings;
  private readonly ApiContext _ctx;
  private readonly IUserRepository _rep;
  private readonly IWebHostEnvironment _env;
  private readonly IJWT _jwt;

  public AppController(IAppSettings appSettings
    , ApiContext context
    , IUserRepository rep
    , IWebHostEnvironment env
    , IJWT jwt
  ) {
    _appSettings = appSettings;
    _ctx = context;
    _rep = rep;
    _env = env;
    _jwt = jwt;
  }
  
  [HttpGet]
  [Route("")]
  [Produces("text/html")]
  public IActionResult Main() {
    if (_appSettings.UseSwaggerUI ?? false)
      return Redirect("~/swagger");
    else
      return Welcome();
  }

  /// <summary>
  /// Version of the Api.
  /// </summary>
  [HttpGet]
  [Route("version")]
  [Produces("text/plain")]
  public string Version() => _appSettings.Version;

  /// <summary>
  /// Welcome message.
  /// </summary>
  private ContentResult Welcome() {
    string html = @"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"" />
  <link rel=""icon"" type=""image/x-icon"" href=""/swagger/favicon.ico"">
  <title>{Title}</title>
</head>
<body>
  <h2>{Title} v.{Version}</h2>
</body>
</html>
";

    html = html.Replace("{Title}", _appSettings.Title);
    html = html.Replace("{Version}", _appSettings.Version);

    return base.Content(html, "text/html");
  }

  /// <summary>
  /// Get a JWT token.
  /// </summary>
  /// <param name="login"></param>
  /// <response code="200">Returns a JWT token for an authenticated user</response>
  /// <response code="401">Otherwise</response>
  /// <response code="500">Internal server error</response>
  [HttpPost]
  [Route("login")]
  [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccessAndRefreshToken))]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  [Consumes("application/json")]
  [Produces("application/json")]
  public async Task<IActionResult> Login([FromBody] Login login) {
    AccessAndRefreshToken? ret = null;
    User? userUpdated = null;
    
    if (login != null && ModelState.IsValid) {
      User? user = await _rep.GetFirst(
        u => u.user_login == login.user && u.user_pwd == login.pwd
          && (u.user_login_expire_date == null || u.user_login_expire_date >= DateTimeOffset.UtcNow)
      );

      Guid? accessKey = null;
      if (user != null) {
        // Generate access token and refresh key.
        accessKey = Guid.NewGuid();
        ret = _jwt.GetAccessTokenAndRefreshKey(accessKey ?? default
          , login.token_access_expiration_minutes, login.token_refresh_expiration_minutes);
      }

      if (ret != null && user != null
        && accessKey != null && !string.IsNullOrEmpty(ret.token_refresh)
      )
          userUpdated = await UpdateUserSessionInfo(
            user, accessKey ?? default, ret.token_refresh
            , ret.token_access_expiration_minutes, ret.token_refresh_expiration_minutes
          );
    }

    if (ret != null && userUpdated != null)
      return Ok(ret);
    else
      return Unauthorized();
  }

  /// <summary>
  /// Obtain a new access token and a new refresh token.
  /// </summary>
  [HttpPost]
  [Route("refreshToken")]
  [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccessAndRefreshToken))]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  [Consumes("application/json")]
  [Produces("application/json")]
  public async Task<IActionResult> RefreshToken([FromBody] AccessAndRefreshToken tokens)
  {
    AccessAndRefreshToken? ret = null;
    User? userUpdated = null;

    string? key = _appSettings.Jwt?.Key;

    if (ModelState.IsValid && tokens != null && !string.IsNullOrEmpty(key)
      && !string.IsNullOrEmpty(tokens.token_access) 
      && !string.IsNullOrEmpty(tokens.token_refresh)
    ) {
      // Get the id of the user accepting outdated tokens.
      Guid? userAccessKey = null;
      ClaimsPrincipal? principal = _jwt.GetPrincipalFromToken(tokens.token_access, key, false);

      if (principal != null) {
        Claim? claimAccessKey = principal.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Sid);
        
        if (claimAccessKey?.Value != null)
          userAccessKey = Guid.Parse(claimAccessKey.Value);
      }

      // Search the user with the specified id and the active refresh token.
      User? user = null;
      if (userAccessKey != null) {
        user = await _rep.GetFirst(u => 
          u.user_access_key == userAccessKey 
          && u.user_refresh_key != null && u.user_refresh_key == tokens.token_refresh
          && u.user_refresh_expire_date != null && u.user_refresh_expire_date >= DateTimeOffset.UtcNow
        );
      }

      // Generate new access token and refresh key.
      Guid? accessKey = null;
      if (user != null) {
        accessKey = Guid.NewGuid();

        // Access and refresh expiration minutes.
        decimal accessMinutes = _jwt.GetValidatedAccessMinutes(
          tokens.token_access_expiration_minutes ?? user.user_access_token_expire_minutes);

        decimal refreshMinutes = _jwt.GetValidatedRefreshMinutes(
          tokens.token_refresh_expiration_minutes ?? user.user_refresh_token_expire_minutes);

        ret = _jwt.GetAccessTokenAndRefreshKey(accessKey ?? default, accessMinutes, refreshMinutes);
      }

      if (user != null && ret != null 
        && accessKey != null && !string.IsNullOrEmpty(ret.token_refresh)
      )
          userUpdated = await UpdateUserSessionInfo(
            user, accessKey ?? default, ret.token_refresh
            , ret.token_access_expiration_minutes, ret.token_refresh_expiration_minutes
          );
    }

    if (ret != null && userUpdated != null)
      return Ok(ret);
    else
      return Unauthorized();
  }

  /// <summary>
  /// Invalidates the JWT token and the refresh key.
  /// </summary>
  /// <response code="200">Returns 'true' if success</response>
  /// <response code="401">Otherwise</response>
  /// <response code="500">Internal server error</response>
  [HttpPost]
  [Route("logout")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  [Authorize]
  public async Task<IActionResult> Logout() {
    User? userUpdated = null;
    
    // Get the access token.
    string? token = null;
    if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues)) {
      string? authorizationHeader = authorizationHeaderValues.FirstOrDefault();

      if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
        token = authorizationHeader["Bearer ".Length..].Trim();
    }
    
    if (!string.IsNullOrEmpty(token)) {
      // Get the id of the user.
      Guid? userAccessKey = null;
      ClaimsPrincipal? principal = _jwt.GetPrincipalFromToken(token);

      if (principal != null) {
        Claim? claimAccessKey = principal.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Sid);
        
        if (claimAccessKey?.Value != null)
          userAccessKey = Guid.Parse(claimAccessKey.Value);
      }

      // Search the user with the specified access key.
      User? user = null;
      if (userAccessKey != null)
        user = await _rep.GetFirst(u => u.user_access_key == userAccessKey);

      if (user != null)
          userUpdated = await UpdateUserSessionInfo(user);
    }

    if (userUpdated != null)
      return Ok();
    else
      return Unauthorized();
  }

  /// <summary>
  /// Get a copy of the DB entities in Sqlite format.
  /// </summary>
  /// <param name="entityList">List of entities to return</param>
  /// <response code="200">Returns a Sqlite database</response>
  /// <response code="401">Unauthorized</response>
  /// <response code="404">Effective list of entities is empty</response>
  /// <response code="500">Internal server error</response>
  [HttpGet]
  [Route("export")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  [Produces("application/octet-stream", "application/json")]
  [Authorize]
  public IActionResult Export(List<string>? entityList) {
    const string TEMPLATE = @"/templates/template.db";
    string rootDirectory = _env.WebRootPath;
    string template = Path.Join(rootDirectory, TEMPLATE);

    string temp = Path.GetTempFileName();

    // The template exists.
    if (!System.IO.File.Exists(template))
      return StatusCode(StatusCodes.Status500InternalServerError, $"Template {TEMPLATE} not found");

    // Get the list of templates to export.
    List<string>? list = GetEffectiveEntityList(entityList);
    if (list == null || !list.Any())
      return NotFound();

    // Copy the template in a temp directory with a unique name.
    System.IO.File.Copy(template, temp, true);

    using (SqliteContext sqlite = new (temp)) {
      sqlite.FromDbContext(_ctx, list);
    }

    // Returns the Sqlite database.
    Stream? stream = new FileStream(temp, FileMode.Open, FileAccess.Read
      , FileShare.None, 4096, FileOptions.DeleteOnClose);

    String now = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
    String fileDownloadName = (_appSettings.Title ?? "app").Replace(' ', '_') + "_" + now + ".db";
    return File(stream, "application/octet-stream", fileDownloadName);
  }

  /// <summary>
  /// Provide the entities in Sqlite format.
  /// </summary>
  /// <param name="file">SQLite to import</param>
  /// <param name="entityList">List of entities to import</param>
  /// <response code="200">The data has been imported</response>
  /// <response code="400">The file is not a SQLite database or is empty</response>
  /// <response code="401">Unauthorized</response>
  /// <response code="500">Internal server error</response>
  [HttpPost]
  [Route("import")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  [Authorize]
  public ActionResult Import(IFormFile? file, List<string>? entityList) {
    if (file == null || file.Length == 0 || !file.IsSqlite())
      return BadRequest();
    
    // Get the list of templates to export.
    List<string>? list = GetEffectiveEntityList(entityList);
    if (list == null || !list.Any())
      return NotFound();

    // Copy the file upoloaded in a temp directory with a unique name.
    string temp = Path.GetTempFileName();
    Stream? stream = new FileStream(temp, FileMode.Open, FileAccess.Write
      , FileShare.None, 4096);
    file.CopyTo(stream);
    stream.Close();

    using (SqliteContext sqlite = new (temp)) {
      _ctx.DeleteEntities(list);
      list.Reverse();
      _ctx.FromDbContext(sqlite, list);
    }

    System.IO.File.Delete(temp);

    return Ok();
  }

  private async Task<User?> UpdateUserSessionInfo(
    User user, Guid? accessKey = null, string? refreshKey = null
    , decimal? accessTokenExpirationMinutes = null, decimal? refreshTokenExpirationMinutes = null
  ) {
    User? userUpdated;
    DateTimeOffset? refreshExpireDate = null;

    // Calculate the refresh expire date.
    if (!string.IsNullOrEmpty(refreshKey) && refreshTokenExpirationMinutes != null) {
      decimal refreshMinutes = _jwt.GetValidatedRefreshMinutes(refreshTokenExpirationMinutes);
      refreshExpireDate = DateTimeOffset.UtcNow.AddMinutes((double) refreshTokenExpirationMinutes);
    }

    // Update the user with the access and refresh values.
    user.user_access_key = accessKey;
    user.user_refresh_key = refreshKey;
    user.user_access_token_expire_minutes = accessTokenExpirationMinutes;
    user.user_refresh_token_expire_minutes = refreshTokenExpirationMinutes;
    user.user_refresh_expire_date = refreshExpireDate;

    userUpdated = await _rep.Upsert(user);

    return userUpdated;
  }

  private List<string>? GetEffectiveEntityList(List<string>? entityList) {
    List<string>? list = null;

    // Get the exportable entities specified in the "appSettings" file..
    List<string>? listexport = _appSettings.ExportEntities?.ToList();
    
    if (listexport != null && listexport.Any()) {
      if (entityList != null && entityList.Any())
        list = entityList.Intersect(listexport).ToList();
      else
        list = listexport;
    }
    
    return list;
  }
}