
using EntityFrameworkCore.EncryptColumn.Attribute;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace webapi.Models;

[Table("d_user")]
public class User : Entity<int?>
{
  [Key]
  public int? user_id { get; set; }

  [MaxLength(128)]
  public string? user_login { get; set; }

  [EncryptColumn]
  public string? user_pwd { get; set; }

  public Guid? user_access_key { get; set; }

  public decimal? user_access_token_expire_minutes {get; set; }

  [MaxLength(128)]
  public string? user_refresh_key { get; set; }

  public decimal? user_refresh_token_expire_minutes {get; set; }

  public DateTimeOffset? user_refresh_expire_date { get; set; }

  public DateTimeOffset? user_login_expire_date { get; set; }

  [IgnoreDataMember]
  [JsonIgnore]
  public override int? Id => user_id;

  public override void CopyOnNew(Entity<int?> orig) { }

  public override void CopyOnUpdate(Entity<int?> orig) {
    if (orig == null || orig is not User)
      throw new Exception(OBJECT_MUST_BE_SAME_CLASS);

    User? entity = orig as User;

    user_login = entity!.user_login;
    user_pwd = entity!.user_pwd;
    user_access_key = entity!.user_access_key;
    user_access_token_expire_minutes = entity!.user_access_token_expire_minutes;
    user_refresh_key = entity!.user_refresh_key;
    user_refresh_token_expire_minutes = entity!.user_refresh_token_expire_minutes;
    user_refresh_expire_date = entity!.user_refresh_expire_date;
    user_login_expire_date = entity!.user_login_expire_date;
  }
}
