/**
 * Dashboard Real-Time Updates via SignalR
 * Handles live notifications and dashboard data updates
 */

class DashboardRealTime {
    constructor() {
        this.notificationHub = null;
        this.isInitialized = false;
    }

    /**
     * Initialize the dashboard real-time connection
     */
    async initialize() {
        if (this.isInitialized) {
            console.warn('Dashboard real-time already initialized');
            return;
        }

        try {
            // Create SignalR connection to notification hub
            this.notificationHub = new SignalRConnection('/hubs/notification');

            // Register event handlers
            this.registerEventHandlers();

            // Start the connection
            await this.notificationHub.start();

            this.isInitialized = true;
            console.log('Dashboard real-time initialized');

            // Update connection status indicator
            this.updateConnectionStatus('connected');

        } catch (error) {
            console.error('Failed to initialize dashboard real-time:', error);
            this.updateConnectionStatus('error');
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

        // Handle new claim notifications
        this.notificationHub.on('ReceiveNewClaim', (data) => {
            this.handleNewClaim(data);
        });

        // Handle claim update notifications
        this.notificationHub.on('ReceiveClaimUpdate', (data) => {
            this.handleClaimUpdate(data);
        });

        // Handle user connection events
        this.notificationHub.on('UserConnected', (userId, role) => {
            console.log(`User ${userId} (${role}) connected`);
        });

        this.notificationHub.on('UserDisconnected', (userId) => {
            console.log(`User ${userId} disconnected`);
        });

        // Override connection state change handler
        this.notificationHub.onConnectionStateChanged = (state) => {
            this.updateConnectionStatus(state);
        };
    }

    /**
     * Handle general notification
     */
    handleNotification(message, type = 'info') {
        // Show toast notification
        if (window.notificationToast) {
            window.notificationToast.show(message, type);
        }

        // Log to console
        console.log(`[${type.toUpperCase()}] ${message}`);
    }

    /**
     * Handle new claim notification
     */
    handleNewClaim(data) {
        console.log('New claim received:', data);

        // Show notification
        this.handleNotification(data.Message, 'info');

        // Increment pending claims counter
        const pendingCounter = document.getElementById('pending-claims-count');
        if (pendingCounter) {
            const currentCount = parseInt(pendingCounter.textContent) || 0;
            pendingCounter.textContent = currentCount + 1;

            // Add animation
            pendingCounter.classList.add('badge-pulse');
            setTimeout(() => pendingCounter.classList.remove('badge-pulse'), 1000);
        }

        // Add claim to dashboard table
        this.addClaimToDashboardTable(data);

        // If we're on the dashboard, reload the claims chart/table
        if (typeof refreshDashboardData === 'function') {
            refreshDashboardData();
        }

        // Add to recent claims list if exists
        this.addClaimToRecentList(data);
    }

    /**
     * Handle claim update notification
     */
    handleClaimUpdate(data) {
        console.log('Claim update received:', data);

        // Update claim in the UI if it's visible
        this.updateClaimInUI(data);

        // Show notification based on type
        let notificationType = 'info';
        if (data.Type === 'status_change') {
            if (data.NewStatus === 'Approved' || data.NewStatus === 'Completed') {
                notificationType = 'success';
            } else if (data.NewStatus === 'Rejected') {
                notificationType = 'error';
            } else if (data.NewStatus === 'OnHold') {
                notificationType = 'warning';
            }
        }

        this.handleNotification(data.Message, notificationType);

        // Refresh dashboard statistics
        if (typeof refreshDashboardStats === 'function') {
            refreshDashboardStats();
        }
    }

    /**
     * Add new claim to dashboard table
     */
    addClaimToDashboardTable(data) {
        const dashboardTable = document.querySelector('#dashboard-claims-table tbody');
        if (!dashboardTable) {
            console.log('Dashboard table not found, cannot add new claim');
            return;
        }

        console.log('Adding new claim to dashboard table:', data);

        // NOTE: ASP.NET Core sends property names in camelCase (lowercase first letter)
        // So we need to use: claimId, vin, vehicleModel, serviceCenterName, statusCode, etc.

        // Create new row matching dashboard structure:
        // ClaimID, Model, VIN, Status, Service Center, Created, Actions
        const row = document.createElement('tr');
        row.setAttribute('data-claim-id', data.claimId || data.ClaimId);
        row.className = 'new-row-animation';

        row.innerHTML = `
            <td>${data.claimId || data.ClaimId}</td>
            <td>${data.vehicleModel || data.VehicleModel || 'N/A'}</td>
            <td>${data.vin || data.Vin}</td>
            <td><span class="badge bg-${this.getStatusColor(data.statusCode || data.StatusCode || 'Pending')}">${data.statusCode || data.StatusCode || 'Pending'}</span></td>
            <td>${data.serviceCenterName || data.ServiceCenterName || 'Unknown'}</td>
            <td>Just now</td>
            <td class="text-nowrap">
                <a class="btn btn-sm btn-outline-primary" href="/Claims/Detail?id=${data.claimId || data.ClaimId}">Detail</a>
            </td>
        `;

        // Add to top of table
        dashboardTable.prepend(row);

        // Remove animation class after animation completes
        setTimeout(() => row.classList.remove('new-row-animation'), 500);
    }

    /**
     * Update a claim in the UI
     */
    updateClaimInUI(data) {
        // Find claim row if it exists
        const claimRow = document.querySelector(`[data-claim-id="${data.ClaimId}"]`);
        if (!claimRow) {
            console.log(`Claim row not found for ClaimId: ${data.ClaimId}`);
            return;
        }

        console.log('Updating claim in dashboard:', data);

        // Update status badge - in dashboard, status is in 4th column (index 3)
        if (data.NewStatus) {
            const statusCell = claimRow.cells[3]; // Status column
            if (statusCell) {
                statusCell.innerHTML = `<span class="badge bg-${this.getStatusColor(data.NewStatus)} badge-pulse">${data.NewStatus}</span>`;

                // Remove pulse animation after 1 second
                setTimeout(() => {
                    const badge = statusCell.querySelector('.badge');
                    if (badge) badge.classList.remove('badge-pulse');
                }, 1000);
            }
        }

        // Highlight the row briefly
        claimRow.classList.add('table-row-highlight');
        setTimeout(() => claimRow.classList.remove('table-row-highlight'), 2000);
    }

    /**
     * Add new claim to recent claims list
     */
    addClaimToRecentList(data) {
        const recentClaimsList = document.getElementById('recent-claims-list');
        if (!recentClaimsList) return;

        // Create new claim item
        const claimItem = document.createElement('div');
        claimItem.className = 'list-group-item list-group-item-action new-claim-animation';
        claimItem.innerHTML = `
            <div class="d-flex w-100 justify-content-between">
                <h6 class="mb-1">Claim #${data.ClaimId}</h6>
                <small class="text-muted">Just now</small>
            </div>
            <p class="mb-1">VIN: ${data.Vin}</p>
            <small class="text-muted">${data.ServiceCenterName}</small>
        `;

        // Add to top of list
        recentClaimsList.prepend(claimItem);

        // Remove animation class after it completes
        setTimeout(() => claimItem.classList.remove('new-claim-animation'), 500);

        // Limit list to 10 items
        const items = recentClaimsList.querySelectorAll('.list-group-item');
        if (items.length > 10) {
            items[items.length - 1].remove();
        }
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
     * Update connection status indicator
     */
    updateConnectionStatus(state) {
        const indicator = document.getElementById('connection-status-indicator');
        if (!indicator) return;

        const statusIcon = indicator.querySelector('i');
        const statusText = indicator.querySelector('.status-text');

        indicator.className = 'connection-status';

        switch (state) {
            case 'connected':
                indicator.classList.add('connected');
                if (statusIcon) statusIcon.className = 'bi bi-circle-fill';
                if (statusText) statusText.textContent = 'Connected';
                break;
            case 'connecting':
            case 'reconnecting':
                indicator.classList.add('connecting');
                if (statusIcon) statusIcon.className = 'bi bi-arrow-repeat';
                if (statusText) statusText.textContent = 'Connecting...';
                break;
            case 'disconnected':
            case 'error':
                indicator.classList.add('disconnected');
                if (statusIcon) statusIcon.className = 'bi bi-x-circle-fill';
                if (statusText) statusText.textContent = 'Disconnected';
                break;
        }
    }

    /**
     * Stop the real-time connection
     */
    async stop() {
        if (this.notificationHub) {
            await this.notificationHub.stop();
            this.isInitialized = false;
            console.log('Dashboard real-time stopped');
        }
    }
}

// Initialize when DOM is ready
let dashboardRealTime;

document.addEventListener('DOMContentLoaded', function() {
    dashboardRealTime = new DashboardRealTime();
    dashboardRealTime.initialize();
});

// Clean up on page unload
window.addEventListener('beforeunload', function() {
    if (dashboardRealTime) {
        dashboardRealTime.stop();
    }
});
