using DataAccess;
using DataAccessLayer.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Services
{
    public class ConversationService : IConversationService
    {
        private readonly MyDbContext _context;

        public ConversationService(MyDbContext context)
        {
            _context = context;
        }

        public async Task<List<ConversationListItemDto>> GetConversationsAsync(int userId, string? tab, string? keyword)
        {
            var normalizedTab = tab?.Trim().ToLowerInvariant();
            var normalizedKeyword = keyword?.Trim().ToLowerInvariant();

            var query = _context.ConversationMembers
                .Where(cm => cm.UserId == userId)
                .Select(cm => new
                {
                    cm.LastReadAt,
                    Conversation = cm.Conversation,
                    DirectOtherMember = cm.Conversation.Members
                        .Where(m => m.UserId != userId)
                        .Select(m => new
                        {
                            m.User.FullName,
                            m.User.AvatarUrl
                        })
                        .FirstOrDefault(),
                    LatestMessage = cm.Conversation.Messages
                        .Where(m => !m.HiddenByUsers.Any(h => h.UserId == userId))
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => new
                        {
                            m.SenderId,
                            SenderName = m.Sender.FullName,
                            m.Content,
                            m.SentAt
                        })
                        .FirstOrDefault(),
                    UnreadCount = cm.Conversation.Messages.Count(m =>
                        !m.HiddenByUsers.Any(h => h.UserId == userId) &&
                        m.SenderId != userId &&
                        m.SentAt > (cm.LastReadAt ?? DateTime.MinValue))
                });

            if (!string.IsNullOrWhiteSpace(normalizedTab))
            {
                if (normalizedTab == "group")
                {
                    query = query.Where(x => x.Conversation.Type.ToLower() == "group");
                }
                else if (normalizedTab == "unread")
                {
                    query = query.Where(x => x.UnreadCount > 0);
                }
            }

            if (!string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                query = query.Where(x =>
                    x.Conversation.Name.ToLower().Contains(normalizedKeyword!) ||
                    (x.DirectOtherMember != null && x.DirectOtherMember.FullName.ToLower().Contains(normalizedKeyword!)));
            }

            var conversations = await query
                .Select(x => new ConversationListItemDto
                {
                    Id = x.Conversation.Id,
                    Name = x.Conversation.Type.ToLower() == "direct"
                        ? (x.DirectOtherMember != null ? x.DirectOtherMember.FullName : x.Conversation.Name)
                        : x.Conversation.Name,
                    Type = x.Conversation.Type,
                    AvatarUrl = x.Conversation.Type.ToLower() == "direct"
                        ? (x.DirectOtherMember != null ? x.DirectOtherMember.AvatarUrl : x.Conversation.AvatarUrl)
                        : x.Conversation.AvatarUrl,
                    LastMessage = x.LatestMessage != null ? x.LatestMessage.Content : string.Empty,
                    LastSenderId = x.LatestMessage != null ? x.LatestMessage.SenderId : null,
                    LastSenderName = x.LatestMessage != null ? x.LatestMessage.SenderName : string.Empty,
                    LastSentAt = x.LatestMessage != null ? x.LatestMessage.SentAt : null,
                    UnreadCount = x.UnreadCount,
                    HasUnread = x.UnreadCount > 0
                })
                .OrderByDescending(c => c.LastSentAt)
                .ToListAsync();

            return conversations;
        }

        public async Task<ConversationDetailDto?> GetConversationDetailAsync(int conversationId, int userId)
        {
            var isMember = await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);

            if (!isMember)
            {
                return null;
            }

            var conversation = await _context.Conversations
                .Where(c => c.Id == conversationId)
                .Select(c => new ConversationDetailDto
                {
                    Id = c.Id,
                    Name = c.Type.ToLower() == "direct"
                        ? (c.Members.Where(m => m.UserId != userId)
                            .Select(m => m.User.FullName)
                            .FirstOrDefault() ?? c.Name)
                        : c.Name,
                    Type = c.Type,
                    AvatarUrl = c.Type.ToLower() == "direct"
                        ? (c.Members.Where(m => m.UserId != userId)
                            .Select(m => m.User.AvatarUrl)
                            .FirstOrDefault() ?? c.AvatarUrl)
                        : c.AvatarUrl,
                    Members = c.Members.Select(m => new ConversationMemberDto
                    {
                        UserId = m.UserId,
                        FullName = m.User.FullName,
                        AvatarUrl = m.User.AvatarUrl
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return conversation;
        }

        public async Task<ConversationDetailDto> CreateConversationAsync(int currentUserId, CreateConversationRequest request)
        {
            var distinctMemberIds = (request.MemberUserIds ?? new List<int>())
                .Append(currentUserId)
                .Distinct()
                .ToList();

            if (distinctMemberIds.Count < 2)
            {
                throw new InvalidOperationException("Conversation needs at least two members.");
            }

            var existingUserIds = await _context.Users
                .Where(u => distinctMemberIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            if (existingUserIds.Count != distinctMemberIds.Count)
            {
                throw new InvalidOperationException("One or more users do not exist.");
            }

            var conversation = new Conversation
            {
                Name = string.IsNullOrWhiteSpace(request.Name) ? "Nhóm chat mới" : request.Name.Trim(),
                Type = string.IsNullOrWhiteSpace(request.Type) ? "group" : request.Type.Trim().ToLowerInvariant(),
                AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl)
                    ? "https://via.placeholder.com/58"
                    : request.AvatarUrl.Trim()
            };

            await _context.Conversations.AddAsync(conversation);
            await _context.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var members = distinctMemberIds.Select(userId => new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = userId,
                LastReadAt = now
            });

            await _context.ConversationMembers.AddRangeAsync(members);
            await _context.SaveChangesAsync();

            var created = await GetConversationDetailAsync(conversation.Id, currentUserId);
            return created!;
        }

        public async Task<ConversationDetailDto> GetOrCreateDirectConversationAsync(int currentUserId, int targetUserId)
        {
            if (currentUserId == targetUserId)
            {
                throw new InvalidOperationException("Cannot create direct conversation with yourself.");
            }

            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);
            if (targetUser is null)
            {
                throw new InvalidOperationException("Target user not found.");
            }

            var existingConversationId = await _context.Conversations
                .Where(c => c.Type.ToLower() == "direct")
                .Where(c => c.Members.Any(m => m.UserId == currentUserId) &&
                            c.Members.Any(m => m.UserId == targetUserId) &&
                            c.Members.Count == 2)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();

            if (existingConversationId.HasValue)
            {
                var existing = await GetConversationDetailAsync(existingConversationId.Value, currentUserId);
                return existing!;
            }

            var currentUser = await _context.Users.FirstAsync(u => u.Id == currentUserId);
            var conversation = new Conversation
            {
                Name = $"{currentUser.FullName}, {targetUser.FullName}",
                Type = "direct",
                AvatarUrl = targetUser.AvatarUrl
            };

            await _context.Conversations.AddAsync(conversation);
            await _context.SaveChangesAsync();

            var now = DateTime.UtcNow;
            await _context.ConversationMembers.AddRangeAsync(
                new ConversationMember
                {
                    ConversationId = conversation.Id,
                    UserId = currentUserId,
                    LastReadAt = now
                },
                new ConversationMember
                {
                    ConversationId = conversation.Id,
                    UserId = targetUserId,
                    LastReadAt = now
                });
            await _context.SaveChangesAsync();

            var created = await GetConversationDetailAsync(conversation.Id, currentUserId);
            return created!;
        }

        public async Task<ConversationDetailDto> UpdateConversationSettingsAsync(int conversationId, int userId, string? name, string? avatarUrl)
        {
            var membership = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);
            if (membership is null)
            {
                throw new UnauthorizedAccessException("You are not a member of this conversation.");
            }

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conversation is null)
            {
                throw new InvalidOperationException("Conversation not found.");
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                conversation.Name = name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                conversation.AvatarUrl = avatarUrl.Trim();
            }

            await _context.SaveChangesAsync();
            var updated = await GetConversationDetailAsync(conversationId, userId);
            return updated!;
        }

        public async Task<ConversationDetailDto> AddMembersAsync(int conversationId, int userId, List<int> memberUserIds)
        {
            var membership = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);
            if (membership is null)
            {
                throw new UnauthorizedAccessException("You are not a member of this conversation.");
            }

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conversation is null)
            {
                throw new InvalidOperationException("Conversation not found.");
            }

            if (!string.Equals(conversation.Type, "group", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only group conversation can add new members.");
            }

            var normalizedIds = (memberUserIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (normalizedIds.Count == 0)
            {
                throw new InvalidOperationException("No member to add.");
            }

            var existingUserIds = await _context.Users
                .Where(u => normalizedIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();
            if (existingUserIds.Count != normalizedIds.Count)
            {
                throw new InvalidOperationException("One or more users do not exist.");
            }

            var existingMemberIds = await _context.ConversationMembers
                .Where(cm => cm.ConversationId == conversationId)
                .Select(cm => cm.UserId)
                .ToListAsync();

            var newMemberIds = normalizedIds
                .Where(id => !existingMemberIds.Contains(id))
                .ToList();

            if (newMemberIds.Count > 0)
            {
                var now = DateTime.UtcNow;
                var newMembers = newMemberIds.Select(id => new ConversationMember
                {
                    ConversationId = conversationId,
                    UserId = id,
                    LastReadAt = now
                });

                await _context.ConversationMembers.AddRangeAsync(newMembers);
                await _context.SaveChangesAsync();
            }

            var updated = await GetConversationDetailAsync(conversationId, userId);
            return updated!;
        }

        public async Task<DateTime> MarkConversationAsReadAsync(int conversationId, int userId)
        {
            var membership = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);

            if (membership is null)
            {
                throw new UnauthorizedAccessException("You are not a member of this conversation.");
            }

            var latestSentAt = await _context.Messages
                .Where(m => m.ConversationId == conversationId &&
                            !m.HiddenByUsers.Any(h => h.UserId == userId))
                .OrderByDescending(m => m.SentAt)
                .Select(m => (DateTime?)m.SentAt)
                .FirstOrDefaultAsync();

            var readAt = latestSentAt ?? DateTime.UtcNow;
            membership.LastReadAt = readAt;
            await _context.SaveChangesAsync();
            return readAt;
        }
    }
}
