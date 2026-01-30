class Modal {
    constructor(options = {}) {
        this.options = {
            title: '',
            content: '',
            size: 'medium', // small, medium, large, full
            closable: true,
            backdrop: true,
            keyboard: true,
            showFooter: true,
            confirmText: 'Confirm',
            cancelText: 'Cancel',
            onConfirm: null,
            onCancel: null,
            onShow: null,
            onHide: null,
            ...options
        };

        this.modal = null;
        this.isVisible = false;
        this.createModal();
    }

    createModal() {
        // Create modal container
        this.modal = document.createElement('div');
        this.modal.className = 'modal';
        this.modal.setAttribute('role', 'dialog');
        this.modal.setAttribute('aria-modal', 'true');
        this.modal.setAttribute('aria-labelledby', 'modal-title');

        this.modal.innerHTML = `
            <div class="modal-backdrop ${this.options.backdrop ? '' : 'transparent'}"></div>
            <div class="modal-container modal-${this.options.size}">
                <div class="modal-header">
                    <h3 id="modal-title" class="modal-title">${this.options.title}</h3>
                    ${this.options.closable ? '<button class="modal-close" aria-label="Close">&times;</button>' : ''}
                </div>
                <div class="modal-body">
                    ${this.options.content}
                </div>
                ${this.options.showFooter ? this.createFooter() : ''}
            </div>
        `;

        // Add to body
        document.body.appendChild(this.modal);

        // Bind events
        this.bindEvents();
    }

    createFooter() {
        return `
            <div class="modal-footer">
                <button class="btn btn-secondary modal-cancel">${this.options.cancelText}</button>
                <button class="btn btn-primary modal-confirm">${this.options.confirmText}</button>
            </div>
        `;
    }

    bindEvents() {
        // Close button
        const closeBtn = this.modal.querySelector('.modal-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => this.hide());
        }

        // Cancel button
        const cancelBtn = this.modal.querySelector('.modal-cancel');
        if (cancelBtn) {
            cancelBtn.addEventListener('click', () => {
                this.handleCancel();
            });
        }

        // Confirm button
        const confirmBtn = this.modal.querySelector('.modal-confirm');
        if (confirmBtn) {
            confirmBtn.addEventListener('click', () => {
                this.handleConfirm();
            });
        }

        // Backdrop click
        if (this.options.backdrop) {
            const backdrop = this.modal.querySelector('.modal-backdrop');
            backdrop.addEventListener('click', () => {
                if (this.options.closable) {
                    this.hide();
                }
            });
        }

        // Keyboard events
        if (this.options.keyboard) {
            document.addEventListener('keydown', (e) => {
                if (this.isVisible) {
                    if (e.key === 'Escape' && this.options.closable) {
                        this.hide();
                    }
                    if (e.key === 'Enter' && !e.shiftKey) {
                        const activeElement = document.activeElement;
                        if (activeElement && activeElement.tagName !== 'TEXTAREA') {
                            this.handleConfirm();
                        }
                    }
                }
            });
        }

        // Focus trap
        this.modal.addEventListener('keydown', (e) => {
            if (e.key === 'Tab') {
                this.trapFocus(e);
            }
        });
    }

    show() {
        if (this.isVisible) return;

        this.isVisible = true;
        this.modal.classList.add('show');
        document.body.classList.add('modal-open');

        // Focus first focusable element
        this.focusFirstElement();

        // Trigger onShow callback
        if (this.options.onShow) {
            this.options.onShow(this);
        }
    }

    hide() {
        if (!this.isVisible) return;

        this.isVisible = false;
        this.modal.classList.remove('show');
        document.body.classList.remove('modal-open');

        // Return focus to original element
        if (this.originalFocus) {
            this.originalFocus.focus();
        }

        // Trigger onHide callback
        if (this.options.onHide) {
            this.options.onHide(this);
        }
    }

    handleConfirm() {
        if (this.options.onConfirm) {
            const result = this.options.onConfirm(this);
            
            // If onConfirm returns false, don't close the modal
            if (result !== false) {
                this.hide();
            }
        } else {
            this.hide();
        }
    }

    handleCancel() {
        if (this.options.onCancel) {
            this.options.onCancel(this);
        }
        this.hide();
    }

    setTitle(title) {
        this.options.title = title;
        const titleElement = this.modal.querySelector('.modal-title');
        if (titleElement) {
            titleElement.textContent = title;
        }
    }

    setContent(content) {
        this.options.content = content;
        const bodyElement = this.modal.querySelector('.modal-body');
        if (bodyElement) {
            bodyElement.innerHTML = content;
        }
    }

    setSize(size) {
        const container = this.modal.querySelector('.modal-container');
        if (container) {
            container.className = `modal-container modal-${size}`;
        }
        this.options.size = size;
    }

    focusFirstElement() {
        this.originalFocus = document.activeElement;
        const focusableElements = this.getFocusableElements();
        
        if (focusableElements.length > 0) {
            focusableElements[0].focus();
        }
    }

    trapFocus(e) {
        const focusableElements = this.getFocusableElements();
        const firstElement = focusableElements[0];
        const lastElement = focusableElements[focusableElements.length - 1];

        if (e.shiftKey) {
            if (document.activeElement === firstElement) {
                lastElement.focus();
                e.preventDefault();
            }
        } else {
            if (document.activeElement === lastElement) {
                firstElement.focus();
                e.preventDefault();
            }
        }
    }

    getFocusableElements() {
        const focusableSelectors = [
            'button:not([disabled])',
            'input:not([disabled])',
            'select:not([disabled])',
            'textarea:not([disabled])',
            'a[href]',
            '[tabindex]:not([tabindex="-1"])'
        ].join(', ');

        return Array.from(this.modal.querySelectorAll(focusableSelectors))
            .filter(element => {
                const style = window.getComputedStyle(element);
                return style.display !== 'none' && style.visibility !== 'hidden';
            });
    }

    destroy() {
        if (this.modal) {
            this.hide();
            this.modal.remove();
            this.modal = null;
        }
    }

    // Static methods for common modal types
    static alert(message, title = 'Alert', options = {}) {
        return new Promise((resolve) => {
            const modal = new Modal({
                title,
                content: `<p>${message}</p>`,
                showFooter: true,
                confirmText: 'OK',
                cancelText: null,
                closable: false,
                onConfirm: () => {
                    modal.destroy();
                    resolve();
                },
                ...options
            });
            
            modal.show();
        });
    }

    static confirm(message, title = 'Confirm', options = {}) {
        return new Promise((resolve) => {
            const modal = new Modal({
                title,
                content: `<p>${message}</p>`,
                showFooter: true,
                onConfirm: () => {
                    modal.destroy();
                    resolve(true);
                },
                onCancel: () => {
                    modal.destroy();
                    resolve(false);
                },
                ...options
            });
            
            modal.show();
        });
    }

    static prompt(message, defaultValue = '', title = 'Prompt', options = {}) {
        return new Promise((resolve) => {
            const inputId = 'modal-prompt-input';
            const content = `
                <p>${message}</p>
                <input type="text" id="${inputId}" class="form-control" value="${defaultValue}" />
            `;

            const modal = new Modal({
                title,
                content,
                showFooter: true,
                onConfirm: () => {
                    const input = modal.modal.querySelector(`#${inputId}`);
                    const value = input ? input.value : '';
                    modal.destroy();
                    resolve(value);
                },
                onCancel: () => {
                    modal.destroy();
                    resolve(null);
                },
                onShow: () => {
                    const input = modal.modal.querySelector(`#${inputId}`);
                    if (input) {
                        input.focus();
                        input.select();
                    }
                },
                ...options
            });
            
            modal.show();
        });
    }

    static loading(message = 'Loading...', title = 'Please Wait', options = {}) {
        const content = `
            <div class="loading-modal">
                <div class="spinner"></div>
                <p>${message}</p>
            </div>
        `;

        const modal = new Modal({
            title,
            content,
            showFooter: false,
            closable: false,
            backdrop: true,
            ...options
        });
        
        modal.show();
        return modal;
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = Modal;
}
