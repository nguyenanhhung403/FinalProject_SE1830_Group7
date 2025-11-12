/**
 * Claim Chat Interface with SignalR
 * Handles real-time messaging for warranty claims
 */

class ClaimChat {
    constructor(claimId) {
        this.claimId = claimId;
        this.chatHub = null;
        this.isInitialized = false;
        this.typingTimer = null;
        this.typingDelay = 1000; // Stop typing indicator after 1 second
        this.currentUser = null;
    }

    /**
     * Initialize the chat connection
     */
    async initialize() {
        if (this.isInitialized) {
            console.warn('Chat already initialized');
            return;
        }

        try {
            // Create SignalR connection to chat hub
            this.chatHub = new SignalRConnection('/hubs/chat');

            // Register event handlers
            this.registerEventHandlers();

            // Start the connection
            await this.chatHub.start();

            // Join the claim chat room
            await this.joinChat();

            this.isInitialized = true;
            console.log(`Chat initialized for claim ${this.claimId}`);

            // Set up UI event handlers
            this.setupUIHandlers();

        } catch (error) {
            console.error('Failed to initialize chat:', error);
        }
    }

    /**
     * Register SignalR event handlers
     */
    registerEventHandlers() {
        // Handle incoming messages
        this.chatHub.on('ReceiveMessage', (data) => {
            this.handleIncomingMessage(data);
        });

        // Handle user joined
        this.chatHub.on('UserJoined', (username, timestamp) => {
            this.showSystemMessage(`${username} joined the chat`);
        });

        // Handle user left
        this.chatHub.on('UserLeft', (username, timestamp) => {
            this.showSystemMessage(`${username} left the chat`);
        });

        // Handle typing indicator
        this.chatHub.on('UserIsTyping', (username) => {
            this.showTypingIndicator(username);
        });

        this.chatHub.on('UserStoppedTyping', (username) => {
            this.hideTypingIndicator(username);
        });

        // Handle messages read
        this.chatHub.on('MessagesRead', (userId, timestamp) => {
            this.handleMessagesRead(userId);
        });
    }

    /**
     * Join the claim chat room
     */
    async joinChat() {
        try {
            await this.chatHub.invoke('JoinClaimChat', this.claimId);
            console.log(`Joined chat for claim ${this.claimId}`);
        } catch (error) {
            console.error('Failed to join chat:', error);
        }
    }

    /**
     * Leave the claim chat room
     */
    async leaveChat() {
        try {
            await this.chatHub.invoke('LeaveClaimChat', this.claimId);
            console.log(`Left chat for claim ${this.claimId}`);
        } catch (error) {
            console.error('Failed to leave chat:', error);
        }
    }

