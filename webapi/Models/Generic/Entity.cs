namespace webapi.Models;

public abstract class Entity<TKey>
{
  internal const string OBJECT_MUST_BE_SAME_CLASS = "An object of the same class must be passed to the 'CopyOnUpdate' method";

  public abstract TKey? Id { get; }

  public abstract void CopyOnNew(Entity<TKey> orig);
  public abstract void CopyOnUpdate(Entity<TKey> orig);
}
