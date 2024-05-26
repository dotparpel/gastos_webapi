using System.Linq.Expressions;

namespace webapi.Repository;

public interface IGenericList<TView, TKey> : IGenericSearch<TView>
{
  Task<TView?> Get(TKey id, bool? asNoTracking = true);
}
