using webapi.Models;
using webapi.Repository;

namespace webapi.Controllers;

public class CategoryController : GenericCRUDController<Category, int?>
{
  public CategoryController(ILogger<CategoryController> logger, ICategoryRepository rep) 
    : base(logger, rep) { }
}
