using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using webapi.Repository;

namespace webapi.Controllers;

public abstract class GenericListController<TTable, TKey> : GenericSearchController<TTable>
  where TTable : class
{
  protected new readonly IGenericList<TTable, TKey> _rep;
  private readonly ILogger<GenericListController<TTable, TKey>> _logger;

  public GenericListController(ILogger<GenericListController<TTable, TKey>> logger, IGenericList<TTable, TKey> rep)
    : base(logger, rep)
  {
    _logger = logger;
    _rep = rep;
  }

  /// <summary>
  /// Get {an} {item}.
  /// </summary>
  /// <param name="key" example="1">Item Id</param>
  /// <returns>Item getted</returns>
  /// <response code="200">{Item} found and returned</response>
  /// <response code="401">Unauthorized</response>
  /// <response code="404">{Item} not found</response>
  /// <response code="500">Internal server error</response>
  [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Object))]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  [Produces("application/json")]
  [Authorize]
  public async virtual Task<IActionResult> Get(TKey key) {
    try {
      TTable? ret = await _rep.Get(key);
      
      if (ret != null)
        return Ok(ret);
      else
        return NotFound();

    } catch(Exception ex) {
      return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
    }
  }
}
