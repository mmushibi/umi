using System;

namespace UmiHealth.Domain.Entities
{
    /// <summary>
    /// Interface for entities that have branch context
    /// Used for automatic branch filtering in multi-tenant scenarios
    /// </summary>
    public interface IBranchEntity
    {
        /// <summary>
        /// The ID of the branch this entity belongs to
        /// Can be null for system-wide entities
        /// </summary>
        Guid? BranchId { get; set; }
    }
}
