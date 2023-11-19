using webapi.Models;
using webapi.Repository;

namespace webapi.Controllers;

public class UserController : GenericCRUDController<User, int?>
{
  public UserController(ILogger<UserController> logger, IUserRepository rep) 
    : base(logger, rep) { }
}
