/**
 * Notification System
 * Handles real-time notifications via SignalR
 */

class NotificationManager {
    constructor() {
        this.notifications = [];
        this.unreadCount = 0;
        this.notificationHub = null;
        this.isInitialized = false;
        this.init();
    }

    async init() {
        if (this.isInitialized) {
            return;
        }

        try {
            // Create SignalR connection to notification hub
            this.notificationHub = new SignalRConnection('/hubs/notification');

            // Setup event handlers before starting connection (they will be registered when connection starts)
            this.setupEventHandlers();

            // Start the connection
            await this.notificationHub.start();

            this.isInitialized = true;
            console.log('NotificationManager initialized');

            // Load initial notifications
            this.loadNotifications();
        } catch (error) {
            console.error('Failed to initialize NotificationManager:', error);
            // Retry after delay
            setTimeout(() => this.init(), 2000);
        }
    }

    setupEventHandlers() {
        if (!this.notificationHub) {
            console.error('NotificationHub not initialized');
            return;
        }

        // Handle general notifications
        this.notificationHub.on('ReceiveNotification', (data) => {
            if (typeof data === 'string') {
                // Simple string notification
                this.addNotification({
                    type: 'info',
                    title: 'Notification',
                    message: data,
                    timestamp: new Date()
                });
            } else {
                // Object notification
                this.addNotification({
                    type: data.Type || data.type || 'info',
                    title: data.Title || data.title || 'Notification',
                    message: data.Message || data.message || '',
                    timestamp: new Date(),
                    ...data
                });
            }
        });

        // Handle claim updates
        this.notificationHub.on('ReceiveClaimUpdate', (data) => {
            if (data.Type === 'status_change') {
                this.addNotification({
                    type: 'info',
                    title: 'Claim Status Changed',
                    message: data.Message || `Claim #${data.ClaimId} status changed from ${data.OldStatus} to ${data.NewStatus}`,
                    claimId: data.ClaimId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'assigned' || data.Type === 'technician_assigned') {
                this.addNotification({
                    type: 'success',
                    title: 'Claim Assigned',
                    message: data.Message || `You have been assigned to Claim #${data.ClaimId}`,
                    claimId: data.ClaimId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'part_added') {
                this.addNotification({
                    type: 'info',
                    title: 'Part Added to Claim',
                    message: data.Message || `Part ${data.PartName || ''} added to Claim #${data.ClaimId}`,
                    claimId: data.ClaimId,
                    timestamp: new Date()
                });
            }
        });

        // Handle new claims
        this.notificationHub.on('ReceiveNewClaim', (data) => {
            this.addNotification({
                type: 'info',
                title: 'New Claim Created',
                message: data.Message || `New claim #${data.ClaimId} from ${data.ServiceCenterName || 'Service Center'}`,
                claimId: data.ClaimId,
                timestamp: new Date()
            });
        });

        // Handle booking updates
        this.notificationHub.on('ReceiveBookingUpdate', (data) => {
            if (data.Type === 'booking_status_changed' || data.Type === 'status_change') {
                this.addNotification({
                    type: data.NewStatus === 'Approved' || data.NewStatus === 'Completed' ? 'success' : 'info',
                    title: 'Booking Status Changed',
                    message: data.Message || `Booking #${data.BookingId || data.bookingId} status changed to ${data.NewStatus || data.Status}`,
                    bookingId: data.BookingId || data.bookingId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'booking_assigned') {
                this.addNotification({
                    type: 'success',
                    title: 'Booking Assigned',
                    message: data.Message || `You have been assigned to Booking #${data.BookingId || data.bookingId}`,
                    bookingId: data.BookingId || data.bookingId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'booking_created') {
                this.addNotification({
                    type: 'info',
                    title: 'New Booking Created',
                    message: data.Message || `New booking #${data.BookingId || data.bookingId} created`,
                    bookingId: data.BookingId || data.bookingId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'booking_completed') {
                this.addNotification({
                    type: 'success',
                    title: 'Booking Completed',
                    message: data.Message || `Booking #${data.BookingId || data.bookingId} has been completed`,
                    bookingId: data.BookingId || data.bookingId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'booking_cancelled') {
                this.addNotification({
                    type: 'warning',
                    title: 'Booking Cancelled',
                    message: data.Message || `Booking #${data.BookingId || data.bookingId} has been cancelled`,
                    bookingId: data.BookingId || data.bookingId,
                    timestamp: new Date()
                });
            }
        });

        // Handle new bookings
        this.notificationHub.on('ReceiveNewBooking', (data) => {
            this.addNotification({
                type: 'info',
                title: 'New Booking Created',
                message: data.Message || `New booking #${data.BookingId || data.bookingId} from ${data.CustomerName || 'Customer'}`,
                bookingId: data.BookingId || data.bookingId,
                timestamp: new Date()
            });
        });

        // Handle part updates
        this.notificationHub.on('ReceivePartUpdate', (data) => {
            if (data.Type === 'part_created') {
                this.addNotification({
                    type: 'success',
                    title: 'New Part Created',
                    message: data.Message || `New part ${data.PartName || data.partName || ''} created`,
                    partId: data.PartId || data.partId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'part_updated') {
                this.addNotification({
                    type: 'info',
                    title: 'Part Updated',
                    message: data.Message || `Part ${data.PartName || data.partName || ''} has been updated`,
                    partId: data.PartId || data.partId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'part_deleted') {
                this.addNotification({
                    type: 'warning',
                    title: 'Part Deleted',
                    message: data.Message || `Part ${data.PartName || data.partName || ''} has been deleted`,
                    partId: data.PartId || data.partId,
                    timestamp: new Date()
                });
            }
        });

        // Handle stock alerts
        this.notificationHub.on('ReceiveStockAlert', (data) => {
            if (data.Type === 'low_stock_alert' || data.Type === 'stock_low') {
                this.addNotification({
                    type: 'warning',
                    title: 'Low Stock Alert',
                    message: data.Message || `⚠️ ${data.PartName || data.partName || 'Part'} is low on stock. Only ${data.StockQuantity || data.stockQuantity || 0} units remaining`,
                    partId: data.PartId || data.partId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'stock_movement') {
                this.addNotification({
                    type: 'info',
                    title: 'Stock Movement',
                    message: data.Message || `Stock movement: ${data.Quantity || 0} units of ${data.PartName || data.partName || 'part'}`,
                    partId: data.PartId || data.partId,
                    timestamp: new Date()
                });
            }
        });

        // Handle inventory updates
        this.notificationHub.on('ReceiveInventoryUpdate', (data) => {
            if (data.Type === 'stock_adjusted') {
                this.addNotification({
                    type: 'success',
                    title: 'Stock Adjusted',
                    message: data.Message || `Stock adjusted for ${data.PartName || data.partName || 'part'}: ${data.Quantity || 0} units`,
                    partId: data.PartId || data.partId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'stock_in') {
                this.addNotification({
                    type: 'success',
                    title: 'Stock Received',
                    message: data.Message || `Stock received: ${data.Quantity || 0} units of ${data.PartName || data.partName || 'part'}`,
                    partId: data.PartId || data.partId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'stock_out') {
                this.addNotification({
                    type: 'info',
                    title: 'Stock Consumed',
                    message: data.Message || `Stock consumed: ${data.Quantity || 0} units of ${data.PartName || data.partName || 'part'}`,
                    partId: data.PartId || data.partId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'inventory_synced') {
                this.addNotification({
                    type: 'info',
                    title: 'Inventory Synchronized',
                    message: data.Message || 'Inventory has been synchronized',
                    timestamp: new Date()
                });
            }
        });

        // Handle service center updates
        this.notificationHub.on('ReceiveServiceCenterUpdate', (data) => {
            if (data.Type === 'servicecenter_updated') {
                this.addNotification({
                    type: 'info',
                    title: 'Service Center Updated',
                    message: data.Message || `Service center ${data.ServiceCenterName || data.name || ''} has been updated`,
                    serviceCenterId: data.ServiceCenterId || data.serviceCenterId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'technician_assigned') {
                this.addNotification({
                    type: 'success',
                    title: 'Technician Assigned',
                    message: data.Message || `You have been assigned to ${data.ServiceCenterName || data.name || 'Service Center'}`,
                    serviceCenterId: data.ServiceCenterId || data.serviceCenterId,
                    timestamp: new Date()
                });
            } else if (data.Type === 'technician_removed') {
                this.addNotification({
                    type: 'warning',
                    title: 'Technician Removed',
                    message: data.Message || `You have been removed from ${data.ServiceCenterName || data.name || 'Service Center'}`,
                    serviceCenterId: data.ServiceCenterId || data.serviceCenterId,
                    timestamp: new Date()
                });
            }
        });

        // Setup mark all read button
        const markAllReadBtn = document.getElementById('markAllReadBtn');
        if (markAllReadBtn) {
            markAllReadBtn.addEventListener('click', () => this.markAllAsRead());
        }
    }

    addNotification(notification) {
        // Add to beginning of array
        this.notifications.unshift({
            id: Date.now() + Math.random(),
            read: false,
            ...notification
        });

        // Keep only last 50 notifications
        if (this.notifications.length > 50) {
            this.notifications = this.notifications.slice(0, 50);
        }

        this.unreadCount++;
        this.updateUI();
        this.showToast(notification);
    }

    updateUI() {
        const badge = document.getElementById('notificationBadge');
        const list = document.getElementById('notificationsList');

        if (badge) {
            if (this.unreadCount > 0) {
                badge.textContent = this.unreadCount > 99 ? '99+' : this.unreadCount;
                badge.style.display = 'block';
            } else {
                badge.style.display = 'none';
            }
        }

        if (list) {
            if (this.notifications.length === 0) {
                list.innerHTML = `
                    <div class="text-center text-muted py-3">
                        <i class="bi bi-inbox" style="font-size: 2rem;"></i>
                        <p class="mb-0 mt-2">No notifications</p>
                    </div>
                `;
            } else {
                list.innerHTML = this.notifications.map(notif => this.renderNotification(notif)).join('');
            }
        }
    }

    renderNotification(notification) {
        const iconMap = {
            'info': 'bi-info-circle text-primary',
            'success': 'bi-check-circle text-success',
            'warning': 'bi-exclamation-triangle text-warning',
            'danger': 'bi-x-circle text-danger',
            'low_stock_alert': 'bi-box-seam text-danger',
            'stock_movement': 'bi-arrow-down-circle text-info',
            'claim_assigned': 'bi-person-check text-success',
            'part_added': 'bi-plus-circle text-primary',
            'booking_created': 'bi-calendar-plus text-primary',
            'booking_assigned': 'bi-person-check text-success',
            'booking_completed': 'bi-check-circle text-success',
            'booking_cancelled': 'bi-x-circle text-warning',
            'part_created': 'bi-plus-circle text-success',
            'part_updated': 'bi-pencil text-info',
            'part_deleted': 'bi-trash text-danger',
            'stock_adjusted': 'bi-arrow-repeat text-info',
            'stock_in': 'bi-arrow-down-circle text-success',
            'stock_out': 'bi-arrow-up-circle text-warning',
            'servicecenter_updated': 'bi-building text-info',
            'technician_assigned': 'bi-person-plus text-success',
            'technician_removed': 'bi-person-dash text-warning'
        };

        const icon = iconMap[notification.type] || iconMap['info'];
        const timeAgo = this.getTimeAgo(notification.timestamp);
        const readClass = notification.read ? '' : 'bg-light';
        
        // Determine link based on notification type
        let link = '';
        if (notification.claimId) {
            link = `href="/Claims/Details/${notification.claimId}"`;
        } else if (notification.bookingId) {
            link = `href="/Bookings/Details/${notification.bookingId}"`;
        } else if (notification.partId) {
            link = `href="/Parts/Details/${notification.partId}"`;
        } else if (notification.serviceCenterId) {
            link = `href="/ServiceCenters/Details/${notification.serviceCenterId}"`;
        }

        return `
            <div class="dropdown-item-text ${readClass} p-2 mb-1 rounded notification-item" data-notification-id="${notification.id}" style="cursor: pointer;" ${link ? `onclick="window.location.href='${link.replace('href=', '').replace(/"/g, '')}'"` : ''}>
                <div class="d-flex align-items-start gap-2">
                    <i class="bi ${icon} mt-1" style="font-size: 1.25rem;"></i>
                    <div class="flex-grow-1">
                        <div class="fw-semibold small">${this.escapeHtml(notification.title)}</div>
                        <div class="text-muted small">${this.escapeHtml(notification.message)}</div>
                        <div class="text-muted" style="font-size: 0.7rem;">${timeAgo}</div>
                    </div>
                    ${!notification.read ? '<span class="badge bg-primary rounded-pill" style="font-size: 0.5rem;"></span>' : ''}
                </div>
            </div>
        `;
    }

    getTimeAgo(date) {
        if (!date) return '';
        const now = new Date();
        const diff = now - new Date(date);
        const seconds = Math.floor(diff / 1000);
        const minutes = Math.floor(seconds / 60);
        const hours = Math.floor(minutes / 60);
        const days = Math.floor(hours / 24);

        if (days > 0) return `${days} day${days > 1 ? 's' : ''} ago`;
        if (hours > 0) return `${hours} hour${hours > 1 ? 's' : ''} ago`;
        if (minutes > 0) return `${minutes} minute${minutes > 1 ? 's' : ''} ago`;
        return 'Just now';
    }

    markAsRead(notificationId) {
        const notification = this.notifications.find(n => n.id === notificationId);
        if (notification && !notification.read) {
            notification.read = true;
            this.unreadCount = Math.max(0, this.unreadCount - 1);
            this.updateUI();
        }
    }

    markAllAsRead() {
        this.notifications.forEach(n => n.read = true);
        this.unreadCount = 0;
        this.updateUI();
    }

    showToast(notification) {
        // Use existing toast system if available
        if (window.notificationToast) {
            window.notificationToast.show(notification.message, notification.type);
        } else {
            // Fallback to browser notification
            if (Notification.permission === 'granted') {
                new Notification(notification.title, {
                    body: notification.message,
                    icon: '/favicon.ico'
                });
            }
        }
    }

    loadNotifications() {
        // Load initial notifications from server if needed
        // For now, we'll rely on real-time updates
        this.updateUI();
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize notification manager when DOM is ready
let notificationManager;
document.addEventListener('DOMContentLoaded', () => {
    notificationManager = new NotificationManager();
    window.notificationManager = notificationManager;
});

