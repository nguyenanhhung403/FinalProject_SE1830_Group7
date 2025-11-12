/**
 * Simple Notification Counter Manager
 * Manages notification badge count (resets on page refresh)
 */

class NotificationManager {
    constructor() {
        this.count = 0;
        this.badgeElement = null;
        this.bellElement = null;
    }

    /**
     * Initialize the notification manager
     */
    initialize() {
        this.badgeElement = document.getElementById('notification-badge');
        this.bellElement = document.getElementById('notification-bell');

        if (!this.badgeElement || !this.bellElement) {
            console.warn('Notification badge or bell element not found');
            return;
        }

        this.updateBadge();
        console.log('Notification manager initialized');
    }

    /**
     * Increment notification count
     */
    increment() {
        this.count++;
        this.updateBadge();
        this.animateBell();
        console.log(`Notification count: ${this.count}`);
    }

    /**
     * Update the badge display
     */
    updateBadge() {
        if (!this.badgeElement) return;

        if (this.count > 0) {
            this.badgeElement.textContent = this.count > 99 ? '99+' : this.count;
            this.badgeElement.style.display = 'inline-block';
        } else {
            this.badgeElement.style.display = 'none';
        }
    }

    /**
     * Animate the bell when new notification arrives
     */
    animateBell() {
        if (!this.bellElement) return;

        // Add shake animation
        this.bellElement.classList.add('notification-bell-shake');

        // Remove animation class after it completes
        setTimeout(() => {
            this.bellElement.classList.remove('notification-bell-shake');
        }, 500);
    }

    /**
     * Get current count
     */
    getCount() {
        return this.count;
    }

    /**
     * Reset count (optional - for future use)
     */
    reset() {
        this.count = 0;
        this.updateBadge();
    }
}

// Create global instance
window.notificationManager = new NotificationManager();

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    window.notificationManager.initialize();
});
