using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace EVWarrantyManagement.Hubs
{
    /// <summary>
    /// SignalR Hub for handling real-time notifications across the application
    /// Manages user connections, role-based groups, and notification broadcasting
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        // Thread-safe dictionary to track user connections
        private static readonly ConcurrentDictionary<string, UserConnectionInfo> _userConnections = new();

        /// <summary>
        /// Called when a new connection is established
        /// Adds user to role-based groups for targeted notifications
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier; // Gets UserId from authenticated user
            var connectionId = Context.ConnectionId;

            // Get user's role from claims
            var userRole = Context.User?.Claims
                .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userRole))
            {
                // Store connection info
                _userConnections[connectionId] = new UserConnectionInfo
                {
                    UserId = userId,
                    ConnectionId = connectionId,
                    Role = userRole
                };

                // Add to role-based group
                await Groups.AddToGroupAsync(connectionId, userRole);

                // Notify others that user is online (optional)
                await Clients.Others.SendAsync("UserConnected", userId, userRole);
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a connection is terminated
        /// Cleans up user connection tracking
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            if (_userConnections.TryRemove(connectionId, out var userInfo))
            {
                // Notify others that user is offline (optional)
                await Clients.Others.SendAsync("UserDisconnected", userInfo.UserId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a claim-specific group to receive updates about a particular claim
        /// Used when user opens a claim details page
        /// </summary>
        /// <param name="claimId">The claim ID to join</param>
        public async Task JoinClaimGroup(int claimId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Claim_{claimId}");
        }

        /// <summary>
        /// Leave a claim-specific group
        /// Used when user leaves a claim details page
        /// </summary>
        /// <param name="claimId">The claim ID to leave</param>
        public async Task LeaveClaimGroup(int claimId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Claim_{claimId}");
        }

        /// <summary>
        /// Get all online users (Admin only)
        /// </summary>
        [Authorize(Policy = "RequireAdmin")]
        public List<UserConnectionInfo> GetOnlineUsers()
        {
            return _userConnections.Values.ToList();
        }

        /// <summary>
        /// Send notification to specific user
        /// </summary>
        public async Task SendToUser(string userId, string message, string type = "info")
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message, type);
        }

        /// <summary>
        /// Send notification to all users in a role
        /// </summary>
        public async Task SendToRole(string role, string message, string type = "info")
        {
            await Clients.Group(role).SendAsync("ReceiveNotification", message, type);
        }

        /// <summary>
        /// Send claim update notification to all users watching a claim
        /// </summary>
        public async Task SendClaimUpdate(int claimId, object claimData)
        {
            await Clients.Group($"Claim_{claimId}").SendAsync("ReceiveClaimUpdate", claimData);
        }

        /// <summary>
        /// Broadcast notification to all connected users
        /// </summary>
        [Authorize(Policy = "RequireAdmin")]
        public async Task BroadcastNotification(string message, string type = "info")
        {
            await Clients.All.SendAsync("ReceiveNotification", message, type);
        }
    }

    /// <summary>
    /// Information about a connected user
    /// </summary>
    public class UserConnectionInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    }
}

