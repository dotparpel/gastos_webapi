namespace webapi.Repository;

public interface IGenericRepository<TTable, TKey> : IGenericSearch<TTable>
{
  Task<TTable?> Get(TKey id, bool asNoTracking = true);
  Task<TTable?> Upsert(TTable item);
  Task<TTable?> Delete(TKey id);
  public string? LastError { get; }
}
