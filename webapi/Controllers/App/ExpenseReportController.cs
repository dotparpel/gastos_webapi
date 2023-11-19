using webapi.Models;
using webapi.Repository;

namespace webapi.Controllers;

public class ExpenseReportController : GenericSearchController<ExpenseReport>
{
  public ExpenseReportController(ILogger<ExpenseReportController> logger, IGenericSearch<ExpenseReport> rep) 
    : base(logger, rep) { }
}
