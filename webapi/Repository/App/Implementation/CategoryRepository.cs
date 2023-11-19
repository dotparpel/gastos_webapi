using webapi.Models;

using Microsoft.EntityFrameworkCore;

namespace webapi.Repository;

public class CategoryRepository: GenericRepository<Category, int?>, ICategoryRepository
{
  private readonly IExpenseRepository _expRep;
  
  public CategoryRepository(DbContext context, IExpenseRepository expRep) : base(context) { 
    _expRep = expRep;
  }

  public IExpenseRepository ExpenseRepository => _expRep;

  public bool IsRepeated(Category item)
  {
    bool ret = false;

    // Try to find an element with the same code.
    IQueryable<Category>? qry = Read(
      u => u.cat_desc == item.cat_desc 
        && (item.cat_id == null || !item.Id.Equals(u.cat_id))
    );

    if (qry != null)
      ret = qry.Count() > 0;

    return ret;
  }

  public bool IsUsed(Category item) {
    bool ret = false;

    IQueryable<Expense>? qry = _expRep.Read(u => u.cat_id == item.cat_id);

    if (qry != null)
      ret = qry.Count() > 0;

    return ret;
  }

  public override bool ValidateOnUpsert(Category item) {
    bool err = string.IsNullOrEmpty(item.cat_desc) || item.cat_desc.Trim() == "";
    if (err)
      LastError = "The category description can't be empty.";

    if (!err) {
      err = IsRepeated(item);
      if (err)
        LastError = $"The category '{item.cat_desc}' already exists.";
    }

    return !err;
  } 

  public override bool ValidateOnDelete(Category item) {
    bool err = IsUsed(item);
    if (err)
      LastError = $"The category '{item.cat_desc}' is being used in some expenses.";

    return !err;
  }
}
