namespace EVWarrantyManagement.BLL.Interfaces
{
    /// <summary>
    /// Service for sending real-time notifications via SignalR
    /// Handles claim status updates, user activities, and system notifications
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Notify users when a claim status changes
        /// </summary>
        Task NotifyClaimStatusChangedAsync(int claimId, string newStatus, string? oldStatus = null, string? note = null);

        /// <summary>
        /// Notify EVM staff when a new claim is created
        /// </summary>
        Task NotifyNewClaimCreatedAsync(int claimId, string vin, string serviceCenterName);

        /// <summary>
        /// Notify relevant users when a part is added to a claim
        /// </summary>
        Task NotifyPartAddedAsync(int claimId, string partName, decimal cost, int? technicianId = null);

        /// <summary>
        /// Notify technician when a claim is assigned to them
        /// </summary>
        Task NotifyClaimAssignedAsync(int claimId, int technicianId, string technicianName);

        /// <summary>
        /// Notify users in a claim group about an update
        /// </summary>
        Task NotifyClaimUpdateAsync(int claimId, string message, string type = "info");

        /// <summary>
        /// Send notification to specific user
        /// </summary>
        Task SendToUserAsync(string userId, string message, string type = "info");

        /// <summary>
        /// Send notification to all users with a specific role
        /// </summary>
        Task SendToRoleAsync(string role, string message, string type = "info");

        /// <summary>
        /// Broadcast notification to all connected users (Admin only)
        /// </summary>
        Task BroadcastNotificationAsync(string message, string type = "info");
    }
}
