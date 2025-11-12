using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace EVWarrantyManagement.Hubs
{
    /// <summary>
    /// SignalR Hub for handling real-time chat/messaging functionality
    /// Enables communication between users regarding specific warranty claims
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        // Track users currently typing in specific claims
        private static readonly ConcurrentDictionary<string, HashSet<string>> _typingUsers = new();

        /// <summary>
        /// Called when a new connection is established
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var username = Context.User?.Claims
                .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a connection is terminated
        /// Cleans up typing indicators
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;

            // Remove from all typing indicators
            foreach (var key in _typingUsers.Keys)
            {
                if (_typingUsers.TryGetValue(key, out var users))
                {
                    users.Remove(userId ?? "");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a claim chat room
        /// </summary>
        /// <param name="claimId">The claim ID to join</param>
        public async Task JoinClaimChat(int claimId)
        {
            var username = Context.User?.Claims
                .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"ClaimChat_{claimId}");

            // Notify others in the room
            await Clients.OthersInGroup($"ClaimChat_{claimId}")
                .SendAsync("UserJoined", username, DateTime.UtcNow);
        }

        /// <summary>
        /// Leave a claim chat room
        /// </summary>
        /// <param name="claimId">The claim ID to leave</param>
        public async Task LeaveClaimChat(int claimId)
        {
            var username = Context.User?.Claims
                .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ClaimChat_{claimId}");

            // Notify others in the room
            await Clients.OthersInGroup($"ClaimChat_{claimId}")
                .SendAsync("UserLeft", username, DateTime.UtcNow);

            // Remove from typing users
            var key = $"ClaimChat_{claimId}";
            if (_typingUsers.TryGetValue(key, out var users))
            {
                users.Remove(Context.UserIdentifier ?? "");
            }
        }

        /// <summary>
        /// Send a message to a claim chat room
        /// This is called from client, then service will persist to DB and broadcast
        /// </summary>
        /// <param name="claimId">The claim ID</param>
        /// <param name="message">The message content</param>
        public async Task SendMessage(int claimId, string message)
        {
            var userId = Context.UserIdentifier;
            var username = Context.User?.Claims
                .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;

            var messageData = new
            {
                ClaimId = claimId,
                UserId = userId,
                Username = username,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            // Broadcast to all users in the claim chat room
            await Clients.Group($"ClaimChat_{claimId}")
                .SendAsync("ReceiveMessage", messageData);

            // Stop typing indicator for this user
            await UserStoppedTyping(claimId);
        }

        /// <summary>
        /// Notify others that user is typing
        /// </summary>
        /// <param name="claimId">The claim ID</param>
        public async Task UserTyping(int claimId)
        {
            var userId = Context.UserIdentifier;
            var username = Context.User?.Claims
                .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId)) return;

            var key = $"ClaimChat_{claimId}";
            var users = _typingUsers.GetOrAdd(key, _ => new HashSet<string>());

            lock (users)
            {
                users.Add(userId);
            }

            // Notify others in the room
            await Clients.OthersInGroup($"ClaimChat_{claimId}")
                .SendAsync("UserIsTyping", username);
        }

        /// <summary>
        /// Notify others that user stopped typing
        /// </summary>
        /// <param name="claimId">The claim ID</param>
        public async Task UserStoppedTyping(int claimId)
        {
            var userId = Context.UserIdentifier;
            var username = Context.User?.Claims
                .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId)) return;

            var key = $"ClaimChat_{claimId}";
            if (_typingUsers.TryGetValue(key, out var users))
            {
                lock (users)
                {
                    users.Remove(userId);
                }
            }

            // Notify others in the room
            await Clients.OthersInGroup($"ClaimChat_{claimId}")
                .SendAsync("UserStoppedTyping", username);
        }

        /// <summary>
        /// Mark messages as read by the current user
        /// </summary>
        /// <param name="claimId">The claim ID</param>
        public async Task MarkMessagesAsRead(int claimId)
        {
            var userId = Context.UserIdentifier;

            // This would typically call a service method to update the database
            // For now, we'll just notify the group
            await Clients.Group($"ClaimChat_{claimId}")
                .SendAsync("MessagesRead", userId, DateTime.UtcNow);
        }

        /// <summary>
        /// Get the list of users currently in a claim chat
        /// </summary>
        /// <param name="claimId">The claim ID</param>
        public async Task GetActiveUsers(int claimId)
        {
            // This would query the connection tracking
            // For now, just acknowledge the request
            await Task.CompletedTask;
        }
    }
}

