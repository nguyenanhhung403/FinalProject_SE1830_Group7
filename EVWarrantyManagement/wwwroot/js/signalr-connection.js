/**
 * SignalR Connection Manager
 * Handles connection, reconnection, and event handling for SignalR hubs
 */

class SignalRConnection {
    constructor(hubUrl) {
        this.hubUrl = hubUrl;
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 10;
        this.reconnectDelay = 3000; // Start with 3 seconds
        this.eventHandlers = new Map();
    }

    /**
     * Initialize and start the connection
     */
    async start() {
        try {
            // Build the connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(this.hubUrl)
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        // Exponential backoff: 0, 2, 10, 30 seconds, then 30 seconds
                        if (retryContext.elapsedMilliseconds < 60000) {
                            return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                        } else {
                            // After 1 minute, give up
                            return null;
                        }
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Set up event handlers
            this.connection.onreconnecting(error => {
                console.warn(`Connection lost. Reconnecting... ${error}`);
                this.isConnected = false;
                this.onConnectionStateChanged('reconnecting');
            });

            this.connection.onreconnected(connectionId => {
                console.log(`Reconnected. Connection ID: ${connectionId}`);
                this.isConnected = true;
                this.reconnectAttempts = 0;
                this.onConnectionStateChanged('connected');
            });

            this.connection.onclose(error => {
                console.error(`Connection closed. ${error}`);
                this.isConnected = false;
                this.onConnectionStateChanged('disconnected');

                // Attempt manual reconnect after delay
                if (this.reconnectAttempts < this.maxReconnectAttempts) {
                    setTimeout(() => this.manualReconnect(), this.reconnectDelay);
                }
            });

            // Register all event handlers
            this.eventHandlers.forEach((handler, eventName) => {
                this.connection.on(eventName, handler);
            });

            // Start the connection
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;
            console.log(`SignalR connected to ${this.hubUrl}`);
            this.onConnectionStateChanged('connected');

            return this.connection;
        } catch (error) {
            console.error('Error starting SignalR connection:', error);
            this.onConnectionStateChanged('error');

            // Attempt to reconnect
            if (this.reconnectAttempts < this.maxReconnectAttempts) {
                setTimeout(() => this.manualReconnect(), this.reconnectDelay);
            }

            throw error;
        }
    }

    /**
     * Manual reconnection with exponential backoff
     */
    async manualReconnect() {
        this.reconnectAttempts++;
        this.reconnectDelay = Math.min(this.reconnectDelay * 2, 30000); // Max 30 seconds

        console.log(`Reconnect attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts}`);

        try {
            await this.start();
        } catch (error) {
            console.error(`Reconnect attempt ${this.reconnectAttempts} failed:`, error);
        }
    }

    /**
     * Register an event handler
     */
    on(eventName, handler) {
        this.eventHandlers.set(eventName, handler);
        if (this.connection) {
            this.connection.on(eventName, handler);
        }
    }

    /**
     * Unregister an event handler
     */
    off(eventName) {
        this.eventHandlers.delete(eventName);
        if (this.connection) {
            this.connection.off(eventName);
        }
    }

    /**
     * Invoke a hub method
     */
    async invoke(methodName, ...args) {
        if (!this.isConnected || !this.connection) {
            console.warn('Cannot invoke method: Not connected');
            return null;
        }

        try {
            return await this.connection.invoke(methodName, ...args);
        } catch (error) {
            console.error(`Error invoking ${methodName}:`, error);
            throw error;
        }
    }

    /**
     * Send a message without waiting for response
     */
    async send(methodName, ...args) {
        if (!this.isConnected || !this.connection) {
            console.warn('Cannot send message: Not connected');
            return;
        }

        try {
            await this.connection.send(methodName, ...args);
        } catch (error) {
            console.error(`Error sending ${methodName}:`, error);
        }
    }

    /**
     * Stop the connection
     */
    async stop() {
        if (this.connection) {
            await this.connection.stop();
            this.isConnected = false;
            this.onConnectionStateChanged('disconnected');
        }
    }

    /**
     * Override this method to handle connection state changes
     */
    onConnectionStateChanged(state) {
        // To be overridden by subclasses or instances
        console.log(`Connection state changed: ${state}`);
    }

    /**
     * Get current connection state
     */
    getState() {
        if (!this.connection) return 'disconnected';

        switch (this.connection.state) {
            case signalR.HubConnectionState.Connected:
                return 'connected';
            case signalR.HubConnectionState.Connecting:
                return 'connecting';
            case signalR.HubConnectionState.Reconnecting:
                return 'reconnecting';
            case signalR.HubConnectionState.Disconnected:
                return 'disconnected';
            default:
                return 'unknown';
        }
    }
}

// Export for use in other scripts
window.SignalRConnection = SignalRConnection;
