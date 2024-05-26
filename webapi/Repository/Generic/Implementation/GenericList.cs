using Microsoft.EntityFrameworkCore;

namespace webapi.Repository;

public class GenericList<TView, TKey> : GenericSearch<TView>, IGenericList<TView, TKey>
  where TView : class
{
  public GenericList(DbContext context) : base(context) {  }

  public virtual async Task<TView?> Get(TKey id, bool? asNoTracking = true)
  {
    TView? ret = null;
    LastError = null;

    ret = await _dbset.FindAsync(id);

    if ((asNoTracking ?? false) && ret != null)
      _ctx.Entry(ret).State = EntityState.Detached;

    return ret;
  }
}
