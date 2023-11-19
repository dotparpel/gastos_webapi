using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace webapi.Repository;

public class GenericSearch<TView> : IGenericSearch<TView>
  where TView : class
{
  protected readonly DbContext _ctx;
  protected readonly DbSet<TView> _dbset;

  public GenericSearch(DbContext context)
  {
    _ctx = context;
    _dbset = _ctx.Set<TView>();
  }

  public DbContext Context => _ctx;

  public virtual IQueryable<TView>? Read(Expression<Func<TView, bool>>? predicate = null, bool? asNoTracking = true)
  {
    IQueryable<TView>? qry = null;

    // Execució de la consulta.
    qry = _dbset;

    if (predicate != null)
      qry = qry.Where(predicate);

    if (asNoTracking ?? false)
      qry = qry.AsNoTracking();

    return qry;
  }

  public virtual async Task<TView?> GetFirst(Expression<Func<TView, bool>>? predicate = null, bool? asNoTracking = true) {
    TView? ret = null;

    IQueryable<TView>? qry = Read(predicate, asNoTracking);

    if (qry != null)
      ret = await qry.FirstOrDefaultAsync();

    return ret;
  }
}
