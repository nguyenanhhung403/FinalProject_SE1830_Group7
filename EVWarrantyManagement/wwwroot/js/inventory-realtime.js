/**
 * Inventory Page Real-Time Updates via SignalR
 * Handles live inventory updates, stock adjustments, and stock movements
 */

class InventoryRealTime {
    constructor() {
        this.notificationHub = null;
        this.isInitialized = false;
        this.currentPartId = null;
        this.reloadScheduled = false;
    }

    /**
     * Initialize the inventory real-time connection
     * @param {number} partId - Optional part ID if on specific inventory page
     */
    async initialize(partId = null) {
        if (this.isInitialized) {
            console.warn('Inventory real-time already initialized');
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

            // If tracking a specific part, join the inventory group
            if (this.currentPartId) {
                await this.joinInventoryGroup(this.currentPartId);
            }

            this.isInitialized = true;
            console.log('Inventory real-time initialized');

        } catch (error) {
            console.error('Failed to initialize inventory real-time:', error);
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

        // Handle inventory updates
        this.notificationHub.on('ReceiveInventoryUpdate', (data) => {
            this.handleInventoryUpdate(data);
        });
    }

    /**
     * Join an inventory-specific group for targeted updates
     */
    async joinInventoryGroup(partId) {
        if (!this.notificationHub) return;

        try {
            await this.notificationHub.invoke('JoinInventoryGroup', partId);
            console.log(`Joined inventory group: ${partId}`);
        } catch (error) {
            console.error(`Failed to join inventory group ${partId}:`, error);
        }
    }

    /**
     * Leave an inventory-specific group
     */
    async leaveInventoryGroup(partId) {
        if (!this.notificationHub) return;

        try {
            await this.notificationHub.invoke('LeaveInventoryGroup', partId);
            console.log(`Left inventory group: ${partId}`);
        } catch (error) {
            console.error(`Failed to leave inventory group ${partId}:`, error);
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
     * Get inventory table body
     */
    getInventoryTableBody() {
        return document.querySelector('#inventory-table tbody, table[data-inventory-table="true"] tbody, .table tbody');
    }

    /**
     * Handle inventory update
     */
    handleInventoryUpdate(data) {
        console.log('Inventory update received:', data);

        // Update inventory in table
        this.updateInventoryInTable(data);

        // Show notification
        let notificationType = 'info';
        if (data.Type === 'stock_adjusted' || data.Type === 'stock_in') {
            notificationType = 'success';
        } else if (data.Type === 'stock_out') {
            notificationType = 'warning';
        }

        const message = data.Message || 
                       (data.Type === 'stock_adjusted' ? `Stock adjusted for ${data.PartName || 'part'}` :
                        data.Type === 'stock_in' ? `Stock received: ${data.Quantity || 0} units of ${data.PartName || 'part'}` :
                        data.Type === 'stock_out' ? `Stock consumed: ${data.Quantity || 0} units of ${data.PartName || 'part'}` :
                        'Inventory updated');

        this.handleNotification(message, notificationType);
    }

    /**
     * Update inventory in table
     */
    updateInventoryInTable(data) {
        const partId = data.PartId || data.partId;
        if (!partId) {
            return;
        }

        const tableBody = this.getInventoryTableBody();
        if (!tableBody) {
            return;
        }

        let inventoryRow = tableBody.querySelector(`tr[data-part-id="${partId}"]`);
        if (!inventoryRow) {
            // Try to find by part ID in cells
            const rows = tableBody.querySelectorAll('tr');
            for (const row of rows) {
                const cells = row.querySelectorAll('td');
                for (const cell of cells) {
                    if (cell.textContent.includes(`#${partId}`) || cell.textContent.includes(partId.toString())) {
                        inventoryRow = row;
                        break;
                    }
                }
                if (inventoryRow) break;
            }
        }

        if (!inventoryRow) {
            // Reload table to show new inventory entry
            this.reloadInventoryTable(500);
            return;
        }

        // Update stock quantity if available
        if (data.StockQuantity !== undefined || data.stockQuantity !== undefined) {
            const stockCell = inventoryRow.querySelector('td[data-stock], .stock-quantity, td:nth-child(3), td:nth-child(4)');
            if (stockCell) {
                const newStock = data.StockQuantity || data.stockQuantity;
                stockCell.textContent = newStock;
                stockCell.classList.add('text-highlight');
                setTimeout(() => stockCell.classList.remove('text-highlight'), 1500);
            }
        }

        // Update last movement if available
        if (data.MovementType || data.movementType) {
            const movementCell = inventoryRow.querySelector('td[data-movement], .movement-type');
            if (movementCell) {
                const movementType = data.MovementType || data.movementType;
                movementCell.textContent = movementType;
            }
        }

        inventoryRow.classList.add('table-row-highlight');
        setTimeout(() => inventoryRow.classList.remove('table-row-highlight'), 2000);
    }

    /**
     * Reload inventory table
     */
    reloadInventoryTable(delay = 800) {
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
        // Leave inventory group if tracking a specific part
        if (this.currentPartId && this.notificationHub) {
            await this.leaveInventoryGroup(this.currentPartId);
        }

        if (this.notificationHub) {
            await this.notificationHub.stop();
            this.isInitialized = false;
            console.log('Inventory real-time stopped');
        }
    }
}

// Export for global use
window.InventoryRealTime = InventoryRealTime;

// Auto-initialize if on inventory page
document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on an inventory page
    const isInventoryPage = window.location.pathname.includes('/Inventory');

    if (isInventoryPage) {
        // Check if we're tracking a specific part
        const urlParams = new URLSearchParams(window.location.search);
        const partId = urlParams.get('partId');

        // Initialize inventory real-time
        window.inventoryRealTime = new InventoryRealTime();
        window.inventoryRealTime.initialize(partId ? parseInt(partId) : null);

        // Clean up on page unload
        window.addEventListener('beforeunload', function() {
            if (window.inventoryRealTime) {
                window.inventoryRealTime.stop();
            }
        });
    }
});

