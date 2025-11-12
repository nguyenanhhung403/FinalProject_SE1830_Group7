using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVWarrantyManagement.DAL.Repositories
{
    /// <summary>
    /// Repository for managing claim messages in the database
    /// </summary>
    public class MessageRepository : IMessageRepository
    {
        private readonly EVWarrantyManagementContext _context;

        public MessageRepository(EVWarrantyManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all messages for a specific claim, ordered by timestamp
        /// </summary>
        public async Task<List<ClaimMessage>> GetMessagesByClaimIdAsync(int claimId)
        {
            return await _context.ClaimMessages
                .AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClaimId == claimId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Get a message by ID
        /// </summary>
        public async Task<ClaimMessage?> GetMessageByIdAsync(int messageId)
        {
            return await _context.ClaimMessages
                .Include(m => m.User)
                .Include(m => m.WarrantyClaim)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);
        }

        /// <summary>
        /// Create a new message
        /// </summary>
        public async Task<ClaimMessage> CreateMessageAsync(ClaimMessage message)
        {
            message.Timestamp = DateTime.UtcNow;
            message.IsRead = false;

            _context.ClaimMessages.Add(message);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            return (await GetMessageByIdAsync(message.MessageId))!;
        }

        /// <summary>
        /// Mark a message as read
        /// </summary>
        public async Task MarkMessageAsReadAsync(int messageId)
        {
            var message = await _context.ClaimMessages.FindAsync(messageId);
            if (message != null && !message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Mark all messages in a claim as read (except those sent by the user)
        /// </summary>
        public async Task MarkClaimMessagesAsReadAsync(int claimId, int userId)
        {
            var messages = await _context.ClaimMessages
                .Where(m => m.ClaimId == claimId && m.UserId != userId && !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            if (messages.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Get unread message count for a user across all claims they're involved in
        /// </summary>
        public async Task<int> GetUnreadMessageCountAsync(int userId)
        {
            // Get all claims the user is involved in
            var userClaimIds = await _context.WarrantyClaims
                .Where(c => c.CreatedByUserId == userId || c.TechnicianId == userId)
                .Select(c => c.ClaimId)
                .ToListAsync();

            // Count unread messages in those claims (excluding messages sent by the user)
            return await _context.ClaimMessages
                .Where(m => userClaimIds.Contains(m.ClaimId) && m.UserId != userId && !m.IsRead)
                .CountAsync();
        }

        /// <summary>
        /// Get unread message count for a specific claim
        /// </summary>
        public async Task<int> GetUnreadMessageCountByClaimAsync(int claimId, int userId)
        {
            return await _context.ClaimMessages
                .Where(m => m.ClaimId == claimId && m.UserId != userId && !m.IsRead)
                .CountAsync();
        }

        /// <summary>
        /// Delete a message
        /// </summary>
        public async Task DeleteMessageAsync(int messageId)
        {
            var message = await _context.ClaimMessages.FindAsync(messageId);
            if (message != null)
            {
                _context.ClaimMessages.Remove(message);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Get recent messages (last N messages) for a claim
        /// </summary>
        public async Task<List<ClaimMessage>> GetRecentMessagesAsync(int claimId, int count = 50)
        {
            return await _context.ClaimMessages
                .AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClaimId == claimId)
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp) // Reverse order for display
                .ToListAsync();
        }
    }
}
