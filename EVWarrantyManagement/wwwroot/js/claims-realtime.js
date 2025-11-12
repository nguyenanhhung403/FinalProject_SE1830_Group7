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
        this.showToast(message, type);
        console.log(`[${type.toUpperCase()}] ${message}`);
    }

    /**
     * Show toast notification
     */
    showToast(message, type = 'info') {
        const toastContainer = document.getElementById('toast-container') || this.createToastContainer();
        
        const toastId = 'toast-' + Date.now();
        const bgClass = type === 'success' ? 'bg-success' : 
                       type === 'error' || type === 'danger' ? 'bg-danger' : 
                       type === 'warning' ? 'bg-warning' : 'bg-info';
        
        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">${message}</div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;
        
        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement, { delay: 5000 });
        toast.show();
        
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });
    }

    /**
     * Create toast container if it doesn't exist
     */
    createToastContainer() {
        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '9999';
        document.body.appendChild(container);
        return container;
    }

    /**
     * Handle claim update
     */
    handleClaimUpdate(data) {
        console.log('Claim update received:', data);

        // If we're on the claim details page for this claim, update the UI
        if (this.currentClaimId && (this.currentClaimId === data.ClaimId || this.currentClaimId === data.claimId)) {
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

        this.handleNotification(data.Message || 'Claim updated', notificationType);
    }

    /**
     * Handle new claim
     */
    handleNewClaim(data) {
        console.log('New claim received:', data);

        // Add to the claims table if it exists
        this.addClaimToTable(data);

        // Show notification
        this.handleNotification(data.Message || `New claim #${data.claimId || data.ClaimId} created`, 'info');
    }

    /**
     * Update claim details page UI
     */
    updateClaimDetailsPage(data) {
        // Update status badge
        if (data.NewStatus) {
            const statusBadge = document.querySelector('.claim-status-badge, .badge.status-pending, .badge.status-approved');
            if (statusBadge) {
                statusBadge.textContent = data.NewStatus;
                statusBadge.className = `badge status-${data.NewStatus.toLowerCase().replace(' ', '')} badge-pulse`;
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
            // Reload page to show new part
            setTimeout(() => {
                window.location.reload();
            }, 1000);
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
        const claimId = data.ClaimId || data.claimId;
        const claimRow = document.querySelector(`tr[data-claim-id="${claimId}"]`);
        if (!claimRow) {
            console.log(`Claim row not found for ClaimId: ${claimId}`);
            return;
        }

        console.log('Updating claim in table:', data);

        // Update status - find status column
        if (data.NewStatus) {
            const statusCell = claimRow.querySelector('td:has(.badge)') || claimRow.cells[5];
            if (statusCell) {
                statusCell.innerHTML = `<span class="badge status-${data.NewStatus.toLowerCase().replace(' ', '')} badge-pulse">${data.NewStatus}</span>`;
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
        const claimsTable = document.querySelector('#claims-table tbody, .table tbody');
        if (!claimsTable) {
            console.log('Claims table not found, cannot add new claim to table');
            return;
        }

        console.log('Adding new claim to table:', data);

        // Create new row matching the actual table structure
        const row = document.createElement('tr');
        row.setAttribute('data-claim-id', data.claimId || data.ClaimId);
        row.className = 'new-row-animation';

        // Truncate description if too long
        const desc = data.description || data.Description || '';
        const description = desc.length > 50 ? desc.substring(0, 50) + '...' : (desc || 'N/A');

        row.innerHTML = `
            <td><strong>#${data.claimId || data.ClaimId}</strong></td>
            <td><i class="bi bi-car-front text-primary me-1"></i>${data.vehicleModel || data.VehicleModel || 'N/A'}</td>
            <td><code>${data.vin || data.Vin}</code></td>
            <td class="text-truncate" style="max-width: 360px;">${description}</td>
            <td><i class="bi bi-calendar3 me-1 text-muted"></i>${new Date().toLocaleDateString()}</td>
            <td><span class="badge status-pending">${data.statusCode || data.StatusCode || 'Pending'}</span></td>
            <td class="text-nowrap">
                <a class="btn btn-sm btn-outline-primary" href="/Claims/Details?id=${data.claimId || data.ClaimId}">
                    <i class="bi bi-eye me-1"></i>Details
                </a>
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

