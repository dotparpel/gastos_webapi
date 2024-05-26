using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace webapi.Models;

public class ExpenseReport: EntityNullableId<int?>
{
  [Key]
  public int? expense_id { get; set; }

  public DateTimeOffset? expense_date { get; set; }
  public string? expense_desc { get; set; }
  public decimal expense_amount { get; set; }

  public int? cat_id { get; set; }
  public string? cat_desc { get; set; }
  public DateTime? expense_date_tz { get; set; }
  public int? year { get; set; }
  public int? month { get; set; }
  public int? week { get; set; }
  public int? day { get; set; }

  [IgnoreDataMember]
  [JsonIgnore]
  public override int? Id => expense_id;
}
