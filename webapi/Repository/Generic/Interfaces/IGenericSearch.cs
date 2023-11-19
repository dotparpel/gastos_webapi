using System.Linq.Expressions;

namespace webapi.Repository;

public interface IGenericSearch<TView>
{
  IQueryable<TView>? Read(Expression<Func<TView, bool>>? predicate = null, bool? asNoTracking = true);
  Task<TView?> GetFirst(Expression<Func<TView, bool>>? predicate = null, bool? asNoTracking = true);
}
