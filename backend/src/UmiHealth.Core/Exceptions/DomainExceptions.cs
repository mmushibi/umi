using System;

namespace UmiHealth.Core.Exceptions
{
    /// <summary>
    /// Base exception class for all domain-level exceptions
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
        protected DomainException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when a tenant resource is not found
    /// </summary>
    public class TenantNotFoundException : DomainException
    {
        public string TenantId { get; set; }

        public TenantNotFoundException(string tenantId) 
            : base($"Tenant with ID '{tenantId}' was not found.") 
        {
            TenantId = tenantId;
        }
    }

    /// <summary>
    /// Exception thrown when a user does not have access to a tenant or branch
    /// </summary>
    public class TenantAccessDeniedException : DomainException
    {
        public string TenantId { get; set; }
        public string UserId { get; set; }

        public TenantAccessDeniedException(string tenantId, string userId, string message = null)
            : base(message ?? $"User '{userId}' does not have access to tenant '{tenantId}'.")
        {
            TenantId = tenantId;
            UserId = userId;
        }
    }

    /// <summary>
    /// Exception thrown when insufficient inventory is available
    /// </summary>
    public class InsufficientInventoryException : DomainException
    {
        public string ProductId { get; set; }
        public int RequestedQuantity { get; set; }
        public int AvailableQuantity { get; set; }

        public InsufficientInventoryException(string productName, string productId, int requested, int available)
            : base($"Insufficient inventory for '{productName}'. Requested: {requested}, Available: {available}")
        {
            ProductId = productId;
            RequestedQuantity = requested;
            AvailableQuantity = available;
        }
    }

    /// <summary>
    /// Exception thrown when attempting to create a duplicate entity
    /// </summary>
    public class DuplicateEntityException : DomainException
    {
        public string EntityType { get; set; }
        public string Key { get; set; }

        public DuplicateEntityException(string entityType, string key)
            : base($"An entity of type '{entityType}' with key '{key}' already exists.")
        {
            EntityType = entityType;
            Key = key;
        }
    }

    /// <summary>
    /// Exception thrown when an invalid operation is attempted
    /// </summary>
    public class InvalidOperationException : DomainException
    {
        public InvalidOperationException(string message) : base(message) { }
        public InvalidOperationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when a branch is not found
    /// </summary>
    public class BranchNotFoundException : DomainException
    {
        public string BranchId { get; set; }

        public BranchNotFoundException(string branchId)
            : base($"Branch with ID '{branchId}' was not found.")
        {
            BranchId = branchId;
        }
    }

    /// <summary>
    /// Exception thrown when a user is not found
    /// </summary>
    public class UserNotFoundException : DomainException
    {
        public string UserId { get; set; }

        public UserNotFoundException(string userId)
            : base($"User with ID '{userId}' was not found.")
        {
            UserId = userId;
        }
    }

    /// <summary>
    /// Exception thrown when a product is not found
    /// </summary>
    public class ProductNotFoundException : DomainException
    {
        public string ProductId { get; set; }

        public ProductNotFoundException(string productId)
            : base($"Product with ID '{productId}' was not found.")
        {
            ProductId = productId;
        }
    }

    /// <summary>
    /// Exception thrown when a subscription limit is exceeded
    /// </summary>
    public class SubscriptionLimitExceededException : DomainException
    {
        public string LimitType { get; set; }

        public SubscriptionLimitExceededException(string limitType)
            : base($"Subscription limit for '{limitType}' has been exceeded.")
        {
            LimitType = limitType;
        }
    }

    /// <summary>
    /// Exception thrown when payment processing fails
    /// </summary>
    public class PaymentFailedException : DomainException
    {
        public string TransactionId { get; set; }
        public string PaymentMethod { get; set; }

        public PaymentFailedException(string message, string transactionId = null, string paymentMethod = null)
            : base(message)
        {
            TransactionId = transactionId;
            PaymentMethod = paymentMethod;
        }
    }

    /// <summary>
    /// Exception thrown when an authorization check fails
    /// </summary>
    public class AuthorizationFailedException : DomainException
    {
        public string UserId { get; set; }
        public string Action { get; set; }
        public string Resource { get; set; }

        public AuthorizationFailedException(string userId, string action, string resource = null)
            : base($"User '{userId}' is not authorized to perform action '{action}'" + 
                   (resource != null ? $" on resource '{resource}'" : "") + ".")
        {
            UserId = userId;
            Action = action;
            Resource = resource;
        }
    }

    /// <summary>
    /// Exception thrown when a prescription cannot be filled
    /// </summary>
    public class PrescriptionFulfillmentException : DomainException
    {
        public string PrescriptionId { get; set; }

        public PrescriptionFulfillmentException(string prescriptionId, string message)
            : base(message)
        {
            PrescriptionId = prescriptionId;
        }
    }
}
