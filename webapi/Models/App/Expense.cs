using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace webapi.Models;

[Table("t_expense")]
public class Expense : Entity<int?>
{
  [Key]
  public int? expense_id { get; set; }

  [Required]
  public DateTimeOffset? expense_date { get; set; }

  [MaxLength(128)]
  public string? expense_desc { get; set; }
  
  public decimal expense_amount { get; set; }

  [ForeignKey("Category")]
  public int? cat_id { get; set; }
  public Category? Category { get; set; }

  [IgnoreDataMember]
  [JsonIgnore]
  public override int? Id => expense_id;

  public override void CopyOnNew(Entity<int?> orig) { }

  public override void CopyOnUpdate(Entity<int?> orig) {
    if (orig == null || orig is not Expense)
      throw new Exception(OBJECT_MUST_BE_SAME_CLASS);

    Expense? entity = orig as Expense;

    expense_date = entity!.expense_date;
    expense_desc = entity!.expense_desc;
    expense_amount = entity!.expense_amount;
    cat_id = entity!.cat_id;
  }
}
