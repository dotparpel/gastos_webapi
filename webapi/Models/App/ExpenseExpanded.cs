using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace webapi.Models;

[Table("v_expense")]
public class ExpenseExpanded : EntityNullableId<int?>
{
  [Key]
  public int? expense_id { get; set; }

  public DateTimeOffset? expense_date { get; set; }
  public string? expense_desc { get; set; }
  public decimal expense_amount { get; set; }

  public int? cat_id { get; set; }
  public string? cat_desc { get; set; }

  [IgnoreDataMember]
  [JsonIgnore]
  public override int? Id => expense_id;
}
