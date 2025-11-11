/**
 * Claims Page Real-Time Updates via SignalR
 * Handles live claim updates on claims listing and details pages
 */

class ClaimsRealTime {
    constructor() {
        this.notificationHub = null;
        this.isInitialized = false;
        this.currentClaimId = null;
    }

    /**
     * Initialize the claims real-time connection
     * @param {number} claimId - Optional claim ID if on details page
     */
    async initialize(claimId = null) {
        if (this.isInitialized) {
            console.warn('Claims real-time already initialized');
            return;
        }

        this.currentClaimId = claimId;

        try {
            // Create SignalR connection to notification hub
            this.notificationHub = new SignalRConnection('/hubs/notification');

            // Register event handlers
            this.registerEventHandlers();

            // Start the connection
            await this.notificationHub.start();

            // If on a claim details page, join the claim group
            if (this.currentClaimId) {
                await this.joinClaimGroup(this.currentClaimId);
            }

            this.isInitialized = true;
            console.log('Claims real-time initialized');

        } catch (error) {
            console.error('Failed to initialize claims real-time:', error);
        }
    }

    /**
     * Register SignalR event handlers
     */
    registerEventHandlers() {
        // Handle general notifications
        this.notificationHub.on('ReceiveNotification', (message, type) => {
            this.handleNotification(message, type);
        });

        // Handle claim updates
        this.notificationHub.on('ReceiveClaimUpdate', (data) => {
            this.handleClaimUpdate(data);
        });

        // Handle new claims
        this.notificationHub.on('ReceiveNewClaim', (data) => {
            this.handleNewClaim(data);
        });
    }

    /**
     * Join a claim-specific group for targeted updates
     */
    async joinClaimGroup(claimId) {
        if (!this.notificationHub) return;

        try {
            await this.notificationHub.invoke('JoinClaimGroup', claimId);
            console.log(`Joined claim group: ${claimId}`);
        } catch (error) {
            console.error(`Failed to join claim group ${claimId}:`, error);
        }
    }

    /**
     * Leave a claim-specific group
     */
    async leaveClaimGroup(claimId) {
        if (!this.notificationHub) return;

        try {
            await this.notificationHub.invoke('LeaveClaimGroup', claimId);
            console.log(`Left claim group: ${claimId}`);
        } catch (error) {
            console.error(`Failed to leave claim group ${claimId}:`, error);
        }
    }

    /**
     * Handle general notification
     */
    handleNotification(message, type = 'info') {
        if (window.notificationToast) {
            window.notificationToast.show(message, type);
        }
        console.log(`[${type.toUpperCase()}] ${message}`);
    }

    /**
     * Handle claim update
     */
    handleClaimUpdate(data) {
        console.log('Claim update received:', data);

        // If we're on the claim details page for this claim, update the UI
        if (this.currentClaimId && this.currentClaimId === data.ClaimId) {
            this.updateClaimDetailsPage(data);
        }

        // If we're on the claims list, update the table row
        this.updateClaimInTable(data);

        // Show notification
        let notificationType = 'info';
        if (data.Type === 'status_change') {
            if (data.NewStatus === 'Approved' || data.NewStatus === 'Completed') {
                notificationType = 'success';
            } else if (data.NewStatus === 'Rejected') {
                notificationType = 'error';
            } else if (data.NewStatus === 'OnHold') {
                notificationType = 'warning';
            }
        } else if (data.Type === 'part_added') {
            notificationType = 'success';
        }

        this.handleNotification(data.Message, notificationType);
    }

    /**
     * Handle new claim
     */
    handleNewClaim(data) {
        console.log('New claim received:', data);

        // Add to the claims table if it exists
        this.addClaimToTable(data);

        // Show notification
        this.handleNotification(data.Message, 'info');
    }

    /**
     * Update claim details page UI
     */
    updateClaimDetailsPage(data) {
        // Update status badge
        if (data.NewStatus) {
            const statusBadge = document.querySelector('.claim-status-badge');
            if (statusBadge) {
                statusBadge.textContent = data.NewStatus;
                statusBadge.className = `badge claim-status-badge bg-${this.getStatusColor(data.NewStatus)} badge-pulse`;
                setTimeout(() => statusBadge.classList.remove('badge-pulse'), 1000);
            }

            // Update status field
            const statusField = document.getElementById('claim-status');
            if (statusField) {
                statusField.textContent = data.NewStatus;
            }
        }

        // Update total cost if changed
        if (data.TotalCost !== undefined) {
            const costElement = document.getElementById('claim-total-cost');
            if (costElement) {
                costElement.textContent = `$${data.TotalCost.toFixed(2)}`;
                costElement.classList.add('text-highlight');
                setTimeout(() => costElement.classList.remove('text-highlight'), 1500);
            }
        }

        // If a part was added, reload the parts table
        if (data.Type === 'part_added') {
            const partsTable = document.getElementById('used-parts-table');
            if (partsTable && typeof reloadUsedParts === 'function') {
                reloadUsedParts();
            }
        }

        // Add to activity log
        if (data.Message) {
            this.addToActivityLog(data);
        }
    }

