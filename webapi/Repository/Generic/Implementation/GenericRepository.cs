using webapi.Models;

using Microsoft.EntityFrameworkCore;

namespace webapi.Repository;

public class GenericRepository<TTable, TKey> : GenericList<TTable, TKey>, IGenericRepository<TTable, TKey>
  where TTable : Entity<TKey>, new()
{

  public GenericRepository(DbContext context) : base(context) { }

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
