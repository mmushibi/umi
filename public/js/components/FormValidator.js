class FormValidator {
    constructor(formId, rules = {}) {
        this.form = document.getElementById(formId);
        this.rules = rules;
        this.errors = {};
        this.isValid = false;
        
        this.init();
    }

    init() {
        if (!this.form) {
            console.error(`Form with id '${formId}' not found`);
            return;
        }

        this.bindEvents();
        this.setupRealtimeValidation();
    }

    bindEvents() {
        this.form.addEventListener('submit', (e) => {
            e.preventDefault();
            this.validate();
            
            if (this.isValid) {
                this.onSubmit();
            } else {
                this.showErrors();
            }
        });

        // Add blur event listeners for real-time validation
        this.form.querySelectorAll('input, select, textarea').forEach(field => {
            field.addEventListener('blur', () => this.validateField(field));
            field.addEventListener('input', () => this.clearFieldError(field));
        });
    }

    setupRealtimeValidation() {
        // Add custom validation attributes
        this.form.querySelectorAll('input, select, textarea').forEach(field => {
            const fieldName = field.name || field.id;
            
            if (this.rules[fieldName]) {
                field.setAttribute('data-validate', fieldName);
            }
        });
    }

    validate() {
        this.errors = {};
        this.isValid = true;

        this.form.querySelectorAll('input, select, textarea').forEach(field => {
            const fieldName = field.name || field.id;
            if (this.rules[fieldName]) {
                const fieldErrors = this.validateField(field);
                if (fieldErrors.length > 0) {
                    this.errors[fieldName] = fieldErrors;
                    this.isValid = false;
                }
            }
        });

        return this.isValid;
    }

    validateField(field) {
        const fieldName = field.name || field.id;
        const value = field.value.trim();
        const fieldRules = this.rules[fieldName];
        const errors = [];

        if (!fieldRules) return errors;

        // Required validation
        if (fieldRules.required && !value) {
            errors.push(fieldRules.requiredMessage || `${fieldName} is required`);
        }

        if (!value && fieldRules.required) {
            return errors; // Skip other validations if field is required but empty
        }

        // Email validation
        if (fieldRules.email && value) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(value)) {
                errors.push(fieldRules.emailMessage || 'Please enter a valid email address');
            }
        }

        // Phone validation
        if (fieldRules.phone && value) {
            const phoneRegex = /^[\+]?[1-9][\d]{0,15}$/;
            if (!phoneRegex.test(value)) {
                errors.push(fieldRules.phoneMessage || 'Please enter a valid phone number');
            }
        }

        // Min length validation
        if (fieldRules.minLength && value.length < fieldRules.minLength) {
            errors.push(fieldRules.minLengthMessage || 
                `${fieldName} must be at least ${fieldRules.minLength} characters long`);
        }

        // Max length validation
        if (fieldRules.maxLength && value.length > fieldRules.maxLength) {
            errors.push(fieldRules.maxLengthMessage || 
                `${fieldName} must not exceed ${fieldRules.maxLength} characters`);
        }

        // Pattern validation
        if (fieldRules.pattern && value) {
            const regex = new RegExp(fieldRules.pattern);
            if (!regex.test(value)) {
                errors.push(fieldRules.patternMessage || `${fieldName} format is invalid`);
            }
        }

        // Custom validation
        if (fieldRules.custom && typeof fieldRules.custom === 'function') {
            const customError = fieldRules.custom(value, field);
            if (customError) {
                errors.push(customError);
            }
        }

        // Password strength validation
        if (fieldRules.password && value) {
            const passwordErrors = this.validatePassword(value);
            errors.push(...passwordErrors);
        }

        // Match field validation
        if (fieldRules.match && value) {
            const matchField = this.form.querySelector(`[name="${fieldRules.match}"], [id="${fieldRules.match}"]`);
            if (matchField && matchField.value !== value) {
                errors.push(fieldRules.matchMessage || `This field must match ${fieldRules.match}`);
            }
        }

        return errors;
    }

    validatePassword(password) {
        const errors = [];
        
        if (password.length < 8) {
            errors.push('Password must be at least 8 characters long');
        }
        
        if (!/[A-Z]/.test(password)) {
            errors.push('Password must contain at least one uppercase letter');
        }
        
        if (!/[a-z]/.test(password)) {
            errors.push('Password must contain at least one lowercase letter');
        }
        
        if (!/\d/.test(password)) {
            errors.push('Password must contain at least one number');
        }
        
        if (!/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
            errors.push('Password must contain at least one special character');
        }
        
        return errors;
    }

    showErrors() {
        // Clear all existing errors
        this.clearAllErrors();

        // Show field-specific errors
        Object.entries(this.errors).forEach(([fieldName, fieldErrors]) => {
            const field = this.form.querySelector(`[name="${fieldName}"], [id="${fieldName}"]`);
            if (field) {
                this.showFieldErrors(field, fieldErrors);
            }
        });

        // Show general error message
        if (this.hasGeneralError()) {
            this.showGeneralError();
        }
    }

    showFieldErrors(field, errors) {
        // Add error class to field
        field.classList.add('error');
        
        // Create or update error message container
        let errorContainer = field.parentNode.querySelector('.field-error');
        if (!errorContainer) {
            errorContainer = document.createElement('div');
            errorContainer.className = 'field-error';
            field.parentNode.appendChild(errorContainer);
        }
        
        errorContainer.innerHTML = errors.map(error => `<span class="error-message">${error}</span>`).join('');
    }

    clearFieldError(field) {
        field.classList.remove('error');
        const errorContainer = field.parentNode.querySelector('.field-error');
        if (errorContainer) {
            errorContainer.remove();
        }
    }

    clearAllErrors() {
        this.form.querySelectorAll('.error').forEach(field => {
            field.classList.remove('error');
        });
        
        this.form.querySelectorAll('.field-error').forEach(container => {
            container.remove();
        });
        
        const generalError = this.form.querySelector('.general-error');
        if (generalError) {
            generalError.remove();
        }
    }

    showGeneralError() {
        let errorContainer = this.form.querySelector('.general-error');
        if (!errorContainer) {
            errorContainer = document.createElement('div');
            errorContainer.className = 'general-error alert alert-danger';
            this.form.insertBefore(errorContainer, this.form.firstChild);
        }
        
        errorContainer.textContent = 'Please correct the errors below and try again.';
    }

    hasGeneralError() {
        return Object.keys(this.errors).length > 0;
    }

    getFormData() {
        const formData = new FormData(this.form);
        const data = {};
        
        for (let [key, value] of formData.entries()) {
            data[key] = value;
        }
        
        return data;
    }

    reset() {
        this.form.reset();
        this.clearAllErrors();
        this.errors = {};
        this.isValid = false;
    }

    setRules(rules) {
        this.rules = rules;
        this.setupRealtimeValidation();
    }

    addRule(fieldName, rule) {
        this.rules[fieldName] = { ...this.rules[fieldName], ...rule };
        this.setupRealtimeValidation();
    }

    removeRule(fieldName) {
        delete this.rules[fieldName];
        this.setupRealtimeValidation();
    }

    onSubmit() {
        // This method should be overridden by the implementing class
        console.log('Form submitted successfully:', this.getFormData());
    }

    // Static method to create common validation rules
    static createCommonRules() {
        return {
            email: {
                required: true,
                email: true,
                requiredMessage: 'Email address is required',
                emailMessage: 'Please enter a valid email address'
            },
            password: {
                required: true,
                password: true,
                minLength: 8,
                requiredMessage: 'Password is required',
                minLengthMessage: 'Password must be at least 8 characters long'
            },
            confirmPassword: {
                required: true,
                match: 'password',
                requiredMessage: 'Please confirm your password',
                matchMessage: 'Passwords do not match'
            },
            phone: {
                phone: true,
                phoneMessage: 'Please enter a valid phone number'
            },
            required: {
                required: true,
                requiredMessage: 'This field is required'
            }
        };
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FormValidator;
}
