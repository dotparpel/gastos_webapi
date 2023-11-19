using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using webapi.Models;

namespace webapi.Repository;

public class SqliteContext : DbContext
{
  readonly string _filename;
  bool _disposed = false;

  public SqliteContext(string filename) {
    _filename = filename;
  }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
      var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = _filename };
      var connectionString = connectionStringBuilder.ToString();
      var connection = new SqliteConnection(connectionString);

      optionsBuilder.UseSqlite(connection);
  }

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
      configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToLongConverter>();
  }

  // From: https://github.com/dotnet/efcore/issues/26580
  public override void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

  protected void Dispose(bool disposing) {
    if (!_disposed && disposing) {
      SqliteConnection.ClearAllPools();
      Database.GetDbConnection().Close();
      base.Dispose();
    }

    _disposed = true;
  }

  public void FromDbContext(DbContext ctx, List<string> entityList) {
    foreach (string entity in entityList) {
      IEntityType? type = this.Model.GetEntityTypes().Where(u => u.Name.Contains("." + entity)).FirstOrDefault();
      IEntityType? typectx = ctx.Model.GetEntityTypes().Where(u => u.Name.Contains("." + entity)).FirstOrDefault();

      if (type != null && typectx != null) {
        var varctx = this.GetType().GetMethods().First(m => m.Name == "Set" && m.IsGenericMethod)?.MakeGenericMethod(typectx.GetType()).Invoke(this, null);

        if (entity.ToLower() == "category")
          this.Category?.AddRange(ctx.Set<Category>());

        if (entity.ToLower() == "expense")
          this.Expense?.AddRange(ctx.Set<Expense>());
      }
    }

    this.SaveChanges();
  }

  public DbSet<Category>? Category {get; set; }
  public DbSet<Expense>? Expense {get; set; }
}

public class DateTimeOffsetToLongConverter : ValueConverter<DateTimeOffset, long>
{
    public DateTimeOffsetToLongConverter() : base(Serialize, Deserialize, null) { }
            
    static Expression<Func<long, DateTimeOffset>> Deserialize = x => DateTimeOffset.FromUnixTimeMilliseconds(x);
    static Expression<Func<DateTimeOffset, long>> Serialize = x => x.ToUnixTimeMilliseconds();
}