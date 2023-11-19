using Microsoft.EntityFrameworkCore;
using EntityFrameworkCore.EncryptColumn.Extension;
using EntityFrameworkCore.EncryptColumn.Interfaces;
using EntityFrameworkCore.EncryptColumn.Util;

using webapi.Application;
using webapi.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace webapi.Repository;

public class ApiContext : DbContext
{
  private readonly IAppSettings _appSettings;
  private IEncryptionProvider? _encProvider;

  Dictionary<string, string> _entityTable = new (StringComparer.OrdinalIgnoreCase) {
    { "category", "d_category" }
    , { "expense", "t_expense" }
  };

  public ApiContext(DbContextOptions options, IAppSettings appSettings) : base(options) {
    _appSettings = appSettings;

    // Optionally, use column encryption if defined.
    if (!string.IsNullOrEmpty(_appSettings.DbEncriptionKey)) {
      // The key *must* be 32 byte lenght.
      string key= _appSettings.DbEncriptionKey.PadRight(32).Substring(0, 32);
      _encProvider = new GenerateEncryptionProvider(key);
    }
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    if (_encProvider != null)
      modelBuilder.UseEncryption(_encProvider);

    base.OnModelCreating(modelBuilder);
  }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // From: https://github.com/npgsql/npgsql/issues/4176
        if (this.Database.IsNpgsql())
            configurationBuilder
                .Properties<DateTimeOffset>()
                .HaveConversion<DateTimeOffsetConverter>();
    }

  private void DeleteEntity<TEntity>(DbSet<TEntity> dbset) where TEntity: class {
    if (this.Database.IsRelational())
      dbset.ExecuteDelete();

    if (this.Database.IsInMemory()) {
      // Delete one by one.
      List<TEntity> list = dbset.ToList();
      foreach(TEntity ent in dbset)
        dbset.Remove(ent);
    }
  }

  private void ResetSequence(string entity) {
    // PostgreSQL.
    if (this.Database.IsNpgsql()) {
      if (entity == "category")
        this.Database.ExecuteSql($"ALTER SEQUENCE public.d_category_cat_id_seq RESTART 1;");
      if (entity == "expense")
        this.Database.ExecuteSql($"ALTER SEQUENCE public.t_expense_expense_id_seq RESTART 1;");
    }

    // SQL-Server.
    if (this.Database.IsSqlServer())
      this.Database.ExecuteSqlRaw($"DBCC CHECKIDENT ('{_entityTable[entity]}', RESEED, 1);");
  }

  private void SetSequenceMax(string table) {
    // PostgreSQL.
    if (this.Database.IsNpgsql()) {
      if (table == "category")
        this.Database.ExecuteSql($"SELECT setval('d_category_cat_id_seq', (SELECT max(cat_id) FROM d_category));");
      if (table == "expense")
        this.Database.ExecuteSql($"SELECT setval('t_expense_expense_id_seq', (SELECT max(expense_id) FROM t_expense));");
    }
  }

  public void DeleteEntities(List<string> entityList) {
    foreach (string entity in entityList) {
      if (entity.ToLower() == "category" && this.Category != null) {
        DeleteEntity<Category>(this.Category);
        ResetSequence("category");
      }

      if (entity.ToLower() == "expense" && this.Expense != null) {
        DeleteEntity<Expense>(this.Expense);
        ResetSequence("expense");
      }
    }
  }

  public void ResetEntitiesCounter(List<string> entityList) {
    foreach (string entity in entityList) {
        SetSequenceMax(entity.ToLower());
    }
  }

  public void FromDbContext(DbContext ctx, List<string> entityList) {
    
    IDbContextTransaction? transaction = null;

    try {
      if (this.Database.IsRelational())
        transaction = this.Database.BeginTransaction();
        
      foreach (string entity in entityList) {
        if (this.Database.IsSqlServer())
          this.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {_entityTable[entity]} ON");

        if (entity.ToLower() == "category")
          this.Category?.AddRange(ctx.Set<Category>());

        if (entity.ToLower() == "expense")
          this.Expense?.AddRange(ctx.Set<Expense>());
        
        this.SaveChanges();

        if (this.Database.IsSqlServer())
          this.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {_entityTable[entity]} OFF");
      }
      
      if (transaction != null)
        transaction.Commit();
    } catch (Exception) {
      if (transaction != null)
        transaction.Rollback();

      throw;
    }

    ResetEntitiesCounter(entityList);
  }

  public DbSet<Category>? Category {get; set; }
  public DbSet<Expense>? Expense {get; set; }
  public DbSet<ExpenseExpanded>? ExpenseExpanded {get; set; }
  public DbSet<ExpenseReport>? ExpenseReport {get; set; }
  public DbSet<User>? User {get; set; }
}

// From: https://github.com/npgsql/npgsql/issues/4176
public class DateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public DateTimeOffsetConverter()
        : base(
            d => d.ToUniversalTime(),
            d => d.ToUniversalTime())
    { }
}