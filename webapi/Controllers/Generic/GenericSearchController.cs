using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

using webapi.Repository;

namespace webapi.Controllers;

public abstract class GenericSearchController<TTable> : ODataController
  where TTable : class
{
  private readonly IGenericSearch<TTable> _rep;
  private readonly ILogger<GenericSearchController<TTable>> _logger;

  public GenericSearchController(ILogger<GenericSearchController<TTable>> logger, IGenericSearch<TTable> rep)
  {
    _logger = logger;
    _rep = rep;
  }

  /// <summary>
  /// Get a list of the existent {items}.
  /// </summary>
  /// <returns>List of items</returns>
  /// <response code="200">{Item} found and returned</response>
  /// <response code="400">Invalid OData parameters</response>
  /// <response code="401">Unauthorized</response>
  /// <response code="500">Internal server error</response>
  [EnableQuery]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  [Produces("application/json")]
  [Authorize]
  public virtual IQueryable<TTable>? Get() => _rep.Read();

}
