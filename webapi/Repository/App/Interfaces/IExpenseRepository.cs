using webapi.Models;

namespace webapi.Repository;

public interface IExpenseRepository : IGenericRepository<Expense, int?> { }
