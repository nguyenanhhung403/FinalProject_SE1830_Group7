using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.DAL.Interfaces
{
    /// <summary>
    /// Repository interface for managing claim messages
    /// </summary>
    public interface IMessageRepository
    {
        /// <summary>
        /// Get all messages for a specific claim
        /// </summary>
        Task<List<ClaimMessage>> GetMessagesByClaimIdAsync(int claimId);

        /// <summary>
        /// Get a message by ID
        /// </summary>
        Task<ClaimMessage?> GetMessageByIdAsync(int messageId);

        /// <summary>
        /// Create a new message
        /// </summary>
        Task<ClaimMessage> CreateMessageAsync(ClaimMessage message);

        /// <summary>
        /// Mark a message as read
        /// </summary>
        Task MarkMessageAsReadAsync(int messageId);

        /// <summary>
        /// Mark all messages in a claim as read for a specific user
        /// </summary>
        Task MarkClaimMessagesAsReadAsync(int claimId, int userId);

        /// <summary>
        /// Get unread message count for a user across all claims
        /// </summary>
        Task<int> GetUnreadMessageCountAsync(int userId);

        /// <summary>
        /// Get unread message count for a specific claim
        /// </summary>
        Task<int> GetUnreadMessageCountByClaimAsync(int claimId, int userId);

        /// <summary>
        /// Delete a message
        /// </summary>
        Task DeleteMessageAsync(int messageId);

        /// <summary>
        /// Get recent messages (last N messages) for a claim
        /// </summary>
        Task<List<ClaimMessage>> GetRecentMessagesAsync(int claimId, int count = 50);
    }
}
