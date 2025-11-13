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
        // Show toast notification using Bootstrap toast
        this.showToast(message, type);

        // Log to console
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
        
        // Remove toast element after it's hidden
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
     * Handle new claim notification
     */
    handleNewClaim(data) {
        console.log('New claim received:', data);

        // Show notification
        this.handleNotification(data.Message || `New claim #${data.claimId || data.ClaimId} created`, 'info');

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

        this.handleNotification(data.Message || 'Claim updated', notificationType);

        // Refresh dashboard statistics
        if (typeof refreshDashboardStats === 'function') {
            refreshDashboardStats();
        }
    }

    /**
     * Add new claim to dashboard table
     */
    addClaimToDashboardTable(data) {
        const dashboardTable = document.querySelector('#dashboard-claims-table tbody, .table tbody');
        if (!dashboardTable) {
            console.log('Dashboard table not found, cannot add new claim');
            return;
        }

        console.log('Adding new claim to dashboard table:', data);

        // Create new row matching dashboard structure
        const row = document.createElement('tr');
        row.setAttribute('data-claim-id', data.claimId || data.ClaimId);
        row.className = 'new-row-animation';

        row.innerHTML = `
            <td><strong>#${data.claimId || data.ClaimId}</strong></td>
            <td><i class="bi bi-car-front text-primary me-1"></i>${data.vehicleModel || data.VehicleModel || 'N/A'}</td>
            <td><code>${data.vin || data.Vin}</code></td>
            <td><span class="badge status-${(data.statusCode || data.StatusCode || 'Pending').toLowerCase().replace(' ', '')}">${data.statusCode || data.StatusCode || 'Pending'}</span></td>
            <td><i class="bi bi-building text-muted me-1"></i>${data.serviceCenterName || data.ServiceCenterName || 'Unknown'}</td>
            <td><i class="bi bi-calendar3 me-1 text-muted"></i>Just now</td>
            <td class="text-nowrap">
                <a class="btn btn-sm btn-outline-primary" href="/Claims/Details/${data.claimId || data.ClaimId}">
                    <i class="bi bi-eye me-1"></i>Detail
                </a>
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
        const claimRow = document.querySelector(`[data-claim-id="${data.ClaimId || data.claimId}"]`);
        if (!claimRow) {
            console.log(`Claim row not found for ClaimId: ${data.ClaimId || data.claimId}`);
            return;
        }

        console.log('Updating claim in dashboard:', data);

        // Update status badge
        if (data.NewStatus) {
            const statusCell = claimRow.querySelector('td:nth-child(4)') || claimRow.cells[3];
            if (statusCell) {
                statusCell.innerHTML = `<span class="badge status-${data.NewStatus.toLowerCase().replace(' ', '')} badge-pulse">${data.NewStatus}</span>`;

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

// Initialize when DOM is ready (only on dashboard pages)
document.addEventListener('DOMContentLoaded', function() {
    // Only initialize on dashboard pages
    if (window.location.pathname.includes('/Dashboard')) {
        window.dashboardRealTime = new DashboardRealTime();
        window.dashboardRealTime.initialize();
    }
});

// Clean up on page unload
window.addEventListener('beforeunload', function() {
    if (window.dashboardRealTime) {
        window.dashboardRealTime.stop();
    }
});