    /**
     * Update claim in table (on claims list page)
     */
    updateClaimInTable(data) {
        const claimRow = document.querySelector(`tr[data-claim-id="${data.ClaimId}"]`);
        if (!claimRow) {
            console.log(`Claim row not found for ClaimId: ${data.ClaimId}`);
            return;
        }

        console.log('Updating claim in table:', data);

        // Update status - the status is in the 6th column (index 5)
        if (data.NewStatus) {
            const statusCell = claimRow.cells[5]; // Status column
            if (statusCell) {
                statusCell.innerHTML = `<span class="badge bg-${this.getStatusColor(data.NewStatus)} badge-pulse">${data.NewStatus}</span>`;
                setTimeout(() => {
                    const badge = statusCell.querySelector('.badge');
                    if (badge) badge.classList.remove('badge-pulse');
                }, 1000);
            }
        }

        // Highlight row to show it was updated
        claimRow.classList.add('table-row-highlight');
        setTimeout(() => claimRow.classList.remove('table-row-highlight'), 2000);
    }

    /**
     * Add new claim to table
     */
    addClaimToTable(data) {
        const claimsTable = document.querySelector('#claims-table tbody');
        if (!claimsTable) {
            console.log('Claims table not found, cannot add new claim to table');
            return;
        }

        console.log('Adding new claim to table:', data);

        // NOTE: ASP.NET Core sends property names in camelCase (lowercase first letter)
        // Support both PascalCase and camelCase for compatibility

        // Create new row matching the actual table structure:
        // ClaimID, Model, VIN, Description, Date, Status, Actions
        const row = document.createElement('tr');
        row.setAttribute('data-claim-id', data.claimId || data.ClaimId);
        row.className = 'new-row-animation';

        // Truncate description if too long
        const desc = data.description || data.Description || '';
        const description = desc.length > 50 ? desc.substring(0, 50) + '...' : (desc || 'N/A');

        row.innerHTML = `
            <td>${data.claimId || data.ClaimId}</td>
            <td>${data.vehicleModel || data.VehicleModel || 'N/A'}</td>
            <td>${data.vin || data.Vin}</td>
            <td class="text-truncate" style="max-width: 360px;">${description}</td>
            <td>${data.dateDiscovered || data.DateDiscovered || new Date().toLocaleDateString()}</td>
            <td><span class="badge bg-secondary">${data.statusCode || data.StatusCode || 'Pending'}</span></td>
            <td class="text-nowrap">
                <a class="btn btn-sm btn-secondary" href="/Claims/Details?id=${data.claimId || data.ClaimId}">Details</a>
            </td>
        `;

        // Add to top of table
        claimsTable.prepend(row);

        // Remove animation class after animation completes
        setTimeout(() => row.classList.remove('new-row-animation'), 500);
    }

    /**
     * Add entry to activity log
     */
    addToActivityLog(data) {
        const activityLog = document.getElementById('activity-log');
        if (!activityLog) return;

        const logEntry = document.createElement('div');
        logEntry.className = 'alert alert-info alert-dismissible fade show new-activity-animation';
        logEntry.innerHTML = `
            <i class="bi bi-info-circle me-2"></i>
            <strong>${data.Type || 'Update'}:</strong> ${data.Message}
            <small class="text-muted d-block mt-1">Just now</small>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        activityLog.prepend(logEntry);

        // Remove animation class
        setTimeout(() => logEntry.classList.remove('new-activity-animation'), 500);

        // Auto-dismiss after 10 seconds
        setTimeout(() => {
            if (logEntry.parentNode) {
                const bsAlert = new bootstrap.Alert(logEntry);
                bsAlert.close();
            }
        }, 10000);
    }

    /**
     * Get Bootstrap color class for status
     */
    getStatusColor(status) {
        const colorMap = {
            'Pending': 'warning',
            'Approved': 'success',
            'Rejected': 'danger',
            'OnHold': 'secondary',
            'InProgress': 'info',
            'Completed': 'success',
            'Closed': 'dark'
        };
        return colorMap[status] || 'secondary';
    }

    /**
     * Stop the real-time connection
     */
    async stop() {
        // Leave claim group if we're on a details page
        if (this.currentClaimId && this.notificationHub) {
            await this.leaveClaimGroup(this.currentClaimId);
        }

        if (this.notificationHub) {
            await this.notificationHub.stop();
            this.isInitialized = false;
            console.log('Claims real-time stopped');
        }
    }
}

// Export for global use
window.ClaimsRealTime = ClaimsRealTime;

// Auto-initialize if on claims page
document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on a claims page
    const isClaimsPage = window.location.pathname.includes('/Claims');

    if (isClaimsPage) {
        // Check if we're on a claim details page
        const urlParams = new URLSearchParams(window.location.search);
        const claimId = urlParams.get('id');

        // Initialize claims real-time
        window.claimsRealTime = new ClaimsRealTime();
        window.claimsRealTime.initialize(claimId ? parseInt(claimId) : null);

        // Clean up on page unload
        window.addEventListener('beforeunload', function() {
            if (window.claimsRealTime) {
                window.claimsRealTime.stop();
            }
        });
    }
});
