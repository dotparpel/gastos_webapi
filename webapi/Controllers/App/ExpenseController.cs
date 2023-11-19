using webapi.Models;
using webapi.Repository;

namespace webapi.Controllers;

public class ExpenseController : GenericCRUDController<Expense, int?>
{
  public ExpenseController(ILogger<ExpenseController> logger, IExpenseRepository rep) 
    : base(logger, rep) { }
}
