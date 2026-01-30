class NotificationSystem {
    constructor(options = {}) {
        this.options = {
            position: 'top-right', // top-left, top-right, bottom-left, bottom-right, top-center, bottom-center
            maxNotifications: 5,
            duration: 5000,
            showProgress: true,
            pauseOnHover: true,
            clickToClose: true,
            ...options
        };

        this.notifications = [];
        this.container = null;
        this.notificationId = 0;

        this.init();
    }

    init() {
        this.createContainer();
        this.addStyles();
    }

    createContainer() {
        this.container = document.createElement('div');
        this.container.className = `notification-container notification-${this.options.position}`;
        document.body.appendChild(this.container);
    }

    addStyles() {
        const styleId = 'notification-system-styles';
        
        if (document.getElementById(styleId)) {
            return; // Styles already added
        }

        const style = document.createElement('style');
        style.id = styleId;
        style.textContent = `
            .notification-container {
                position: fixed;
                z-index: 9999;
                pointer-events: none;
                max-width: 400px;
                width: 100%;
            }

            .notification-top-left {
                top: 20px;
                left: 20px;
            }

            .notification-top-right {
                top: 20px;
                right: 20px;
            }

            .notification-top-center {
                top: 20px;
                left: 50%;
                transform: translateX(-50%);
            }

            .notification-bottom-left {
                bottom: 20px;
                left: 20px;
            }

            .notification-bottom-right {
                bottom: 20px;
                right: 20px;
            }

            .notification-bottom-center {
                bottom: 20px;
                left: 50%;
                transform: translateX(-50%);
            }

            .notification {
                background: white;
                border-radius: 8px;
                box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
                margin-bottom: 10px;
                overflow: hidden;
                pointer-events: auto;
                animation: notificationSlideIn 0.3s ease-out;
                position: relative;
            }

            @keyframes notificationSlideIn {
                from {
                    opacity: 0;
                    transform: translateY(-20px);
                }
                to {
                    opacity: 1;
                    transform: translateY(0);
                }
            }

            .notification.removing {
                animation: notificationSlideOut 0.3s ease-out forwards;
            }

            @keyframes notificationSlideOut {
                from {
                    opacity: 1;
                    transform: translateX(0);
                }
                to {
                    opacity: 0;
                    transform: translateX(100%);
                }
            }

            .notification-header {
                display: flex;
                align-items: center;
                padding: 12px 16px;
                border-bottom: 1px solid rgba(0, 0, 0, 0.1);
            }

            .notification-icon {
                margin-right: 12px;
                font-size: 18px;
                flex-shrink: 0;
            }

            .notification-title {
                font-weight: 600;
                font-size: 14px;
                flex: 1;
                margin: 0;
            }

            .notification-close {
                background: none;
                border: none;
                font-size: 18px;
                cursor: pointer;
                padding: 0;
                margin-left: 8px;
                opacity: 0.6;
                transition: opacity 0.2s;
            }

            .notification-close:hover {
                opacity: 1;
            }

            .notification-body {
                padding: 12px 16px;
                font-size: 14px;
                line-height: 1.4;
            }

            .notification-progress {
                position: absolute;
                bottom: 0;
                left: 0;
                height: 3px;
                background: rgba(0, 0, 0, 0.1);
                width: 100%;
            }

            .notification-progress-bar {
                height: 100%;
                background: currentColor;
                opacity: 0.3;
                transition: width linear;
            }

            .notification-success {
                border-left: 4px solid #28a745;
                color: #155724;
            }

            .notification-success .notification-icon {
                color: #28a745;
            }

            .notification-error {
                border-left: 4px solid #dc3545;
                color: #721c24;
            }

            .notification-error .notification-icon {
                color: #dc3545;
            }

            .notification-warning {
                border-left: 4px solid #ffc107;
                color: #856404;
            }

            .notification-warning .notification-icon {
                color: #ffc107;
            }

            .notification-info {
                border-left: 4px solid #17a2b8;
                color: #0c5460;
            }

            .notification-info .notification-icon {
                color: #17a2b8;
            }

            .loading-modal {
                text-align: center;
                padding: 20px;
            }

            .spinner {
                border: 3px solid #f3f3f3;
                border-top: 3px solid #007bff;
                border-radius: 50%;
                width: 40px;
                height: 40px;
                animation: spin 1s linear infinite;
                margin: 0 auto 15px;
            }

            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }
        `;

        document.head.appendChild(style);
    }

    show(message, type = 'info', options = {}) {
        const notification = {
            id: ++this.notificationId,
            message,
            type,
            title: options.title || this.getDefaultTitle(type),
            duration: options.duration !== undefined ? options.duration : this.options.duration,
            persistent: options.persistent || false,
            actions: options.actions || [],
            onClick: options.onClick || null
        };

        // Remove oldest notifications if max limit reached
        if (this.notifications.length >= this.options.maxNotifications) {
            this.remove(this.notifications[0].id);
        }

        this.notifications.push(notification);
        this.renderNotification(notification);

        if (!notification.persistent && notification.duration > 0) {
            this.startTimer(notification);
        }

        return notification.id;
    }

    renderNotification(notification) {
        const element = document.createElement('div');
        element.className = `notification notification-${notification.type}`;
        element.dataset.notificationId = notification.id;

        const icon = this.getIcon(notification.type);
        
        element.innerHTML = `
            <div class="notification-header">
                <span class="notification-icon">${icon}</span>
                <h4 class="notification-title">${notification.title}</h4>
                ${this.options.clickToClose ? '<button class="notification-close">&times;</button>' : ''}
            </div>
            <div class="notification-body">${notification.message}</div>
            ${this.options.showProgress && !notification.persistent ? this.createProgressBar(notification) : ''}
        `;

        // Add actions if provided
        if (notification.actions.length > 0) {
            const actionsContainer = document.createElement('div');
            actionsContainer.className = 'notification-actions';
            actionsContainer.style.cssText = 'padding: 8px 16px; border-top: 1px solid rgba(0,0,0,0.1);';
            
            notification.actions.forEach(action => {
                const button = document.createElement('button');
                button.className = `btn btn-sm ${action.className || 'btn-secondary'}`;
                button.textContent = action.text;
                button.style.cssText = 'margin-right: 8px;';
                button.addEventListener('click', () => {
                    if (action.handler) {
                        action.handler(notification);
                    }
                    this.remove(notification.id);
                });
                actionsContainer.appendChild(button);
            });
            
            element.appendChild(actionsContainer);
        }

        // Bind events
        this.bindNotificationEvents(element, notification);

        // Add to container
        this.container.appendChild(element);

        // Store element reference
        notification.element = element;
    }

    createProgressBar(notification) {
        return `
            <div class="notification-progress">
                <div class="notification-progress-bar" style="width: 100%; transition-duration: ${notification.duration}ms;"></div>
            </div>
        `;
    }

    bindNotificationEvents(element, notification) {
        // Close button
        const closeBtn = element.querySelector('.notification-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => this.remove(notification.id));
        }

        // Click handler
        if (notification.onClick) {
            element.addEventListener('click', (e) => {
                if (!e.target.closest('.notification-close') && !e.target.closest('.notification-actions button')) {
                    notification.onClick(notification);
                }
            });
            element.style.cursor = 'pointer';
        }

        // Pause on hover
        if (this.options.pauseOnHover && !notification.persistent) {
            element.addEventListener('mouseenter', () => this.pauseTimer(notification));
            element.addEventListener('mouseleave', () => this.resumeTimer(notification));
        }
    }

    startTimer(notification) {
        notification.startTime = Date.now();
        notification.remainingTime = notification.duration;
        notification.timer = setTimeout(() => this.remove(notification.id), notification.duration);
    }

    pauseTimer(notification) {
        if (notification.timer) {
            clearTimeout(notification.timer);
            notification.remainingTime = notification.duration - (Date.now() - notification.startTime);
            
            const progressBar = notification.element?.querySelector('.notification-progress-bar');
            if (progressBar) {
                progressBar.style.transition = 'none';
                const currentWidth = (notification.remainingTime / notification.duration) * 100;
                progressBar.style.width = `${currentWidth}%`;
            }
        }
    }

    resumeTimer(notification) {
        if (notification.remainingTime > 0) {
            const progressBar = notification.element?.querySelector('.notification-progress-bar');
            if (progressBar) {
                progressBar.style.transition = `width linear ${notification.remainingTime}ms`;
                progressBar.style.width = '0%';
            }
            
            notification.timer = setTimeout(() => this.remove(notification.id), notification.remainingTime);
        }
    }

    remove(notificationId) {
        const index = this.notifications.findIndex(n => n.id === notificationId);
        if (index === -1) return;

        const notification = this.notifications[index];
        
        if (notification.timer) {
            clearTimeout(notification.timer);
        }

        if (notification.element) {
            notification.element.classList.add('removing');
            
            setTimeout(() => {
                if (notification.element && notification.element.parentNode) {
                    notification.element.parentNode.removeChild(notification.element);
                }
            }, 300);
        }

        this.notifications.splice(index, 1);
    }

    clear() {
        this.notifications.forEach(notification => {
            if (notification.timer) {
                clearTimeout(notification.timer);
            }
            if (notification.element && notification.element.parentNode) {
                notification.element.parentNode.removeChild(notification.element);
            }
        });
        this.notifications = [];
    }

    getDefaultTitle(type) {
        const titles = {
            success: 'Success',
            error: 'Error',
            warning: 'Warning',
            info: 'Information'
        };
        return titles[type] || 'Notification';
    }

    getIcon(type) {
        const icons = {
            success: '✓',
            error: '✕',
            warning: '⚠',
            info: 'ℹ'
        };
        return icons[type] || 'ℹ';
    }

    // Convenience methods
    success(message, options = {}) {
        return this.show(message, 'success', options);
    }

    error(message, options = {}) {
        return this.show(message, 'error', options);
    }

    warning(message, options = {}) {
        return this.show(message, 'warning', options);
    }

    info(message, options = {}) {
        return this.show(message, 'info', options);
    }

    // Static instance for global use
    static getInstance() {
        if (!window.notificationSystem) {
            window.notificationSystem = new NotificationSystem();
        }
        return window.notificationSystem;
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = NotificationSystem;
}
