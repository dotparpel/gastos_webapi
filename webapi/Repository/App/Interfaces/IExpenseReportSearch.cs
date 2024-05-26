using System.Linq.Expressions;
using webapi.Models;

namespace webapi.Repository;

public interface IExpenseReportSearch : IGenericSearch<ExpenseReport> { 
    IQueryable<ExpenseReport>? Read(
      string? timezone = null, Expression<Func<ExpenseReport, bool>>? predicate = null, bool? asNoTracking = true);
}
