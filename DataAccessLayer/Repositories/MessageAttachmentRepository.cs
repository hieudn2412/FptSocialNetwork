using DataAccess;

namespace DataAccessLayer.Repositories
{
    public class MessageAttachmentRepository : GenericRepository<MessageAttachment>, IMessageAttachmentRepository
    {
        public MessageAttachmentRepository(MyDbContext context) : base(context)
        {
        }
    }
}
