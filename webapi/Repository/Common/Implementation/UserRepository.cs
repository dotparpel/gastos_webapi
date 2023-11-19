using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using webapi.Extensions;
using webapi.Models;

namespace webapi.Repository;

public class UserRepository: GenericRepository<User, int?>, IUserRepository
{
  public UserRepository(DbContext context) : base(context) { }

  public bool IsRepeated(User item)
  {
    bool ret = false;

    // If the item is new, try to find an element with the same code.
    IQueryable<User>? qry = null;
    if (item.user_id == null)
      qry = Read(u => u.user_login == item.user_login);

    if (qry != null)
      ret = qry.Count() > 0;

    return ret;
  }

  public override bool ValidateOnUpsert(User item) {
    LastError = null;

    bool err = 
      string.IsNullOrEmpty(item.user_login) || item.user_login.Trim() == ""
      || string.IsNullOrEmpty(item.user_pwd) || item.user_pwd.Trim() == "";

    if (err)
      LastError = "You must provide a user code and a password.";

    if (!err) {
      err = IsRepeated(item);
      if (err)
        LastError = $"The user '{item.user_login}' already exists.";
    }

    return !err;
  }

  public List<User>? FromConnectionStringToList(string? connStr) {
    List<User>? ret = new List<User>();

    if (!string.IsNullOrEmpty(connStr)) {
    Dictionary<string, string>? userDict 
      = connStr?.ToDictionary(stringComparer: StringComparer.OrdinalIgnoreCase);

    if (userDict != null && userDict.Count > 0) {
        // Try to get the user and pwd in classic mode (ex, "Username=admin;Password=pwd").
        if (userDict.ContainsKey("username") && userDict.ContainsKey("password")) {
          ret = new() { 
            new User() {
              user_login = userDict["username"]
              , user_pwd = userDict["password"]
            }
          };
        }

        // Try to get "n" users in JSON format.
        if (userDict.ContainsKey("users")) {
          Dictionary<string, string>? dict = JsonSerializer.Deserialize<Dictionary<string, string>>(userDict["users"]);

          if (dict != null && dict.Count > 0)
            foreach(KeyValuePair<string, string> pair in dict) {
              if (ret == null)
                ret = new();
                
              ret.Add(new User() {
                user_login = pair.Key
                , user_pwd = pair.Value
              });
            }
        }
      }
    }

    return ret;
  }

  public async Task<int> InsertFromList(List<User>? list) {
    int ret = 0;

    if (list != null && list.Count > 0)
      foreach (User item in list) {
        User? userReaded = Read(u => u.user_login == item.user_login)?.FirstOrDefault();

        if (userReaded == null 
          && !string.IsNullOrEmpty(item.user_login) && !string.IsNullOrEmpty(item.user_pwd)
        ) {
          User? userUpdated = await Upsert(
            new User() {
              user_login = item.user_login
              , user_pwd = item.user_pwd
            });

          if (userUpdated != null)
            ret++;
        }
      }
    
    return ret;
  }
}
