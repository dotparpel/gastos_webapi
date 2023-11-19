using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;

using webapi.Models;
using webapi.Repository;

namespace webapi.Controllers;

public abstract class GenericCRUDController<TTable, TKey> : GenericSearchController<TTable>
  where TTable : Entity<TKey>, new()
{
  private readonly IGenericRepository<TTable, TKey> _rep;
  private readonly ILogger<GenericCRUDController<TTable, TKey>> _logger;

  // From: https://dev.to/kondrashov/odata-for-aspnet-core-60-on-mac-via-command-line-55ei
  // From: https://dev.to/berviantoleo/odata-with-net-6-5e1p
  public GenericCRUDController(ILogger<GenericCRUDController<TTable, TKey>> logger, IGenericRepository<TTable, TKey> rep) : base(logger, rep)
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

  /// <summary>
  /// Create {an} {item}.
  /// </summary>
  /// <param name="item"></param>
  /// <param name="returnEntity"></param>
  /// <returns>Item created</returns>
  /// <response code="200">Returns the newly created {item} (if 'returnEntity' is TRUE)</response>
  /// <response code="201">Returns the newly created {item} id (if 'returnEntity' is FALSE)</response>
  /// <response code="204">Returns nothing, indicates {Item} updated</response>
  /// <response code="400">The item is null or invalid</response>
  /// <response code="401">Unauthorized</response>
  /// <response code="500">Internal server error</response>
  [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Object))]
  [ProducesResponseType(StatusCodes.Status201Created)]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  [Consumes("application/json")]
  [Produces("application/json")]
  [Authorize]
  public async virtual Task<IActionResult> Post([FromBody] TTable item, bool? returnEntity = false) {
    // New element.
    try {
      TTable? ret = null;

      if (ModelState.IsValid)
        ret = await _rep.Upsert(item);

      if (ret != null) {
        if (item.Id == null || !item.Id.Equals(ret.Id)) {
          if (returnEntity ?? false)
            return Ok(ret);
          else 
            return StatusCode(StatusCodes.Status201Created, ret.Id);
        } else
          return Updated(ret);
      } else {
        if (_rep.LastError != null)
          ModelState.AddModelError("LastError", _rep.LastError);

        return BadRequest(ModelState);
      }

    } catch(Exception ex) {
      return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
    }
  }

  /// <summary>
  /// Update an existing {item}.
  /// </summary>
  /// <param name="key" example="1">Item Id</param>
  /// <param name="delta"></param>
  /// <param name="returnEntity"></param>
  /// <returns>Item updated</returns>
  /// <response code="200">Returns the {item} updated (if 'returnEntity' is TRUE)</response>
  /// <response code="204">Returns nothing, indicates {Item} updated (if 'returnEntity' is FALSE)</response>
  /// <response code="400">If the item is null or invalid</response>
  /// <response code="401">Unauthorized</response>
  /// <response code="404">{Item} not found</response>
  /// <response code="500">Internal server error</response>
  [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Object))]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  [Consumes("application/json")]
  [Produces("application/json")]
  [Authorize]
  public async virtual Task<IActionResult> Patch(TKey key, [FromBody] Delta<TTable> delta, bool? returnEntity = false)
  {
    try {
      TTable? itemDb = null;

      if (!ModelState.IsValid)
        return BadRequest(ModelState);
      
      itemDb = await _rep.Get(key);

      if (itemDb == null)
        return NotFound();

      delta.Patch(itemDb);

      TTable? ret = await _rep.Upsert(itemDb);

      if (ret != null) {
          if (returnEntity ?? false)
            return Ok(ret);
          else 
            return Updated(ret);
      } else {
        if (_rep.LastError != null)
          ModelState.AddModelError("LastError", _rep.LastError);
        
        return BadRequest(ModelState);
      }

    } catch (Exception ex) {
      return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
    }
  }

  /// <summary>
  /// Delete {an} {item}.
  /// </summary>
  /// <param name="key" example="1">Item Id</param>
  /// <returns>Nothing</returns>
  /// <response code="204">{Item} deleted</response>
  /// <response code="400">The {item} can't be deleted, it's used by other entity</response>
  /// <response code="401">Unauthorized</response>
  /// <response code="404">{Item} not found</response>
  /// <response code="500">Internal server error</response>
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  [Produces("application/json")]
  [Authorize]
  public async virtual Task<IActionResult> Delete(TKey key)
  {
    try {
      TTable? itemDb = await _rep.Get(key);

      if (itemDb == null)
        return NotFound();

      TTable? ret = await _rep.Delete(key);

      if (ret != null)
        return NoContent();
      else {
        if (_rep.LastError != null)
          ModelState.AddModelError("LastError", _rep.LastError);
        
        return BadRequest(ModelState);
      }

    } catch (Exception ex) {
      return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
    }
  }
}
