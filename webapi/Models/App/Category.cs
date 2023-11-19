using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace webapi.Models;

[Table("d_category")]
public class Category : Entity<int?>
{
  [Key]
  public int? cat_id { get; set; }

  [Required]
  [MaxLength(64)]
  public string? cat_desc { get; set; }
  
  public int? cat_order { get; set; }

  public IEnumerable<Expense>? Expense { get; set; }

  [IgnoreDataMember]
  [JsonIgnore]
  public override int? Id => cat_id;

  public override void CopyOnNew(Entity<int?> orig) { }

  public override void CopyOnUpdate(Entity<int?> orig) {
    if (orig == null || orig is not Category)
      throw new Exception(OBJECT_MUST_BE_SAME_CLASS);

    Category? entity = orig as Category;

    cat_desc = entity!.cat_desc;
    cat_order = entity!.cat_order;
  }
}
