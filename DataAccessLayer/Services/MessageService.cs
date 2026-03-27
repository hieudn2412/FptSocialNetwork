using DataAccess;
using DataAccessLayer.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Services
{
    public class MessageService : IMessageService
    {
        private readonly MyDbContext _context;

        public MessageService(MyDbContext context)
        {
            _context = context;
        }

        public async Task<List<MessageDto>> GetMessagesAsync(int conversationId, int currentUserId, int page, int pageSize)
        {
            if (!await IsMemberAsync(conversationId, currentUserId))
            {
                throw new UnauthorizedAccessException("You are not a member of this conversation.");
            }

            if (page <= 0)
            {
                page = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 30;
            }

            var skip = (page - 1) * pageSize;

            var messages = await QueryMessagesForUser(currentUserId)
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            messages.Reverse();
            foreach (var message in messages)
            {
                NormalizeDeletedMessage(message);
            }

            return messages;
        }

        public async Task<MessageSearchResultDto> SearchMessagesAsync(int conversationId, int currentUserId, string keyword, int page, int pageSize)
        {
            if (!await IsMemberAsync(conversationId, currentUserId))
            {
                throw new UnauthorizedAccessException("You are not a member of this conversation.");
            }

            var normalizedKeyword = keyword?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                return new MessageSearchResultDto();
            }

            if (page <= 0)
            {
                page = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 30;
            }

            var skip = (page - 1) * pageSize;
            var pattern = $"%{normalizedKeyword}%";

            var query = _context.Messages
                .Where(m => m.ConversationId == conversationId &&
                            !m.IsDeletedForEveryone &&
                            !m.HiddenByUsers.Any(h => h.UserId == currentUserId) &&
                            EF.Functions.Like(m.Content, pattern));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.SentAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(m => new MessageSearchItemDto
                {
                    MessageId = m.Id,
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FullName,
                    SenderAvatarUrl = m.Sender.AvatarUrl,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    EditedAt = m.EditedAt
                })
                .ToListAsync();

            return new MessageSearchResultDto
            {
                TotalCount = totalCount,
                Items = items
            };
        }

        public async Task<MessageDto> SendMessageAsync(SendMessageRequest request)
        {
            var hasContent = !string.IsNullOrWhiteSpace(request.Content);
            var hasAttachment = request.Attachments is { Count: > 0 };
            if (!hasContent && !hasAttachment)
            {
                throw new InvalidOperationException("Message content or attachment is required.");
            }

            if (!await IsMemberAsync(request.ConversationId, request.SenderId))
            {
                throw new UnauthorizedAccessException("You are not a member of this conversation.");
            }

            if (request.ReplyToMessageId.HasValue)
            {
                var validReply = await _context.Messages.AnyAsync(m =>
                    m.Id == request.ReplyToMessageId.Value &&
                    m.ConversationId == request.ConversationId &&
                    !m.IsDeletedForEveryone);
                if (!validReply)
                {
                    throw new InvalidOperationException("Reply target is invalid.");
                }
            }

            var message = new Message
            {
                ConversationId = request.ConversationId,
                SenderId = request.SenderId,
                ReplyToMessageId = request.ReplyToMessageId,
                Content = request.Content?.Trim() ?? string.Empty,
                MessageType = string.IsNullOrWhiteSpace(request.MessageType) ? "text" : request.MessageType.Trim().ToLowerInvariant(),
                SentAt = DateTime.UtcNow,
                IsDeletedForEveryone = false
            };

            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();

            if (request.Attachments is { Count: > 0 })
            {
                var attachments = request.Attachments.Select(a => new MessageAttachment
                {
                    MessageId = message.Id,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    AttachmentType = a.AttachmentType
                });

                await _context.MessageAttachments.AddRangeAsync(attachments);
                await _context.SaveChangesAsync();
            }

            var senderMembership = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == request.ConversationId && cm.UserId == request.SenderId);
            if (senderMembership is not null)
            {
                senderMembership.LastReadAt = message.SentAt;
                await _context.SaveChangesAsync();
            }

            var created = await GetMessageByIdAsync(message.Id, request.SenderId);
            return created!;
        }

        public async Task<MessageDto> EditMessageAsync(long messageId, int userId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Message content is empty.");
            }

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId);
            if (message is null)
            {
                throw new InvalidOperationException("Message not found.");
            }

            if (!await IsMemberAsync(message.ConversationId, userId))
            {
                throw new UnauthorizedAccessException("You are not a member of this conversation.");
            }

            if (message.SenderId != userId)
            {
                throw new InvalidOperationException("Only sender can edit this message.");
            }

            if (message.IsDeletedForEveryone)
            {
                throw new InvalidOperationException("Cannot edit a deleted message.");
            }

            message.Content = content.Trim();
            message.EditedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var updated = await GetMessageByIdAsync(messageId, userId);
            return updated!;
        }

        public async Task<MessageDto> ToggleReactionAsync(long messageId, int userId, string reactionType)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId);
            if (message is null)
            {
                throw new InvalidOperationException("Message not found.");
            }

            if (!await IsMemberAsync(message.ConversationId, userId))
            {
                throw new UnauthorizedAccessException("You are not a member of this conversation.");
            }

            var normalizedReaction = string.IsNullOrWhiteSpace(reactionType) ? "like" : reactionType.Trim().ToLowerInvariant();

            var existingMyReactions = await _context.MessageReactions
                .Where(r => r.MessageId == messageId && r.UserId == userId)
                .ToListAsync();

            var sameReaction = existingMyReactions.FirstOrDefault(r => r.ReactionType == normalizedReaction);
            if (sameReaction is not null)
            {
                _context.MessageReactions.Remove(sameReaction);
            }
            else
            {
                if (existingMyReactions.Count > 0)
                {
                    _context.MessageReactions.RemoveRange(existingMyReactions);
                }

                await _context.MessageReactions.AddAsync(new MessageReaction
                {
                    MessageId = messageId,
                    UserId = userId,
                    ReactionType = normalizedReaction,
                    ReactedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            var updated = await GetMessageByIdAsync(messageId, userId);
            return updated!;
        }

        public async Task<MessageDto> UnsendMessageAsync(long messageId, int userId)
        {
            var message = await _context.Messages
                .Include(m => m.Attachments)
                .Include(m => m.Reactions)
                .FirstOrDefaultAsync(m => m.Id == messageId);
            if (message is null)
            {
                throw new InvalidOperationException("Message not found.");
            }

            if (!await IsMemberAsync(message.ConversationId, userId))
            {
                throw new UnauthorizedAccessException("You are not a member of this conversation.");
            }

            if (message.SenderId != userId)
            {
                throw new InvalidOperationException("Only sender can unsend this message.");
            }

            if (message.IsDeletedForEveryone)
            {
                var deleted = await GetMessageByIdAsync(messageId, userId);
                return deleted!;
            }

            message.IsDeletedForEveryone = true;
            message.DeletedAt = DateTime.UtcNow;
            message.Content = "Tin nhắn đã được gỡ";
            message.ReplyToMessageId = null;

            if (message.Attachments.Count > 0)
            {
                _context.MessageAttachments.RemoveRange(message.Attachments);
            }

            if (message.Reactions.Count > 0)
            {
                _context.MessageReactions.RemoveRange(message.Reactions);
            }

            await _context.SaveChangesAsync();
            var updated = await GetMessageByIdAsync(messageId, userId);
            return updated!;
        }

        public async Task<int> DeleteForMeAsync(long messageId, int userId)
        {
            var message = await _context.Messages
                .Select(m => new { m.Id, m.ConversationId })
                .FirstOrDefaultAsync(m => m.Id == messageId);
            if (message is null)
            {
                throw new InvalidOperationException("Message not found.");
            }

            if (!await IsMemberAsync(message.ConversationId, userId))
            {
                throw new UnauthorizedAccessException("You are not a member of this conversation.");
            }

            var existed = await _context.MessageHiddens
                .AnyAsync(h => h.MessageId == messageId && h.UserId == userId);
            if (!existed)
            {
                await _context.MessageHiddens.AddAsync(new MessageHidden
                {
                    MessageId = messageId,
                    UserId = userId,
                    HiddenAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return message.ConversationId;
        }

        private IQueryable<MessageDto> QueryMessagesForUser(int currentUserId)
        {
            return _context.Messages
                .Where(m => !m.HiddenByUsers.Any(h => h.UserId == currentUserId))
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FullName,
                    SenderAvatarUrl = m.Sender.AvatarUrl,
                    ReplyToMessageId = m.ReplyToMessageId,
                    ReplyTo = m.ReplyToMessage == null
                        ? null
                        : new MessageReplyPreviewDto
                        {
                            MessageId = m.ReplyToMessage.Id,
                            SenderId = m.ReplyToMessage.SenderId,
                            SenderName = m.ReplyToMessage.Sender.FullName,
                            Content = m.ReplyToMessage.Content,
                            IsDeletedForEveryone = m.ReplyToMessage.IsDeletedForEveryone
                        },
                    Content = m.Content,
                    MessageType = m.MessageType,
                    SentAt = m.SentAt,
                    EditedAt = m.EditedAt,
                    IsDeletedForEveryone = m.IsDeletedForEveryone,
                    DeletedAt = m.DeletedAt,
                    Attachments = m.Attachments.Select(a => new MessageAttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FileUrl = a.FileUrl,
                        AttachmentType = a.AttachmentType
                    }).ToList(),
                    Reactions = m.Reactions
                        .GroupBy(r => r.ReactionType)
                        .Select(g => new MessageReactionDto
                        {
                            ReactionType = g.Key,
                            Count = g.Count(),
                            IsMine = g.Any(r => r.UserId == currentUserId)
                        }).ToList(),
                    SeenBy = m.Conversation.Members
                        .Where(cm => cm.UserId != m.SenderId && (cm.LastReadAt ?? DateTime.MinValue) >= m.SentAt)
                        .Select(cm => new SeenByDto
                        {
                            UserId = cm.UserId,
                            FullName = cm.User.FullName,
                            AvatarUrl = cm.User.AvatarUrl
                        }).ToList(),
                    Receipts = m.Conversation.Members
                        .Where(cm => cm.UserId != m.SenderId)
                        .Select(cm => new MessageReceiptDto
                        {
                            UserId = cm.UserId,
                            FullName = cm.User.FullName,
                            AvatarUrl = cm.User.AvatarUrl,
                            Status = (cm.LastReadAt ?? DateTime.MinValue) >= m.SentAt ? "seen" : "delivered",
                            LastReadAt = cm.LastReadAt
                        }).ToList()
                });
        }

        private async Task<MessageDto?> GetMessageByIdAsync(long messageId, int currentUserId)
        {
            var message = await QueryMessagesForUser(currentUserId)
                .FirstOrDefaultAsync(m => m.Id == messageId);
            if (message is null)
            {
                return null;
            }

            NormalizeDeletedMessage(message);
            return message;
        }

        private static void NormalizeDeletedMessage(MessageDto message)
        {
            if (!message.IsDeletedForEveryone)
            {
                return;
            }

            message.Content = "Tin nhắn đã được gỡ";
            message.Attachments = new List<MessageAttachmentDto>();
            message.Reactions = new List<MessageReactionDto>();
            message.Receipts = new List<MessageReceiptDto>();
            message.ReplyToMessageId = null;
            message.ReplyTo = null;
            message.MessageType = "deleted";
        }

        private async Task<bool> IsMemberAsync(int conversationId, int userId)
        {
            return await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);
        }
    }
}
