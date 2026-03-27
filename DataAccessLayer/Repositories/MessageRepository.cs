using DataAccess;

namespace DataAccessLayer.Repositories
{
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        public MessageRepository(MyDbContext context) : base(context)
        {
        }
    }
}
