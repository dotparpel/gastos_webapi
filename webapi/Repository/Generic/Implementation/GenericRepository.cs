using webapi.Models;

using Microsoft.EntityFrameworkCore;

namespace webapi.Repository;

public class GenericRepository<TTable, TKey> : GenericSearch<TTable>, IGenericRepository<TTable, TKey>
  where TTable : Entity<TKey>, new()
{

  public GenericRepository(DbContext context) : base(context) { }

  public string? LastError { get; protected set; }

  public virtual async Task<TTable?> Get(TKey id, bool asNoTracking = true)
  {
    TTable? ret = null;
    LastError = null;

    ret = await _dbset.FindAsync(id);

    if (asNoTracking && ret != null)
      _ctx.Entry(ret).State = EntityState.Detached;

    return ret;
  }

  public virtual bool ValidateOnUpsert(TTable item) => true;

  public virtual bool ValidateOnDelete(TTable item) => true;

  public async Task<TTable?> Upsert(TTable item)
  {
    TTable? ret = null;
    LastError = null;

    if (ValidateOnUpsert(item)) {
      TTable? itemDb = null;
      
      if (item.Id != null)
        itemDb = await Get(item.Id, false);

      if (itemDb == null) {
        // Create the entity.
        itemDb = new TTable();
        itemDb.CopyOnNew(item!);

        _dbset.Add(itemDb);
      }

      // Update the entity.
      if (itemDb != null) {
        itemDb.CopyOnUpdate(item);

        await _ctx.SaveChangesAsync();
        ret = itemDb;
      }
    }

    return ret;
  }

  public async Task<TTable?> Delete(TKey id)
  {
    TTable? ret = null;
    LastError = null;

    TTable? itemDb = await Get(id, false);

    if (itemDb != null && ValidateOnDelete(itemDb))
    {
      _dbset.Remove(itemDb);

      await _ctx.SaveChangesAsync();
      ret = itemDb;
    }

    return ret;
  }
}
