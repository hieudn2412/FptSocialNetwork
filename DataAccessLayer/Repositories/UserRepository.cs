using DataAccess;

namespace DataAccessLayer.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(MyDbContext context) : base(context)
        {
        }
    }
}
