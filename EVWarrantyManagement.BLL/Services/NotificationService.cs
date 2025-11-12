using EVWarrantyManagement.BLL.Interfaces;

namespace EVWarrantyManagement.BLL.Services
{
    /// <summary>
    /// Service for sending real-time notifications via SignalR
    /// Note: This is a stub service. Actual SignalR notifications should be
    /// triggered from the UI layer using IHubContext directly to avoid circular dependencies.
    /// The UI layer should inject IHubContext and call hub methods directly.
    /// </summary>
    public class NotificationService : INotificationService
    {
        public NotificationService()
        {
        }

        /// <summary>
        /// Notify users when a claim status changes
        /// Note: Implement in UI layer with IHubContext injection
        /// </summary>
        public Task NotifyClaimStatusChangedAsync(int claimId, string newStatus, string? oldStatus = null, string? note = null)
        {
            // Implemented at UI layer
            return Task.CompletedTask;
        }

        /// <summary>
        /// Notify EVM staff when a new claim is created
        /// Note: Implement in UI layer with IHubContext injection
        /// </summary>
        public Task NotifyNewClaimCreatedAsync(int claimId, string vin, string serviceCenterName)
        {
            // Implemented at UI layer
            return Task.CompletedTask;
        }

        /// <summary>
        /// Notify relevant users when a part is added to a claim
        /// Note: Implement in UI layer with IHubContext injection
        /// </summary>
        public Task NotifyPartAddedAsync(int claimId, string partName, decimal cost, int? technicianId = null)
        {
            // Implemented at UI layer
            return Task.CompletedTask;
        }

        /// <summary>
        /// Notify technician when a claim is assigned to them
        /// Note: Implement in UI layer with IHubContext injection
        /// </summary>
        public Task NotifyClaimAssignedAsync(int claimId, int technicianId, string technicianName)
        {
            // Implemented at UI layer
            return Task.CompletedTask;
        }

        /// <summary>
        /// Notify users in a claim group about an update
        /// Note: Implement in UI layer with IHubContext injection
        /// </summary>
        public Task NotifyClaimUpdateAsync(int claimId, string message, string type = "info")
        {
            // Implemented at UI layer
            return Task.CompletedTask;
        }

        /// <summary>
        /// Send notification to specific user
        /// Note: Implement in UI layer with IHubContext injection
        /// </summary>
        public Task SendToUserAsync(string userId, string message, string type = "info")
        {
            // Implemented at UI layer
            return Task.CompletedTask;
        }

        /// <summary>
        /// Send notification to all users with a specific role
        /// Note: Implement in UI layer with IHubContext injection
        /// </summary>
        public Task SendToRoleAsync(string role, string message, string type = "info")
        {
            // Implemented at UI layer
            return Task.CompletedTask;
        }

        /// <summary>
        /// Broadcast notification to all connected users
        /// Note: Implement in UI layer with IHubContext injection
        /// </summary>
        public Task BroadcastNotificationAsync(string message, string type = "info")
        {
            // Implemented at UI layer
            return Task.CompletedTask;
        }
    }
}
