using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

using webapi.Models;
using webapi.Repository;

namespace webapi.Controllers;

public class ExpenseReportController : ODataController
{
  private readonly IExpenseReportSearch _rep;
  private readonly ILogger<ExpenseReportController> _logger;

  public ExpenseReportController(ILogger<ExpenseReportController> logger, IExpenseReportSearch rep) { 
    _logger = logger;
    _rep = rep;
  }

  /// <summary>
  /// Get a list of the existent {items}.
  /// </summary>
  /// <param name="timezone" example="Europe/Madrid">A 'timezone' in IANA or Windows format</param>
  /// <returns>List of items</returns>
  /// <response code="200">Items found and returned</response>
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
  public IQueryable<ExpenseReport>? Get(string? timezone = null) => _rep.Read(timezone);
}
