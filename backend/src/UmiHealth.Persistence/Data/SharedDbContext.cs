using Microsoft.EntityFrameworkCore;
using UmiHealth.Domain.Entities;

namespace UmiHealth.Persistence.Data
{
    public class SharedDbContext : DbContext
    {
        public SharedDbContext(DbContextOptions<SharedDbContext> options) : base(options)
        {
        }

        // Shared schema tables
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionTransaction> SubscriptionTransactions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<AdditionalUserRequest> AdditionalUserRequests { get; set; }
        public DbSet<AdditionalUserCharge> AdditionalUserCharges { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationSettings> NotificationSettings { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        
        // Multi-tenancy entities
        public DbSet<StockTransfer> StockTransfers { get; set; }
        public DbSet<StockTransferItem> StockTransferItems { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<BranchPermission> BranchPermissions { get; set; }
        public DbSet<ProcurementRequest> ProcurementRequests { get; set; }
        public DbSet<ProcurementItem> ProcurementItems { get; set; }
        public DbSet<ProcurementDistribution> ProcurementDistributions { get; set; }
        public DbSet<BranchReport> BranchReports { get; set; }

        // Queue Management entities
        public DbSet<QueuePatient> QueuePatients { get; set; }
        public DbSet<QueueHistory> QueueHistory { get; set; }
        public DbSet<QueueSettings> QueueSettings { get; set; }
        public DbSet<QueueNotification> QueueNotifications { get; set; }
        public DbSet<QueueAnalytics> QueueAnalytics { get; set; }
        public DbSet<HourlyQueueData> HourlyQueueData { get; set; }
        public DbSet<ProviderPerformance> ProviderPerformance { get; set; }

        // Super Admin schema tables
        public DbSet<SuperAdminLog> SuperAdminLogs { get; set; }
        public DbSet<SuperAdminReport> SuperAdminReports { get; set; }
        public DbSet<SystemAnalytics> SystemAnalytics { get; set; }
        public DbSet<SecurityEvent> SecurityEvents { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<SuperAdminUser> SuperAdminUsers { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }
        public DbSet<BackupRecord> BackupRecords { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Tenant entity
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.ToTable("tenants", "shared");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Subdomain)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.DatabaseName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("active");

                entity.Property(e => e.SubscriptionPlan)
                    .HasMaxLength(50)
                    .HasDefaultValue("basic");

                entity.Property(e => e.Settings)
                    .HasColumnType("jsonb");

                entity.Property(e => e.BillingInfo)
                    .HasColumnType("jsonb");

                entity.Property(e => e.ComplianceSettings)
                    .HasColumnType("jsonb");

                entity.HasIndex(e => e.Subdomain)
                    .IsUnique();

                entity.HasIndex(e => e.DatabaseName)
                    .IsUnique();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure Branch entity
            modelBuilder.Entity<Branch>(entity =>
            {
                entity.ToTable("branches", "shared");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Phone)
                    .HasMaxLength(50);

                entity.Property(e => e.Email)
                    .HasMaxLength(100);

                entity.Property(e => e.LicenseNumber)
                    .HasMaxLength(100);

                entity.Property(e => e.OperatingHours)
                    .HasColumnType("jsonb");

                entity.Property(e => e.Settings)
                    .HasColumnType("jsonb");

                entity.HasOne(e => e.Tenant)
                    .WithMany(t => t.Branches)
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Code);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Unique constraint on tenant_id + code
                entity.HasIndex(e => new { e.TenantId, e.Code })
                    .IsUnique();
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users", "shared");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.BranchAccess)
                    .HasColumnType("uuid[]");

                entity.Property(e => e.Permissions)
                    .HasColumnType("jsonb");

                entity.HasOne(e => e.Tenant)
                    .WithMany(t => t.Users)
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Branch)
                    .WithMany(b => b.Users)
                    .HasForeignKey(e => e.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.BranchId);
                entity.HasIndex(e => e.Email);

                // Unique constraint on tenant_id + email
                entity.HasIndex(e => new { e.TenantId, e.Email })
                    .IsUnique();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure Subscription entity
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.ToTable("subscriptions", "shared");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.PlanType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("active");

                entity.Property(e => e.BillingCycle)
                    .HasMaxLength(20)
                    .HasDefaultValue("monthly");

                entity.Property(e => e.Currency)
                    .HasMaxLength(3)
                    .HasDefaultValue("ZMW");

                entity.Property(e => e.Features)
                    .HasColumnType("jsonb");

                entity.Property(e => e.Limits)
                    .HasColumnType("jsonb");

                entity.HasOne(e => e.Tenant)
                    .WithMany(t => t.Subscriptions)
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure SubscriptionTransaction entity
            modelBuilder.Entity<SubscriptionTransaction>(entity =>
            {
                entity.ToTable("subscription_transactions", "shared");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.TransactionId)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Currency)
                    .HasMaxLength(3)
                    .HasDefaultValue("ZMW");

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("pending_approval");

                entity.Property(e => e.PlanFrom)
                    .HasMaxLength(50);

                entity.Property(e => e.PlanTo)
                    .HasMaxLength(50);

                entity.Property(e => e.RejectionReason)
                    .HasMaxLength(1000);

                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Subscription)
                    .WithMany()
                    .HasForeignKey(e => e.SubscriptionId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.RequestedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.RequestedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ApprovedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.TransactionId)
                    .IsUnique();
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure SubscriptionPlan entity
            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.ToTable("subscription_plans", "shared");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasColumnType("text");

                entity.Property(e => e.Features)
                    .HasColumnType("jsonb");

                entity.HasIndex(e => e.Name)
                    .IsUnique();
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure AdditionalUserRequest entity
            modelBuilder.Entity<AdditionalUserRequest>(entity =>
            {
                entity.ToTable("additional_user_requests", "shared");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.RequestId)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.UserEmail)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.UserFirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.UserLastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.UserRole)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.SubscriptionPlanAtRequest)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("pending_approval");

