using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.BLL.Interfaces
{
    /// <summary>
    /// Service interface for managing claim messages and chat functionality
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// Get all messages for a specific claim with user details
        /// </summary>
        Task<List<ClaimMessage>> GetMessagesByClaimIdAsync(int claimId);

        /// <summary>
        /// Send a new message to a claim chat
        /// </summary>
        Task<ClaimMessage> SendMessageAsync(int claimId, int userId, string message);

        /// <summary>
        /// Mark a message as read
        /// </summary>
        Task MarkMessageAsReadAsync(int messageId);

        /// <summary>
        /// Mark all messages in a claim as read for current user
        /// </summary>
        Task MarkClaimMessagesAsReadAsync(int claimId, int userId);

        /// <summary>
        /// Get unread message count for a user
        /// </summary>
        Task<int> GetUnreadMessageCountAsync(int userId);

        /// <summary>
        /// Get unread message count for a specific claim
        /// </summary>
        Task<int> GetUnreadMessageCountByClaimAsync(int claimId, int userId);

        /// <summary>
        /// Delete a message (soft delete or hard delete based on business rules)
        /// </summary>
        Task DeleteMessageAsync(int messageId, int userId);

        /// <summary>
        /// Get recent messages for a claim
        /// </summary>
        Task<List<ClaimMessage>> GetRecentMessagesAsync(int claimId, int count = 50);
    }
}
