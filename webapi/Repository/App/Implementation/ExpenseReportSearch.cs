using webapi.Models;

using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace webapi.Repository;

public class ExpenseReportSearch: IExpenseReportSearch { 
  private readonly ApiContext _ctx;

  public ExpenseReportSearch(ApiContext context) { 
    _ctx = context;
  }

  public async Task<ExpenseReport?> GetFirst(Expression<Func<ExpenseReport, bool>>? predicate = null, string? timezone = null, bool? asNoTracking = true)
  {
    ExpenseReport? ret = null;

    IQueryable<ExpenseReport>? qry = Read(timezone, predicate, asNoTracking);

    if (qry != null)
      ret = await qry.FirstOrDefaultAsync();

    return ret;
  }

  public async Task<ExpenseReport?> GetFirst(Expression<Func<ExpenseReport, bool>>? predicate = null, bool? asNoTracking = true)
    => await GetFirst(predicate, null, asNoTracking);

  public IQueryable<ExpenseReport>? Read(
    string? timezone = null, Expression<Func<ExpenseReport, bool>>? predicate = null, bool? asNoTracking = true
  ) 
  {
    IQueryable<ExpenseReport>? ret = null;

    TimeZoneInfo? tz = null;
    if (timezone != null)
      tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
      
    if (_ctx.Database.IsNpgsql()) {
      string? tzId = GetIanaTimeZone(tz);

      ret = Read(predicate, asNoTracking, tzId);
    }

    if (_ctx.Database.IsSqlServer() || _ctx.Database.IsInMemory()) {
      string? tzId = GetWindowsTimeZone(tz);

      ret = Read(predicate, asNoTracking, tzId);
    }

    return ret;
  }

  public IQueryable<ExpenseReport>? Read(Expression<Func<ExpenseReport, bool>>? predicate = null, bool? asNoTracking = true)
    => Read(null, predicate, asNoTracking);

    // From: https://devblogs.microsoft.com/dotnet/date-time-and-time-zone-enhancements-in-net-6/#time-zone-conversion-apis
  private string? GetIanaTimeZone(TimeZoneInfo? tz) {
    string? ret = null;

    if (tz != null) {
      if (string.IsNullOrEmpty(ret) && tz.HasIanaId)
        ret = tz.Id;

      if (string.IsNullOrEmpty(ret) && TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out string? tzId))
        ret = tzId;
    }

    return ret;
  }

  private string? GetWindowsTimeZone(TimeZoneInfo? tz) {
    string? ret = null;

    if (tz != null) {
      if (string.IsNullOrEmpty(ret) && !tz.HasIanaId)
        ret = tz.Id;

      if (string.IsNullOrEmpty(ret) && TimeZoneInfo.TryConvertIanaIdToWindowsId(tz.Id, out string? tzId))
        ret = tzId;
    }

    return ret;
  }

  private IQueryable<ExpenseReport>? Read(
    Expression<Func<ExpenseReport, bool>>? predicate = null, bool? asNoTracking = true, string? timezone = null
    ) 
  {
    IQueryable<ExpenseReport>? ret = null;

    if (_ctx != null) {
      ret = _ctx.fn_expense_report(timezone);

      if (predicate != null)
        ret = ret.Where(predicate);

      if (asNoTracking ?? false)
        ret = ret.AsNoTracking();
    }

    return ret;
  }
}
