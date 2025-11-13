/**
 * Claims Page Real-Time Updates via SignalR
 * Handles live claim updates on claims listing and details pages
 */

class ClaimsRealTime {
    constructor() {
        this.notificationHub = null;
        this.isInitialized = false;
        this.currentClaimId = null;
        this.reloadScheduled = false;
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

    getClaimsTableBody() {
        return document.querySelector('#claims-table tbody, table[data-claims-table="true"] tbody');
    }

    normalizeStatusValue(status) {
        if (!status) return '';
        return status.toString().trim();
    }

    getStatusClass(status) {
        const normalized = this.normalizeStatusValue(status).toLowerCase();
        switch (normalized) {
            case 'pending':
                return 'status-pending';
            case 'approved':
                return 'status-approved';
            case 'rejected':
                return 'status-rejected';
            case 'inprogress':
            case 'in progress':
                return 'status-inprogress';
            case 'completed':
                return 'status-completed';
            case 'archived':
                return 'status-archived';
            case 'onhold':
            case 'on hold':
                return 'status-onhold';
            case 'closed':
                return 'status-closed';
            default:
                return 'bg-secondary';
        }
    }

    formatDate(dateValue) {
        if (!dateValue) {
            return '';
        }

        try {
            if (dateValue instanceof Date) {
                return dateValue.toLocaleDateString();
            }

            // Handle DateOnly string (yyyy-MM-dd) or ISO strings
            const parsed = new Date(dateValue);
            if (!Number.isNaN(parsed.getTime())) {
                return parsed.toLocaleDateString();
            }
        } catch (error) {
            console.warn('Unable to format date value', dateValue, error);
        }

        return dateValue.toString();
    }

    shouldDisplayClaim(status) {
        const filterSelect = document.querySelector('select[name="status"]');
        if (!filterSelect) {
            return true;
        }

        const filterValue = (filterSelect.value || '').trim().toLowerCase();
        if (!filterValue) {
            return true;
        }

        return this.normalizeStatusValue(status).toLowerCase() === filterValue;
    }

    hasMinimumClaimData(data) {
        const model = data.vehicleModel || data.VehicleModel;
        const vin = data.vin || data.Vin;
        return Boolean(model && vin);
    }

    reloadClaimsTable(delay = 800) {
        if (this.reloadScheduled) {
            return;
        }
        this.reloadScheduled = true;
        setTimeout(() => {
            window.location.reload();
        }, delay);
    }

    updateClaimRowContent(row, data) {
        if (!row || !row.cells || row.cells.length < 7) {
            return;
        }

        const claimId = data.ClaimId || data.claimId;
        const vehicleModel = data.VehicleModel || data.vehicleModel || 'N/A';
        const vin = data.Vin || data.vin || '';
        const descriptionRaw = data.Description || data.description || '';
        const description = descriptionRaw.length > 0 ? descriptionRaw : 'N/A';
        const dateDiscovered = this.formatDate(data.DateDiscovered || data.dateDiscovered);
        const statusRaw = data.NewStatus || data.StatusCode || data.statusCode || row.dataset.claimStatus || '';
        const statusValue = this.normalizeStatusValue(statusRaw);
        const statusClass = this.getStatusClass(statusValue || statusRaw);
        const statusDisplay = statusValue || statusRaw || 'N/A';

        row.dataset.claimStatus = statusDisplay;

        row.cells[0].innerHTML = `<strong style="font-size: 0.9rem;">#${claimId}</strong>`;
        row.cells[1].innerHTML = `<i class="bi bi-car-front text-primary me-1"></i>${vehicleModel}`;
        row.cells[2].innerHTML = `<code style="font-size: 0.8rem;">${vin}</code>`;
        row.cells[3].textContent = description;
        row.cells[3].classList.add('text-truncate');
        row.cells[3].style.maxWidth = '280px';
        row.cells[4].innerHTML = `<i class="bi bi-calendar3 me-1 text-muted"></i>${dateDiscovered || ''}`;
        row.cells[5].innerHTML = `<span class="badge ${statusClass}">${statusDisplay}</span>`;
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
        if (!claimId) {
            return;
        }

        const tableBody = this.getClaimsTableBody();
        if (!tableBody) {
            return;
        }

        const statusValue = this.normalizeStatusValue(data.NewStatus || data.StatusCode || data.statusCode || '');
        const shouldDisplay = this.shouldDisplayClaim(statusValue);

        let claimRow = tableBody.querySelector(`tr[data-claim-id="${claimId}"]`);

        if (!claimRow) {
            console.log(`Claim row not found for ClaimId: ${claimId}`);

            if (shouldDisplay) {
                if (this.hasMinimumClaimData(data)) {
                    this.addClaimToTable(data);
                } else {
                    this.reloadClaimsTable();
                }
            }
            return;
        }

        if (!shouldDisplay) {
            claimRow.remove();
            return;
        }

        const previousStatus = (claimRow.dataset.claimStatus || '').toLowerCase();

        this.updateClaimRowContent(claimRow, data);

        const updatedStatus = (claimRow.dataset.claimStatus || '').toLowerCase();
        if (previousStatus && updatedStatus && previousStatus !== updatedStatus) {
            this.reloadClaimsTable(600);
            return;
        }

        const statusBadge = claimRow.querySelector('td:nth-child(6) .badge');
        if (statusBadge) {
            statusBadge.classList.add('badge-pulse');
            setTimeout(() => statusBadge.classList.remove('badge-pulse'), 1000);
        }

        claimRow.classList.add('table-row-highlight');
        setTimeout(() => claimRow.classList.remove('table-row-highlight'), 2000);
    }

    /**
     * Add new claim to table
     */
    addClaimToTable(data) {
        const tableBody = this.getClaimsTableBody();
        if (!tableBody) {
            console.log('Claims table not found, cannot add new claim to table');
            return;
        }

        const claimId = data.ClaimId || data.claimId;
        if (!claimId) {
            return;
        }

        const statusValue = this.normalizeStatusValue(data.StatusCode || data.statusCode || data.NewStatus || data.status || '');
        if (!this.shouldDisplayClaim(statusValue)) {
            return;
        }

        const existingRow = tableBody.querySelector(`tr[data-claim-id="${claimId}"]`);
        if (existingRow) {
            this.updateClaimRowContent(existingRow, data);
            return;
        }

        if (!this.hasMinimumClaimData(data)) {
            this.reloadClaimsTable();
            return;
        }

        const vehicleModel = data.VehicleModel || data.vehicleModel || 'N/A';
        const vin = data.Vin || data.vin || '';
        const desc = data.Description || data.description || '';
        const description = desc.length > 50 ? `${desc.substring(0, 50)}...` : (desc || 'N/A');
        const dateDiscovered = this.formatDate(data.DateDiscovered || data.dateDiscovered);
        const statusClass = this.getStatusClass(statusValue || 'Pending');
        const statusDisplay = statusValue || 'Pending';

        const row = document.createElement('tr');
        row.dataset.claimId = claimId;
        row.dataset.claimStatus = statusDisplay;
        row.className = 'new-row-animation';

        row.innerHTML = `
            <td><strong style="font-size: 0.9rem;">#${claimId}</strong></td>
            <td style="font-size: 0.875rem;"><i class="bi bi-car-front text-primary me-1"></i>${vehicleModel}</td>
            <td><code style="font-size: 0.8rem;">${vin}</code></td>
            <td class="text-truncate" style="max-width: 280px; font-size: 0.875rem;">${description}</td>
            <td style="font-size: 0.875rem;"><i class="bi bi-calendar3 me-1 text-muted"></i>${dateDiscovered || ''}</td>
            <td><span class="badge ${statusClass}">${statusDisplay}</span></td>
            <td class="py-1 text-nowrap">
                <a class="btn btn-xs btn-outline-primary" href="/Claims/Details?id=${claimId}" style="font-size: 0.75rem; padding: 0.2rem 0.5rem;">
                    <i class="bi bi-eye"></i>
                </a>
            </td>
        `;

        if (tableBody.firstChild) {
            tableBody.insertBefore(row, tableBody.firstChild);
        } else {
            tableBody.appendChild(row);
        }

        setTimeout(() => row.classList.remove('new-row-animation'), 1500);
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

