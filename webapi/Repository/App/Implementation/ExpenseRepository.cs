using webapi.Models;

using Microsoft.EntityFrameworkCore;

namespace webapi.Repository;

public class ExpenseRepository: GenericRepository<Expense, int?>, IExpenseRepository { 
    public ExpenseRepository(DbContext context) : base(context) { }

    public override bool ValidateOnUpsert(Expense item) {
        bool err = item.expense_date == null;
        if (err)
            LastError = "The expense date can't be null.";

        return !err;
    } 
}
