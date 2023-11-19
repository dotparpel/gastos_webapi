using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webapi.Models;

[Table("v_expense_report")]
public class ExpenseReport
{
  [Key]
  public int? expense_id { get; set; }

  public DateTimeOffset? expense_date { get; set; }
  public string? expense_desc { get; set; }
  public decimal expense_amount { get; set; }

  public int? cat_id { get; set; }
  public string? cat_desc { get; set; }
  public int? year { get; set; }
  public int? month { get; set; }
  public int? week { get; set; }
  public int? day { get; set; }
}
