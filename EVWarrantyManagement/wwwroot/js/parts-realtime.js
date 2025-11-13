/**
 * Parts Page Real-Time Updates via SignalR
 * Handles live part updates, stock changes, and low stock alerts
 */

class PartsRealTime {
    constructor() {
        this.notificationHub = null;
        this.isInitialized = false;
        this.currentPartId = null;
        this.reloadScheduled = false;
    }

    /**
     * Initialize the parts real-time connection
     * @param {number} partId - Optional part ID if on details page
     */
    async initialize(partId = null) {
        if (this.isInitialized) {
            console.warn('Parts real-time already initialized');
            return;
        }

        this.currentPartId = partId;

        try {
            // Create SignalR connection to notification hub
            this.notificationHub = new SignalRConnection('/hubs/notification');

            // Register event handlers
            this.registerEventHandlers();

            // Start the connection
            await this.notificationHub.start();

            // If on a part details page, join the part group
            if (this.currentPartId) {
                await this.joinPartGroup(this.currentPartId);
            }

            this.isInitialized = true;
            console.log('Parts real-time initialized');

        } catch (error) {
            console.error('Failed to initialize parts real-time:', error);
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

        // Handle part updates
        this.notificationHub.on('ReceivePartUpdate', (data) => {
            this.handlePartUpdate(data);
        });

        // Handle stock alerts
        this.notificationHub.on('ReceiveStockAlert', (data) => {
            this.handleStockAlert(data);
        });
    }

    /**
     * Join a part-specific group for targeted updates
     */
    async joinPartGroup(partId) {
        if (!this.notificationHub) return;

        try {
            await this.notificationHub.invoke('JoinPartGroup', partId);
            console.log(`Joined part group: ${partId}`);
        } catch (error) {
            console.error(`Failed to join part group ${partId}:`, error);
        }
    }

    /**
     * Leave a part-specific group
     */
    async leavePartGroup(partId) {
        if (!this.notificationHub) return;

        try {
            await this.notificationHub.invoke('LeavePartGroup', partId);
            console.log(`Left part group: ${partId}`);
        } catch (error) {
            console.error(`Failed to leave part group ${partId}:`, error);
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
     * Get parts table body
     */
    getPartsTableBody() {
        return document.querySelector('#parts-table tbody, table[data-parts-table="true"] tbody, .table tbody');
    }

    /**
     * Handle part update
     */
    handlePartUpdate(data) {
        console.log('Part update received:', data);

        // If stock movement, reload Recent Part Updates table
        if (data.Type === 'stock_movement') {
            this.reloadRecentPartUpdates();
        }

        // If we're on the part details page for this part, update the UI
        if (this.currentPartId && (this.currentPartId === data.PartId || this.currentPartId === data.partId)) {
            this.updatePartDetailsPage(data);
        }

        // If we're on the parts list, update the table row
        this.updatePartInTable(data);

        // Show notification
        let notificationType = 'info';
        if (data.Type === 'part_created') {
            notificationType = 'success';
        } else if (data.Type === 'part_deleted') {
            notificationType = 'warning';
        } else if (data.Type === 'part_updated') {
            notificationType = 'info';
        } else if (data.Type === 'stock_movement') {
            notificationType = 'info';
        }

        this.handleNotification(data.Message || 'Part updated', notificationType);
    }

    /**
     * Handle stock alert
     */
    handleStockAlert(data) {
        console.log('Stock alert received:', data);

        // Show warning notification for low stock
        if (data.Type === 'low_stock_alert' || data.Type === 'stock_low') {
            this.showToast(
                `⚠️ Low Stock Alert: ${data.PartName || data.partName || 'Part'} - Only ${data.StockQuantity || data.stockQuantity || 0} units remaining`,
                'warning'
            );
        }

        // Update stock in table if on parts list
        if (data.PartId || data.partId) {
            this.updateStockInTable(data);
        }

        // Update stock in details page if viewing this part
        if (this.currentPartId && (this.currentPartId === data.PartId || this.currentPartId === data.partId)) {
            this.updateStockInDetailsPage(data);
        }
    }

    /**
     * Update part details page UI
     */
    updatePartDetailsPage(data) {
        // Update stock quantity if changed
        if (data.StockQuantity !== undefined || data.stockQuantity !== undefined) {
            const stockElement = document.getElementById('part-stock-quantity') || 
                                document.querySelector('[data-stock-quantity]');
            if (stockElement) {
                const newStock = data.StockQuantity || data.stockQuantity;
                stockElement.textContent = newStock;
                stockElement.classList.add('text-highlight');
                setTimeout(() => stockElement.classList.remove('text-highlight'), 1500);
            }
        }

        // Reload page if part was deleted
        if (data.Type === 'part_deleted') {
            setTimeout(() => {
                window.location.href = '/Parts/Index';
            }, 2000);
        } else if (data.Type === 'part_updated' || data.Type === 'stock_movement') {
            // Reload to show updated information
            setTimeout(() => {
                window.location.reload();
            }, 1000);
        }
    }

    /**
     * Update part in table (on parts list page)
     */
    updatePartInTable(data) {
        const partId = data.PartId || data.partId;
        if (!partId) {
            return;
        }

        const tableBody = this.getPartsTableBody();
        if (!tableBody) {
            return;
        }

        let partRow = tableBody.querySelector(`tr[data-part-id="${partId}"]`);
        if (!partRow) {
            // Try to find by part ID in first cell or part code
            const rows = tableBody.querySelectorAll('tr');
            for (const row of rows) {
                const firstCell = row.querySelector('td:first-child');
                if (firstCell && (firstCell.textContent.includes(`#${partId}`) || firstCell.textContent.includes(partId))) {
                    partRow = row;
                    break;
                }
            }
        }

        if (!partRow) {
            if (data.Type === 'part_created') {
                this.reloadPartsTable(500);
            } else if (data.Type === 'part_deleted') {
                // Part was deleted, don't try to update
                return;
            } else {
                this.reloadPartsTable();
            }
            return;
        }

        // If part was deleted, remove the row
        if (data.Type === 'part_deleted') {
            partRow.classList.add('table-row-delete');
            setTimeout(() => {
                partRow.remove();
            }, 1000);
            return;
        }

        // Update stock quantity if available
        if (data.StockQuantity !== undefined || data.stockQuantity !== undefined) {
            const stockCell = partRow.querySelector('td[data-stock], .stock-quantity');
            if (stockCell) {
                const newStock = data.StockQuantity || data.stockQuantity;
                stockCell.textContent = newStock;
                stockCell.classList.add('text-highlight');
                setTimeout(() => stockCell.classList.remove('text-highlight'), 1500);
            }
        }

        partRow.classList.add('table-row-highlight');
        setTimeout(() => partRow.classList.remove('table-row-highlight'), 2000);
    }

    /**
     * Update stock in table
     */
    updateStockInTable(data) {
        const partId = data.PartId || data.partId;
        if (!partId) {
            return;
        }

        const tableBody = this.getPartsTableBody();
        if (!tableBody) {
            return;
        }

        let partRow = tableBody.querySelector(`tr[data-part-id="${partId}"]`);
        if (!partRow) {
            return;
        }

        // Update stock quantity
        if (data.StockQuantity !== undefined || data.stockQuantity !== undefined) {
            const stockCell = partRow.querySelector('td[data-stock], .stock-quantity');
            if (stockCell) {
                const newStock = data.StockQuantity || data.stockQuantity;
                stockCell.textContent = newStock;
                
                // Add warning class if low stock
                if (data.MinStockLevel && newStock < data.MinStockLevel) {
                    stockCell.classList.add('text-danger', 'fw-bold');
                } else {
                    stockCell.classList.remove('text-danger', 'fw-bold');
                }
                
                stockCell.classList.add('text-highlight');
                setTimeout(() => stockCell.classList.remove('text-highlight'), 1500);
            }
        }
    }

    /**
     * Update stock in details page
     */
    updateStockInDetailsPage(data) {
        if (data.StockQuantity !== undefined || data.stockQuantity !== undefined) {
            const stockElement = document.getElementById('part-stock-quantity') || 
                                document.querySelector('[data-stock-quantity]');
            if (stockElement) {
                const newStock = data.StockQuantity || data.stockQuantity;
                stockElement.textContent = newStock;
                
                // Add warning class if low stock
                if (data.MinStockLevel && newStock < data.MinStockLevel) {
                    stockElement.classList.add('text-danger', 'fw-bold');
                } else {
                    stockElement.classList.remove('text-danger', 'fw-bold');
                }
                
                stockElement.classList.add('text-highlight');
                setTimeout(() => stockElement.classList.remove('text-highlight'), 1500);
            }
        }
    }

    /**
     * Reload parts table
     */
    reloadPartsTable(delay = 800) {
        if (this.reloadScheduled) {
            return;
        }
        this.reloadScheduled = true;
        setTimeout(() => {
            window.location.reload();
        }, delay);
    }

    /**
     * Reload Recent Part Updates table
     */
    reloadRecentPartUpdates(delay = 500) {
        // Check if we're on the Parts Index page
        if (!window.location.pathname.includes('/Parts') || window.location.pathname.includes('/Details')) {
            return;
        }

        // Reload the page to refresh Recent Part Updates
        if (!this.reloadScheduled) {
            this.reloadScheduled = true;
            setTimeout(() => {
                window.location.reload();
            }, delay);
        }
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
        // Leave part group if we're on a details page
        if (this.currentPartId && this.notificationHub) {
            await this.leavePartGroup(this.currentPartId);
        }

        if (this.notificationHub) {
            await this.notificationHub.stop();
            this.isInitialized = false;
            console.log('Parts real-time stopped');
        }
    }
}

// Export for global use
window.PartsRealTime = PartsRealTime;

// Auto-initialize if on parts page
document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on a parts page
    const isPartsPage = window.location.pathname.includes('/Parts');

    if (isPartsPage) {
        // Check if we're on a part details page
        const urlParams = new URLSearchParams(window.location.search);
        const partId = urlParams.get('id');
        const pathMatch = window.location.pathname.match(/\/Parts\/Details\/(\d+)/);
        const partIdFromPath = pathMatch ? parseInt(pathMatch[1]) : null;

        // Initialize parts real-time
        window.partsRealTime = new PartsRealTime();
        window.partsRealTime.initialize(partId ? parseInt(partId) : partIdFromPath);

        // Clean up on page unload
        window.addEventListener('beforeunload', function() {
            if (window.partsRealTime) {
                window.partsRealTime.stop();
            }
        });
    }
});

