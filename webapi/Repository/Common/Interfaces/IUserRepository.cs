using webapi.Models;

namespace webapi.Repository;

public interface IUserRepository : IGenericRepository<User, int?> { 
    List<User>? FromConnectionStringToList(string connStr);
    Task<int> InsertFromList(List<User>? list);
}
