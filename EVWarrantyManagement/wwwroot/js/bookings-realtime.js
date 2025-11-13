/**
 * Bookings Page Real-Time Updates via SignalR
 * Handles live booking updates on bookings listing and details pages
 */

class BookingsRealTime {
    constructor() {
        this.notificationHub = null;
        this.isInitialized = false;
        this.currentBookingId = null;
        this.reloadScheduled = false;
    }

    /**
     * Initialize the bookings real-time connection
     * @param {number} bookingId - Optional booking ID if on details page
     */
    async initialize(bookingId = null) {
        if (this.isInitialized) {
            console.warn('Bookings real-time already initialized');
            return;
        }

        this.currentBookingId = bookingId;

        try {
            // Create SignalR connection to notification hub
            this.notificationHub = new SignalRConnection('/hubs/notification');

            // Register event handlers
            this.registerEventHandlers();

            // Start the connection
            await this.notificationHub.start();

            // If on a booking details page, join the booking group
            if (this.currentBookingId) {
                await this.joinBookingGroup(this.currentBookingId);
            }

            this.isInitialized = true;
            console.log('Bookings real-time initialized');

        } catch (error) {
            console.error('Failed to initialize bookings real-time:', error);
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

        // Handle booking updates
        this.notificationHub.on('ReceiveBookingUpdate', (data) => {
            this.handleBookingUpdate(data);
        });

        // Handle new bookings
        this.notificationHub.on('ReceiveNewBooking', (data) => {
            this.handleNewBooking(data);
        });
    }

    /**
     * Join a booking-specific group for targeted updates
     */
    async joinBookingGroup(bookingId) {
        if (!this.notificationHub) return;

        try {
            await this.notificationHub.invoke('JoinBookingGroup', bookingId);
            console.log(`Joined booking group: ${bookingId}`);
        } catch (error) {
            console.error(`Failed to join booking group ${bookingId}:`, error);
        }
    }

    /**
     * Leave a booking-specific group
     */
    async leaveBookingGroup(bookingId) {
        if (!this.notificationHub) return;

        try {
            await this.notificationHub.invoke('LeaveBookingGroup', bookingId);
            console.log(`Left booking group: ${bookingId}`);
        } catch (error) {
            console.error(`Failed to leave booking group ${bookingId}:`, error);
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
     * Get bookings table body
     */
    getBookingsTableBody() {
        return document.querySelector('#bookings-table tbody, table[data-bookings-table="true"] tbody, .table tbody');
    }

    /**
     * Normalize status value
     */
    normalizeStatusValue(status) {
        if (!status) return '';
        return status.toString().trim();
    }

    /**
     * Get status class for badge
     */
    getStatusClass(status) {
        const normalized = this.normalizeStatusValue(status).toLowerCase();
        switch (normalized) {
            case 'pending':
                return 'bg-warning';
            case 'approved':
                return 'bg-success';
            case 'inprogress':
            case 'in progress':
                return 'bg-info';
            case 'completed':
                return 'bg-success';
            case 'rejected':
                return 'bg-danger';
            case 'cancelled':
                return 'bg-secondary';
            default:
                return 'bg-secondary';
        }
    }

    /**
     * Format date
     */
    formatDate(dateValue) {
        if (!dateValue) {
            return '';
        }

        try {
            if (dateValue instanceof Date) {
                return dateValue.toLocaleDateString();
            }

            const parsed = new Date(dateValue);
            if (!Number.isNaN(parsed.getTime())) {
                return parsed.toLocaleDateString();
            }
        } catch (error) {
            console.warn('Unable to format date value', dateValue, error);
        }

        return dateValue.toString();
    }

    /**
     * Reload bookings table
     */
    reloadBookingsTable(delay = 800) {
        if (this.reloadScheduled) {
            return;
        }
        this.reloadScheduled = true;
        setTimeout(() => {
            window.location.reload();
        }, delay);
    }

    /**
     * Handle booking update
     */
    handleBookingUpdate(data) {
        console.log('Booking update received:', data);

        // If we're on the booking details page for this booking, update the UI
        if (this.currentBookingId && (this.currentBookingId === data.BookingId || this.currentBookingId === data.bookingId)) {
            this.updateBookingDetailsPage(data);
        }

        // If we're on the bookings list, update the table row
        this.updateBookingInTable(data);

        // Show notification
        let notificationType = 'info';
        if (data.Type === 'status_change' || data.Type === 'booking_status_changed') {
            if (data.NewStatus === 'Approved' || data.NewStatus === 'Completed') {
                notificationType = 'success';
            } else if (data.NewStatus === 'Rejected' || data.NewStatus === 'Cancelled') {
                notificationType = 'error';
            } else if (data.NewStatus === 'Pending') {
                notificationType = 'warning';
            }
        } else if (data.Type === 'booking_assigned') {
            notificationType = 'success';
        }

        this.handleNotification(data.Message || 'Booking updated', notificationType);
    }

    /**
     * Handle new booking
     */
    handleNewBooking(data) {
        console.log('New booking received:', data);

        // Add to the bookings table if it exists
        this.addBookingToTable(data);

        // Show notification
        this.handleNotification(data.Message || `New booking #${data.bookingId || data.BookingId} created`, 'info');
    }

    /**
     * Update booking details page UI
     */
    updateBookingDetailsPage(data) {
        // Update status badge
        if (data.NewStatus || data.Status) {
            const statusBadge = document.querySelector('.booking-status-badge, .badge.bg-warning, .badge.bg-success');
            if (statusBadge) {
                const newStatus = data.NewStatus || data.Status;
                statusBadge.textContent = newStatus;
                statusBadge.className = `badge ${this.getStatusClass(newStatus)} badge-pulse`;
                setTimeout(() => statusBadge.classList.remove('badge-pulse'), 1000);
            }
        }

        // Reload page if status changed significantly
        if (data.Type === 'status_change' || data.Type === 'booking_status_changed') {
            setTimeout(() => {
                window.location.reload();
            }, 1000);
        }
    }

    /**
     * Update booking in table (on bookings list page)
     */
    updateBookingInTable(data) {
        const bookingId = data.BookingId || data.bookingId;
        if (!bookingId) {
            return;
        }

        const tableBody = this.getBookingsTableBody();
        if (!tableBody) {
            return;
        }

        let bookingRow = tableBody.querySelector(`tr[data-booking-id="${bookingId}"]`);
        if (!bookingRow) {
            // Try to find by booking ID in first cell
            const rows = tableBody.querySelectorAll('tr');
            for (const row of rows) {
                const firstCell = row.querySelector('td:first-child');
                if (firstCell && firstCell.textContent.includes(`#${bookingId}`)) {
                    bookingRow = row;
                    break;
                }
            }
        }

        if (!bookingRow) {
            if (data.Type === 'booking_created' || data.Type === 'new_booking') {
                this.addBookingToTable(data);
            } else {
                this.reloadBookingsTable();
            }
            return;
        }

        // Update status if changed
        if (data.NewStatus || data.Status) {
            const statusCell = bookingRow.querySelector('td:last-child, .badge');
            if (statusCell) {
                const newStatus = data.NewStatus || data.Status;
                statusCell.innerHTML = `<span class="badge ${this.getStatusClass(newStatus)}">${newStatus}</span>`;
            }
        }

        bookingRow.classList.add('table-row-highlight');
        setTimeout(() => bookingRow.classList.remove('table-row-highlight'), 2000);
    }

    /**
     * Add new booking to table
     */
    addBookingToTable(data) {
        const tableBody = this.getBookingsTableBody();
        if (!tableBody) {
            console.log('Bookings table not found, cannot add new booking to table');
            return;
        }

        const bookingId = data.BookingId || data.bookingId;
        if (!bookingId) {
            return;
        }

        // Reload table to ensure all data is correct
        this.reloadBookingsTable(500);
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
        // Leave booking group if we're on a details page
        if (this.currentBookingId && this.notificationHub) {
            await this.leaveBookingGroup(this.currentBookingId);
        }

        if (this.notificationHub) {
            await this.notificationHub.stop();
            this.isInitialized = false;
            console.log('Bookings real-time stopped');
        }
    }
}

// Export for global use
window.BookingsRealTime = BookingsRealTime;

// Auto-initialize if on bookings page
document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on a bookings page
    const isBookingsPage = window.location.pathname.includes('/Bookings');

    if (isBookingsPage) {
        // Check if we're on a booking details page
        const urlParams = new URLSearchParams(window.location.search);
        const bookingId = urlParams.get('id');
        const pathMatch = window.location.pathname.match(/\/Bookings\/Details\/(\d+)/);
        const bookingIdFromPath = pathMatch ? parseInt(pathMatch[1]) : null;

        // Initialize bookings real-time
        window.bookingsRealTime = new BookingsRealTime();
        window.bookingsRealTime.initialize(bookingId ? parseInt(bookingId) : bookingIdFromPath);

        // Clean up on page unload
        window.addEventListener('beforeunload', function() {
            if (window.bookingsRealTime) {
                window.bookingsRealTime.stop();
            }
        });
    }
});

