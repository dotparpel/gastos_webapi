using webapi.Models;
using webapi.Repository;

namespace webapi.Controllers;

public class ExpenseExpandedController : GenericSearchController<ExpenseExpanded>
{
  public ExpenseExpandedController(ILogger<ExpenseExpandedController> logger, IGenericSearch<ExpenseExpanded> rep) 
    : base(logger, rep) { }
}
