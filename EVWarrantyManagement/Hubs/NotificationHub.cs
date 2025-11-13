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

            // Get all user's roles from claims
            var userRoles = Context.User?.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList() ?? new List<string>();

            if (!string.IsNullOrEmpty(userId) && userRoles.Any())
            {
                var primaryRole = userRoles.First();
                
                // Store connection info
                _userConnections[connectionId] = new UserConnectionInfo
                {
                    UserId = userId,
                    ConnectionId = connectionId,
                    Role = primaryRole
                };

                // Add to all role-based groups (users can have multiple roles)
                foreach (var role in userRoles)
                {
                    await Groups.AddToGroupAsync(connectionId, role);
                }

                // Notify others that user is online (optional)
                await Clients.Others.SendAsync("UserConnected", userId, primaryRole);
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
        /// Join a booking-specific group to receive updates about a particular booking
        /// Used when user opens a booking details page
        /// </summary>
        /// <param name="bookingId">The booking ID to join</param>
        public async Task JoinBookingGroup(int bookingId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Booking_{bookingId}");
        }

        /// <summary>
        /// Leave a booking-specific group
        /// Used when user leaves a booking details page
        /// </summary>
        /// <param name="bookingId">The booking ID to leave</param>
        public async Task LeaveBookingGroup(int bookingId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Booking_{bookingId}");
        }

        /// <summary>
        /// Join a part-specific group to receive updates about a particular part
        /// Used when user opens a part details page
        /// </summary>
        /// <param name="partId">The part ID to join</param>
        public async Task JoinPartGroup(int partId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Part_{partId}");
        }

        /// <summary>
        /// Leave a part-specific group
        /// Used when user leaves a part details page
        /// </summary>
        /// <param name="partId">The part ID to leave</param>
        public async Task LeavePartGroup(int partId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Part_{partId}");
        }

        /// <summary>
        /// Join an inventory-specific group to receive updates about inventory
        /// Used when user opens an inventory page
        /// </summary>
        /// <param name="partId">The part ID for inventory tracking</param>
        public async Task JoinInventoryGroup(int partId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Inventory_{partId}");
        }

        /// <summary>
        /// Leave an inventory-specific group
        /// Used when user leaves an inventory page
        /// </summary>
        /// <param name="partId">The part ID for inventory tracking</param>
        public async Task LeaveInventoryGroup(int partId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Inventory_{partId}");
        }

        /// <summary>
        /// Join a service center-specific group to receive updates about a particular service center
        /// Used when user opens a service center details page
        /// </summary>
        /// <param name="serviceCenterId">The service center ID to join</param>
        public async Task JoinServiceCenterGroup(int serviceCenterId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ServiceCenter_{serviceCenterId}");
        }

        /// <summary>
        /// Leave a service center-specific group
        /// Used when user leaves a service center details page
        /// </summary>
        /// <param name="serviceCenterId">The service center ID to leave</param>
        public async Task LeaveServiceCenterGroup(int serviceCenterId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ServiceCenter_{serviceCenterId}");
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
        /// Send booking update notification to all users watching a booking
        /// </summary>
        public async Task SendBookingUpdate(int bookingId, object bookingData)
        {
            await Clients.Group($"Booking_{bookingId}").SendAsync("ReceiveBookingUpdate", bookingData);
        }

        /// <summary>
        /// Send part update notification to all users watching a part
        /// </summary>
        public async Task SendPartUpdate(int partId, object partData)
        {
            await Clients.Group($"Part_{partId}").SendAsync("ReceivePartUpdate", partData);
        }

        /// <summary>
        /// Send inventory update notification to all users watching inventory
        /// </summary>
        public async Task SendInventoryUpdate(int partId, object inventoryData)
        {
            await Clients.Group($"Inventory_{partId}").SendAsync("ReceiveInventoryUpdate", inventoryData);
        }

        /// <summary>
        /// Send service center update notification to all users watching a service center
        /// </summary>
        public async Task SendServiceCenterUpdate(int serviceCenterId, object serviceCenterData)
        {
            await Clients.Group($"ServiceCenter_{serviceCenterId}").SendAsync("ReceiveServiceCenterUpdate", serviceCenterData);
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

