using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;

namespace EVWarrantyManagement.BLL.Services
{
    /// <summary>
    /// Service for managing claim messages
    /// Note: SignalR broadcasting should be done in the UI layer with IHubContext
    /// </summary>
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;

        public MessageService(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        /// <summary>
        /// Get all messages for a specific claim
        /// </summary>
        public async Task<List<ClaimMessage>> GetMessagesByClaimIdAsync(int claimId)
        {
            return await _messageRepository.GetMessagesByClaimIdAsync(claimId);
        }

        /// <summary>
        /// Send a new message (without SignalR broadcasting - handle that in UI layer)
        /// </summary>
        public async Task<ClaimMessage> SendMessageAsync(int claimId, int userId, string message)
        {
            var claimMessage = new ClaimMessage
            {
                ClaimId = claimId,
                UserId = userId,
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            var savedMessage = await _messageRepository.CreateMessageAsync(claimMessage);

            // Note: SignalR broadcasting should be done in UI layer after calling this method

            return savedMessage;
        }

        /// <summary>
        /// Mark a message as read
        /// </summary>
        public async Task MarkMessageAsReadAsync(int messageId)
        {
            await _messageRepository.MarkMessageAsReadAsync(messageId);
        }

        /// <summary>
        /// Mark all messages in a claim as read for the current user
        /// </summary>
        public async Task MarkClaimMessagesAsReadAsync(int claimId, int userId)
        {
            await _messageRepository.MarkClaimMessagesAsReadAsync(claimId, userId);

            // Note: SignalR notifications should be handled in UI layer
        }

        /// <summary>
        /// Get unread message count for a user
        /// </summary>
        public async Task<int> GetUnreadMessageCountAsync(int userId)
        {
            return await _messageRepository.GetUnreadMessageCountAsync(userId);
        }

        /// <summary>
        /// Get unread message count for a specific claim
        /// </summary>
        public async Task<int> GetUnreadMessageCountByClaimAsync(int claimId, int userId)
        {
            return await _messageRepository.GetUnreadMessageCountByClaimAsync(claimId, userId);
        }

        /// <summary>
        /// Delete a message (with authorization check in the calling layer)
        /// </summary>
        public async Task DeleteMessageAsync(int messageId, int userId)
        {
            var message = await _messageRepository.GetMessageByIdAsync(messageId);

            if (message != null && message.UserId == userId)
            {
                await _messageRepository.DeleteMessageAsync(messageId);

                // Note: SignalR notifications should be handled in UI layer
            }
        }

        /// <summary>
        /// Get recent messages for a claim
        /// </summary>
        public async Task<List<ClaimMessage>> GetRecentMessagesAsync(int claimId, int count = 50)
        {
            return await _messageRepository.GetRecentMessagesAsync(claimId, count);
        }
    }
}
