using DataAccess;

namespace DataAccessLayer.Repositories
{
    public class ConversationMemberRepository : GenericRepository<ConversationMember>, IConversationMemberRepository
    {
        public ConversationMemberRepository(MyDbContext context) : base(context)
        {
        }
    }
}
