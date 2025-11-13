/**
 * Service Centers Page Real-Time Updates via SignalR
 * Handles live service center updates and technician assignments
 */

class ServiceCentersRealTime {
    constructor() {
        this.notificationHub = null;
        this.isInitialized = false;
        this.currentServiceCenterId = null;
        this.reloadScheduled = false;
    }

    /**
     * Initialize the service centers real-time connection
     * @param {number} serviceCenterId - Optional service center ID if on details page
     */
    async initialize(serviceCenterId = null) {
        if (this.isInitialized) {
            console.warn('Service centers real-time already initialized');
            return;
        }

        this.currentServiceCenterId = serviceCenterId;

        try {
            // Create SignalR connection to notification hub
            this.notificationHub = new SignalRConnection('/hubs/notification');

            // Register event handlers
            this.registerEventHandlers();

            // Start the connection
            await this.notificationHub.start();

            // If on a service center details page, join the service center group
            if (this.currentServiceCenterId) {
                await this.joinServiceCenterGroup(this.currentServiceCenterId);
            }

            this.isInitialized = true;
            console.log('Service centers real-time initialized');

        } catch (error) {
            console.error('Failed to initialize service centers real-time:', error);
        }
    }

    /**
     * Register SignalR event handlers
     */
    registerEventHandlers() {
        // Handle general notifications
        this.notificationHub.on('ReceiveNotification', (data) => {
            if (typeof data === 'string') {
                this.handleNotification(data, 'info');
            } else {
                this.handleNotification(data.Message || data.message || '', data.Type || data.type || 'info');
            }
        });

        // Handle service center updates
        this.notificationHub.on('ReceiveServiceCenterUpdate', (data) => {
            this.handleServiceCenterUpdate(data);
        });
    }

    /**
     * Join a service center-specific group for targeted updates
     */
    async joinServiceCenterGroup(serviceCenterId) {
        if (!this.notificationHub) return;

        try {
            await this.notificationHub.invoke('JoinServiceCenterGroup', serviceCenterId);
            console.log(`Joined service center group: ${serviceCenterId}`);
        } catch (error) {
            console.error(`Failed to join service center group ${serviceCenterId}:`, error);
        }
    }

    /**
     * Leave a service center-specific group
     */
    async leaveServiceCenterGroup(serviceCenterId) {
        if (!this.notificationHub) return;

        try {
            await this.notificationHub.invoke('LeaveServiceCenterGroup', serviceCenterId);
            console.log(`Left service center group: ${serviceCenterId}`);
        } catch (error) {
            console.error(`Failed to leave service center group ${serviceCenterId}:`, error);
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
                    <div class="toast-body">${this.escapeHtml(message)}</div>
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
        const existing = document.getElementById('toast-container');
        if (existing) {
            return existing;
        }

        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '9999';
        document.body.appendChild(container);
        return container;
    }

    /**
     * Get service centers table body
     */
    getServiceCentersTableBody() {
        return document.querySelector('#servicecenters-table tbody, table[data-servicecenters-table="true"] tbody, .table tbody');
    }

    /**
     * Handle service center update
     */
    handleServiceCenterUpdate(data) {
        console.log('Service center update received:', data);

        // If we're on the service center details page for this service center, update the UI
        if (this.currentServiceCenterId && (this.currentServiceCenterId === data.ServiceCenterId || this.currentServiceCenterId === data.serviceCenterId)) {
            this.updateServiceCenterDetailsPage(data);
        }

        // If we're on the service centers list, update the table row
        this.updateServiceCenterInTable(data);

        // Show notification
        let notificationType = 'info';
        if (data.Type === 'servicecenter_updated') {
            notificationType = 'success';
        } else if (data.Type === 'technician_assigned') {
            notificationType = 'success';
        } else if (data.Type === 'technician_removed') {
            notificationType = 'warning';
        }

        this.handleNotification(data.Message || 'Service center updated', notificationType);
    }

    /**
     * Update service center details page UI
     */
    updateServiceCenterDetailsPage(data) {
        // If technician was assigned or removed, reload to show updated list
        if (data.Type === 'technician_assigned' || data.Type === 'technician_removed') {
            setTimeout(() => {
                window.location.reload();
            }, 1000);
        } else if (data.Type === 'servicecenter_updated') {
            // Reload to show updated information
            setTimeout(() => {
                window.location.reload();
            }, 1000);
        }
    }

    /**
     * Update service center in table (on service centers list page)
     */
    updateServiceCenterInTable(data) {
        const serviceCenterId = data.ServiceCenterId || data.serviceCenterId;
        if (!serviceCenterId) {
            return;
        }

        const tableBody = this.getServiceCentersTableBody();
        if (!tableBody) {
            return;
        }

        let serviceCenterRow = tableBody.querySelector(`tr[data-servicecenter-id="${serviceCenterId}"]`);
        if (!serviceCenterRow) {
            // Try to find by service center ID in first cell
            const rows = tableBody.querySelectorAll('tr');
            for (const row of rows) {
                const firstCell = row.querySelector('td:first-child');
                if (firstCell && (firstCell.textContent.includes(`#${serviceCenterId}`) || firstCell.textContent.includes(serviceCenterId.toString()))) {
                    serviceCenterRow = row;
                    break;
                }
            }
        }

        if (!serviceCenterRow) {
            // Reload table to show new service center entry
            this.reloadServiceCentersTable(500);
            return;
        }

        // Update name if changed
        if (data.Name || data.name) {
            const nameCell = serviceCenterRow.querySelector('td:first-child, .service-center-name');
            if (nameCell) {
                nameCell.textContent = data.Name || data.name;
            }
        }

        serviceCenterRow.classList.add('table-row-highlight');
        setTimeout(() => serviceCenterRow.classList.remove('table-row-highlight'), 2000);
    }

    /**
     * Reload service centers table
     */
    reloadServiceCentersTable(delay = 800) {
        if (this.reloadScheduled) {
            return;
        }
        this.reloadScheduled = true;
        setTimeout(() => {
            window.location.reload();
        }, delay);
    }

    /**
     * Escape HTML
     */
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Stop the real-time connection
     */
    async stop() {
        // Leave service center group if we're on a details page
        if (this.currentServiceCenterId && this.notificationHub) {
            await this.leaveServiceCenterGroup(this.currentServiceCenterId);
        }

        if (this.notificationHub) {
            await this.notificationHub.stop();
            this.isInitialized = false;
            console.log('Service centers real-time stopped');
        }
    }
}

// Export for global use
window.ServiceCentersRealTime = ServiceCentersRealTime;

// Auto-initialize if on service centers page
document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on a service centers page
    const isServiceCentersPage = window.location.pathname.includes('/ServiceCenters');

    if (isServiceCentersPage) {
        // Check if we're on a service center details page
        const urlParams = new URLSearchParams(window.location.search);
        const serviceCenterId = urlParams.get('id');
        const pathMatch = window.location.pathname.match(/\/ServiceCenters\/Details\/(\d+)/);
        const serviceCenterIdFromPath = pathMatch ? parseInt(pathMatch[1]) : null;

        // Initialize service centers real-time
        window.serviceCentersRealTime = new ServiceCentersRealTime();
        window.serviceCentersRealTime.initialize(serviceCenterId ? parseInt(serviceCenterId) : serviceCenterIdFromPath);

        // Clean up on page unload
        window.addEventListener('beforeunload', function() {
            if (window.serviceCentersRealTime) {
                window.serviceCentersRealTime.stop();
            }
        });
    }
});