    /**
     * Setup UI event handlers
     */
    setupUIHandlers() {
        const messageInput = document.getElementById('chat-message-input');
        const sendButton = document.getElementById('chat-send-button');

        if (messageInput) {
            // Send message on Enter key
            messageInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    this.sendMessage();
                }
            });

            // Typing indicator
            messageInput.addEventListener('input', () => {
                this.handleTyping();
            });
        }

        if (sendButton) {
            sendButton.addEventListener('click', () => {
                this.sendMessage();
            });
        }
    }

    /**
     * Send a message
     */
    async sendMessage() {
        const messageInput = document.getElementById('chat-message-input');
        if (!messageInput) return;

        const message = messageInput.value.trim();
        if (!message) return;

        try {
            // Send via SignalR (hub will broadcast to all users)
            await this.chatHub.invoke('SendMessage', this.claimId, message);

            // Clear input
            messageInput.value = '';

            // Stop typing indicator
            await this.chatHub.invoke('UserStoppedTyping', this.claimId);

        } catch (error) {
            console.error('Failed to send message:', error);
            this.showToast('Failed to send message', 'error');
        }
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
        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '9999';
        document.body.appendChild(container);
        return container;
    }

    /**
     * Handle typing indicator
     */
    handleTyping() {
        // Clear existing timer
        if (this.typingTimer) {
            clearTimeout(this.typingTimer);
        }

        // Notify others that user is typing
        this.chatHub.invoke('UserTyping', this.claimId);

        // Set timer to stop typing indicator
        this.typingTimer = setTimeout(() => {
            this.chatHub.invoke('UserStoppedTyping', this.claimId);
        }, this.typingDelay);
    }

    /**
     * Handle incoming message
     */
    handleIncomingMessage(data) {
        this.appendMessage(data);

        // Mark as read if the chat window is visible
        const chatContainer = document.getElementById('chat-messages');
        if (chatContainer && this.isVisible(chatContainer)) {
            this.markMessagesAsRead();
        }

        // Play notification sound if message is from another user
        if (data.UserId !== this.currentUser?.userId) {
            this.playMessageSound();
        }
    }

    /**
     * Append a message to the chat UI
     */
    appendMessage(data) {
        const chatMessages = document.getElementById('chat-messages');
        if (!chatMessages) return;

        const isOwnMessage = data.UserId === this.currentUser?.userId;

        const messageDiv = document.createElement('div');
        messageDiv.className = `chat-message ${isOwnMessage ? 'own-message' : 'other-message'} message-fade-in`;
        messageDiv.setAttribute('data-message-id', data.MessageId || Date.now());

        const timestamp = new Date(data.Timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        messageDiv.innerHTML = `
            <div class="message-header">
                <strong>${data.Username || 'Unknown'}</strong>
                <small class="text-muted">${timestamp}</small>
            </div>
            <div class="message-content">${this.escapeHtml(data.Message)}</div>
        `;

        chatMessages.appendChild(messageDiv);

        // Scroll to bottom
        this.scrollToBottom();

        // Remove animation class
        setTimeout(() => messageDiv.classList.remove('message-fade-in'), 300);
    }

    /**
     * Show system message
     */
    showSystemMessage(message) {
        const chatMessages = document.getElementById('chat-messages');
        if (!chatMessages) return;

        const messageDiv = document.createElement('div');
        messageDiv.className = 'chat-system-message message-fade-in';
        messageDiv.innerHTML = `<small class="text-muted"><i class="bi bi-info-circle"></i> ${message}</small>`;

        chatMessages.appendChild(messageDiv);
        this.scrollToBottom();
    }

    /**
     * Show typing indicator
     */
    showTypingIndicator(username) {
        const typingIndicator = document.getElementById('typing-indicator');
        if (!typingIndicator) return;

        typingIndicator.textContent = `${username} is typing...`;
        typingIndicator.classList.remove('d-none');
    }

    /**
     * Hide typing indicator
     */
    hideTypingIndicator(username) {
        const typingIndicator = document.getElementById('typing-indicator');
        if (!typingIndicator) return;

        typingIndicator.classList.add('d-none');
    }

    /**
     * Mark messages as read
     */
    async markMessagesAsRead() {
        try {
            await this.chatHub.invoke('MarkMessagesAsRead', this.claimId);
        } catch (error) {
            console.error('Failed to mark messages as read:', error);
        }
    }

    /**
     * Handle messages read event
     */
    handleMessagesRead(userId) {
        console.log(`User ${userId} read messages`);
    }

    /**
     * Scroll chat to bottom
     */
    scrollToBottom() {
        const chatMessages = document.getElementById('chat-messages');
        if (chatMessages) {
            chatMessages.scrollTop = chatMessages.scrollHeight;
        }
    }

    /**
     * Check if element is visible
     */
    isVisible(element) {
        return element.offsetParent !== null;
    }

    /**
     * Play message notification sound
     */
    playMessageSound() {
        try {
            const context = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = context.createOscillator();
            const gainNode = context.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(context.destination);

            oscillator.frequency.value = 600;
            oscillator.type = 'sine';

            gainNode.gain.setValueAtTime(0.05, context.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, context.currentTime + 0.1);

            oscillator.start(context.currentTime);
            oscillator.stop(context.currentTime + 0.1);
        } catch (e) {
            // Silently fail if audio is not supported
        }
    }

    /**
     * Escape HTML to prevent XSS
     */
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Stop the chat connection
     */
    async stop() {
        await this.leaveChat();

        if (this.chatHub) {
            await this.chatHub.stop();
            this.isInitialized = false;
            console.log('Chat stopped');
        }
    }
}

// Export for global use
window.ClaimChat = ClaimChat;

