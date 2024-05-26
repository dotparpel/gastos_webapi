namespace webapi.Repository;

public interface IGenericRepository<TTable, TKey> : IGenericList<TTable, TKey>
{
  Task<TTable?> Upsert(TTable item);
  Task<TTable?> Delete(TKey id);
  public string? LastError { get; }
}