                entity.Property(e => e.RejectionReason)
                    .HasMaxLength(1000);

                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Branch)
                    .WithMany()
                    .HasForeignKey(e => e.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.RequestedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.RequestedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ApprovedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UserCreated)
                    .WithMany()
                    .HasForeignKey(e => e.UserCreatedId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.RequestId)
                    .IsUnique();
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.RequestedBy);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure AdditionalUserCharge entity
            modelBuilder.Entity<AdditionalUserCharge>(entity =>
            {
                entity.ToTable("additional_user_charges", "shared");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Currency)
                    .HasMaxLength(3)
                    .HasDefaultValue("ZMW");

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("pending_payment");

                entity.Property(e => e.PaymentReference)
                    .HasMaxLength(100);

                entity.Property(e => e.PaymentMethod)
                    .HasMaxLength(50);

                entity.Property(e => e.RejectionReason)
                    .HasMaxLength(1000);

                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ApprovedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.BillingMonth);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Unique constraint on tenant_id + user_id + billing_month
                entity.HasIndex(e => new { e.TenantId, e.UserId, e.BillingMonth })
                    .IsUnique();
            });

            // Configure Notification entity
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("notifications", "shared");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Message)
                    .IsRequired();

                entity.Property(e => e.Data)
                    .HasColumnType("jsonb");

                entity.Property(e => e.ActionUrl)
                    .HasMaxLength(500);

                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Sender)
                    .WithMany()
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.IsRead);
                entity.HasIndex(e => e.CreatedAt);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure NotificationSettings entity
            modelBuilder.Entity<NotificationSettings>(entity =>
            {
                entity.ToTable("notification_settings", "shared");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CustomAlerts)
                    .HasColumnType("jsonb");

                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.UserId);

                // Unique constraint on tenant_id + user_id
                entity.HasIndex(e => new { e.TenantId, e.UserId })
                    .IsUnique();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure PaymentTransaction entity
            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.ToTable("payment_transactions", "shared");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.TransactionReference)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Currency)
                    .HasMaxLength(3)
                    .HasDefaultValue("ZMW");

                entity.Property(e => e.PaymentMethod)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("pending");

                entity.Property(e => e.RefundReason)
                    .HasMaxLength(1000);

                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Charge)
                    .WithMany()
                    .HasForeignKey(e => e.ChargeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TransactionReference)
                    .IsUnique();
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ChargeId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.TransactionDate);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure SuperAdminLog entity
            modelBuilder.Entity<SuperAdminLog>(entity =>
            {
                entity.ToTable("super_admin_logs", "superadmin");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.LogLevel)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Message)
                    .IsRequired();

                entity.Property(e => e.UserId)
                    .HasMaxLength(255);

                entity.Property(e => e.TenantId)
                    .HasMaxLength(255);

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(45);

                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);

                entity.Property(e => e.Metadata)
                    .HasColumnType("jsonb");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.LogLevel);
                entity.HasIndex(e => e.Category);
            });

            // Configure SuperAdminReport entity
            modelBuilder.Entity<SuperAdminReport>(entity =>
            {
                entity.ToTable("super_admin_reports", "superadmin");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Description)
                    .HasMaxLength(1000);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("generating");

                entity.Property(e => e.FilePath)
                    .HasMaxLength(500);

                entity.Property(e => e.GeneratedBy)
                    .HasMaxLength(255);

                entity.Property(e => e.Parameters)
                    .HasColumnType("jsonb");

                entity.Property(e => e.Results)
                    .HasColumnType("jsonb");

                entity.Property(e => e.GeneratedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.GeneratedAt);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Type);
            });

            // Configure SystemAnalytics entity
            modelBuilder.Entity<SystemAnalytics>(entity =>
            {
                entity.ToTable("system_analytics", "superadmin");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Date)
                    .IsRequired();

                entity.Property(e => e.TenantStats)
                    .HasColumnType("jsonb");

                entity.Property(e => e.UserRoleStats)
                    .HasColumnType("jsonb");

                entity.Property(e => e.ApiUsageStats)
                    .HasColumnType("jsonb");

                entity.Property(e => e.PerformanceMetrics)
                    .HasColumnType("jsonb");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Date)
                    .IsUnique();
            });

            // Configure SecurityEvent entity
            modelBuilder.Entity<SecurityEvent>(entity =>
            {
                entity.ToTable("security_events", "superadmin");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.EventType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Severity)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.UserId)
                    .HasMaxLength(255);

                entity.Property(e => e.TenantId)
                    .HasMaxLength(255);

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(45);

                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);

                entity.Property(e => e.Resource)
                    .HasMaxLength(255);

                entity.Property(e => e.Action)
                    .HasMaxLength(100);

                entity.Property(e => e.FailureReason)
                    .HasMaxLength(500);

                entity.Property(e => e.Details)
                    .HasColumnType("jsonb");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.Severity);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TenantId);
            });

            // Configure SystemSetting entity
            modelBuilder.Entity<SystemSetting>(entity =>
            {
                entity.ToTable("system_settings", "superadmin");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Value)
                    .IsRequired();

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.DataType)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("string");

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.UpdatedBy)
                    .HasMaxLength(255);

                entity.Property(e => e.ValidationRules)
                    .HasColumnType("jsonb");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Key)
                    .IsUnique();
                entity.HasIndex(e => e.Category);
            });

            // Configure SuperAdminUser entity
            modelBuilder.Entity<SuperAdminUser>(entity =>
            {
                entity.ToTable("super_admin_users", "superadmin");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("superadmin");

                entity.Property(e => e.Permissions)
                    .HasColumnType("text[]");

                entity.Property(e => e.TwoFactorSecret)
                    .HasMaxLength(255);

                entity.Property(e => e.BackupCodes)
                    .HasColumnType("text[]");

                entity.Property(e => e.Preferences)
                    .HasColumnType("jsonb");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Email)
                    .IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            // Configure SystemNotification entity
            modelBuilder.Entity<SystemNotification>(entity =>
            {
                entity.ToTable("system_notifications", "superadmin");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Message)
                    .IsRequired();

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.TargetAudience)
                    .HasMaxLength(50);

                entity.Property(e => e.TargetTenants)
                    .HasColumnType("text[]");

                entity.Property(e => e.TargetUsers)
                    .HasColumnType("text[]");

                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(255);

                entity.Property(e => e.Metadata)
                    .HasColumnType("jsonb");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.Type);
            });

            // Configure BackupRecord entity
            modelBuilder.Entity<BackupRecord>(entity =>
            {
                entity.ToTable("backup_records", "superadmin");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("pending");

                entity.Property(e => e.TenantId)
                    .HasMaxLength(255);

                entity.Property(e => e.FilePath)
                    .HasMaxLength(500);

                entity.Property(e => e.Checksum)
                    .HasMaxLength(255);

                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(255);

                entity.Property(e => e.ErrorMessage)
                    .HasMaxLength(1000);

                entity.Property(e => e.Configuration)
                    .HasColumnType("jsonb");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Type);
            });

            // Configure ApiKey entity
            modelBuilder.Entity<ApiKey>(entity =>
            {
                entity.ToTable("api_keys", "superadmin");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.KeyHash)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Prefix)
                    .HasMaxLength(20);

                entity.Property(e => e.Permissions)
                    .HasColumnType("text[]");

                entity.Property(e => e.AllowedEndpoints)
                    .HasColumnType("text[]");

                entity.Property(e => e.AllowedIpAddresses)
                    .HasColumnType("text[]");

                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(255);

                entity.Property(e => e.Metadata)
                    .HasColumnType("jsonb");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.Prefix);
            });

            // Apply soft deletes globally
            ApplySoftDeletes(modelBuilder);
        }

        private void ApplySoftDeletes(ModelBuilder modelBuilder)
        {
            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Where(e => typeof(ISoftDeletable).IsAssignableFrom(e.ClrType));

            foreach (var entityType in entityTypes)
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, "DeletedAt");
                var condition = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(null));
                var lambda = System.Linq.Expressions.Expression.Lambda(condition, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public interface ISoftDeletable
    {
        public DateTime? DeletedAt { get; set; }
    }
}
