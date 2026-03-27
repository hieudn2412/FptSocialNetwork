using DataAccess;

namespace DataAccessLayer.Repositories
{
    public class ConversationRepository : GenericRepository<Conversation>, IConversationRepository
    {
        public ConversationRepository(MyDbContext context) : base(context)
        {
        }
    }
}
